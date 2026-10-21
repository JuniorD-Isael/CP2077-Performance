<#
.SYNOPSIS
  Aplica um perfil de performance ao Cyberpunk 2077.

.DESCRIPTION
  Le um perfil JSON e aplica-o em tres camadas:
    1. engine\config\platform\pc\user.ini   (cvars do motor)
    2. UserSettings.json                    (opcoes do menu)
    3. cyber_engine_tweaks\config.json      (patches do CET)

  Sempre cria backup antes de escrever. E idempotente: rodar duas vezes
  produz o mesmo resultado. Opera com o jogo FECHADO (o jogo reescreve
  UserSettings.json ao sair e sobrescreveria as mudancas).

.PARAMETER Profile
  Caminho do perfil JSON. Padrao: profile\lowspec-30fps.json

.PARAMETER GameRoot
  Raiz do jogo. Padrao: procura o Cyberpunk2077.exe em locais comuns.

.PARAMETER DryRun
  Mostra o que seria alterado sem escrever nada.

.PARAMETER Restore
  Restaura os backups mais recentes.

.PARAMETER Force
  Nao falha se o jogo estiver rodando (apenas avisa).

.EXAMPLE
  .\Install-Profile.ps1 -DryRun
  .\Install-Profile.ps1
  .\Install-Profile.ps1 -Restore
#>
[CmdletBinding()]
param(
    # Nao usar $PSScriptRoot como default aqui: com [CmdletBinding()] o
    # PowerShell 5.1 nao popula $PSScriptRoot durante a avaliacao dos
    # defaults do param quando o script e chamado via `powershell -File`.
    # O default e resolvido no corpo do script, mais abaixo.
    [string]$Profile = "",
    [string]$GameRoot = "",
    [switch]$DryRun,
    [switch]$Restore,
    [switch]$Force,
    [switch]$SkipBinaryCheck,
    [switch]$NoMod
)

$ErrorActionPreference = "Stop"

if (-not $Profile) {
    $Profile = Join-Path $PSScriptRoot "..\profile\lowspec-30fps.json"
}

# ---------------------------------------------------------------- funcoes

function Find-GameRoot {
    if ($GameRoot -and (Test-Path $GameRoot)) { return (Resolve-Path $GameRoot).Path }

    $candidates = @(
        "E:\SteamLibrary\steamapps\common\Cyberpunk 2077",
        "D:\SteamLibrary\steamapps\common\Cyberpunk 2077",
        "C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077",
        "C:\Program Files\Steam\steamapps\common\Cyberpunk 2077"
    )
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c "bin\x64\Cyberpunk2077.exe")) { return $c }
    }

    # Ultimo recurso: achar pelo executavel
    $found = Get-ChildItem -Path "C:\", "D:\", "E:\" -Filter "Cyberpunk2077.exe" -Recurse -ErrorAction SilentlyContinue |
             Where-Object { $_.FullName -match "bin\\x64" } | Select-Object -First 1
    if ($found) { return (Split-Path (Split-Path (Split-Path $found.FullName) -Parent) -Parent) }

    throw "Nao foi possivel localizar o Cyberpunk 2077. Use -GameRoot <caminho>."
}

function Get-UserSettingsPath {
    $base = Join-Path $env:LOCALAPPDATA "CD Projekt Red\Cyberpunk 2077"
    return (Join-Path $base "UserSettings.json")
}

function ConvertTo-Hashtable {
    # Converte PSCustomObject do ConvertFrom-Json em hashtable, preservando ordem de insercao.
    param($InputObject)
    if ($null -eq $InputObject) { return $null }
    if ($InputObject -is [System.Collections.IDictionary]) { return $InputObject }
    if ($InputObject -is [System.Management.Automation.PSCustomObject]) {
        $h = [ordered]@{}
        foreach ($p in $InputObject.PSObject.Properties) { $h[$p.Name] = ConvertTo-Hashtable $p.Value }
        return $h
    }
    if ($InputObject -is [Array]) {
        $l = @()
        foreach ($i in $InputObject) { $l += ,(ConvertTo-Hashtable $i) }
        return ,$l
    }
    return $InputObject
}

function Get-SettingsSchema {
    # Le o schema oficial do jogo: r6/config/settings/options.json + platform/pc/options.json
    # Cada opcao pode ter um array 'values' com os valores aceitos.
    param([string]$Root)
    $schema = @{}
    $files = @(
        (Join-Path $Root "r6\config\settings\options.json"),
        (Join-Path $Root "r6\config\settings\platform\pc\options.json")
    )
    foreach ($f in $files) {
        if (-not (Test-Path $f)) { continue }
        try { $j = Get-Content $f -Raw | ConvertFrom-Json } catch { continue }
        foreach ($g in $j.options) {
            foreach ($o in $g.options) {
                if (-not $schema.ContainsKey($o.name)) { $schema[$o.name] = $o }
            }
        }
    }
    return $schema
}

# ------------------------------------------------------------------ binario

# Chaves de cvar que o proprio user.ini original do jogo traz, mas que NAO
# existem como token no executavel. Ver docs/FINDINGS.md secao 10.
$KnownAbsentCvars = @(
    "GlassUseMipChain"
)

# Retorna o conjunto de tokens ASCII imprimiveis do executavel do jogo.
# O motor registra as opcoes como struct GameOption e o log do CET expoe o
# path no formato "Grupo/Feature/Nome" - o path e montado em runtime, o
# executavel guarda a secao e a chave SEPARADAMENTE. Logo, para validar uma
# chave de cvar, procuramos o token isolado (delimitado por byte nao
# imprimivel) correspondente ao NOME da chave.
function Get-BinaryTokenSet {
    param([string]$Path, [string[]]$Tokens)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $text  = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)
    $want = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($t in $Tokens) { [void]$want.Add($t) }
    $minLen = 999; $maxLen = 0
    foreach ($t in $Tokens) {
        if ($t.Length -lt $minLen) { $minLen = $t.Length }
        if ($t.Length -gt $maxLen) { $maxLen = $t.Length }
    }
    if ($minLen -gt $maxLen) { $minLen = 1; $maxLen = 1 }
    foreach ($chunk in $text.Split([char]0)) {
        if ($chunk.Length -lt $minLen -or $chunk.Length -gt $maxLen) { continue }
        # [void] obrigatorio: HashSet.Remove() devolve um booleano que, sem
        # supressao, seria emitido no pipeline e poluiria o retorno da funcao.
        if ($want.Contains($chunk)) { [void]$want.Remove($chunk) }
    }
    # o que sobrou em $want NAO existe no binario
    return $want
}

# ------------------------------------------------------------------ assert

function Assert-GameClosed {
    $proc = Get-Process -Name "Cyberpunk2077" -ErrorAction SilentlyContinue
    if ($proc -and -not $Force) {
        throw "O jogo esta rodando (PID $($proc.Id)). Feche-o antes de aplicar: ele reescreve UserSettings.json ao sair. Use -Force para ignorar."
    }
    if ($proc) { Write-Warning "Jogo rodando (PID $($proc.Id)) - as mudancas podem ser sobrescritas ao sair." }
}

function Backup-File {
    param([string]$Path, [string]$Label)
    if (-not (Test-Path $Path)) { return $null }
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $dir = Join-Path $PSScriptRoot "..\backups\$stamp"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $dest = Join-Path $dir ($Label + "-" + (Split-Path $Path -Leaf))
    Copy-Item $Path $dest -Force
    Write-Host "  backup: $dest" -ForegroundColor DarkGray
    return $dest
}

# ---------------------------------------------------------------- ini

function Read-Ini {
    param([string]$Path)
    $result = @{}
    if (-not (Test-Path $Path)) { return $result }
    $section = ""
    foreach ($line in Get-Content $Path) {
        $t = $line.Trim()
        if ($t -eq "" -or $t.StartsWith(";") -or $t.StartsWith("#")) { continue }
        if ($t -match "^\[(.+)\]$") { $section = $Matches[1]; if (-not $result.ContainsKey($section)) { $result[$section] = @{} }; continue }
        if ($t -match "^([^=]+)=(.*)$") {
            if (-not $result.ContainsKey($section)) { $result[$section] = @{} }
            $result[$section][$Matches[1].Trim()] = $Matches[2].Trim()
        }
    }
    return $result
}

function Update-IniFile {
    # Atualiza o INI preservando ordem de secoes, ordem de chaves e comentarios.
    # Só toca nas linhas realmente alteradas; novas secoes/chaves sao inseridas
    # no fim. Comentarios do motor (avisos de efeito colateral) sao preservados.
    param([string]$Path, $Values, $Preserve)

    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($l in (Get-Content $Path)) { $lines.Add($l) }

    $changed = @()

    # localizar secoes
    $sections = @{}   # nome -> @{ start; end; keys }
    $current = ""
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $t = $lines[$i].Trim()
        if ($t -match "^\[(.+)\]$") {
            $current = $Matches[1]
            $sections[$current] = @{ start = $i; end = $lines.Count; keys = @{} }
        }
        elseif ($current -ne "" -and $t -match "^([^=;#]+)=(.*)$") {
            $sections[$current].keys[$Matches[1].Trim()] = $i
        }
    }
    # corrigir limites de secao
    $starts = @($sections.Keys | ForEach-Object { $sections[$_].start } | Sort-Object)
    foreach ($sec in $sections.Keys) {
        $s = $sections[$sec].start
        $next = $starts | Where-Object { $_ -gt $s } | Select-Object -First 1
        $sections[$sec].end = if ($next) { $next } else { $lines.Count }
    }

    # 1) atualizar chaves existentes
    foreach ($sec in $Values.Keys) {
        if ($sec.StartsWith("_")) { continue }
        if (-not $sections.ContainsKey($sec)) { continue }
        foreach ($k in $Values[$sec].Keys) {
            if ($Preserve -and $Preserve.Contains($sec) -and $Preserve[$sec].Contains($k)) { continue }
            if (-not $sections[$sec].keys.ContainsKey($k)) { continue }
            $idx = $sections[$sec].keys[$k]
            $m = [regex]::Match($lines[$idx], "^\s*([^=;#]+?)\s*=(.*)$")
            $have = $m.Groups[2].Value.Trim()
            $want = [string]$Values[$sec][$k]
            if ($have -ne $want) {
                $lines[$idx] = "$($m.Groups[1].Value) = $want"
                $changed += "[$sec] $k : $have -> $want"
            }
        }
    }

    # 2) inserir chaves novas dentro de secoes existentes (antes da proxima secao)
    $inserts = @{}
    foreach ($sec in $Values.Keys) {
        if ($sec.StartsWith("_")) { continue }
        if (-not $sections.ContainsKey($sec)) { continue }
        foreach ($k in $Values[$sec].Keys) {
            if ($Preserve -and $Preserve.Contains($sec) -and $Preserve[$sec].Contains($k)) { continue }
            if ($sections[$sec].keys.ContainsKey($k)) { continue }
            if (-not $inserts.ContainsKey($sec)) { $inserts[$sec] = @() }
            $inserts[$sec] += $k
        }
    }
    # do fim para o inicio, para nao invalidar indices
    foreach ($sec in @($inserts.Keys | Sort-Object { $sections[$_].start } -Descending)) {
        $at = $sections[$sec].end
        $block = @()
        foreach ($k in $inserts[$sec]) {
            $block += "$k = $($Values[$sec][$k])"
            $changed += "[$sec] $k : (novo) -> $($Values[$sec][$k])"
        }
        $lines.InsertRange($at, [string[]]$block)
    }

    # 3) secoes novas, anexadas ao fim
    $newSections = @()
    foreach ($sec in $Values.Keys) {
        if ($sec.StartsWith("_")) { continue }
        if ($sections.ContainsKey($sec)) { continue }
        $newSections += $sec
    }
    foreach ($sec in $newSections) {
        $lines.Add("")
        $lines.Add("[$sec]")
        foreach ($k in $Values[$sec].Keys) {
            if ($Preserve -and $Preserve.Contains($sec) -and $Preserve[$sec].Contains($k)) { continue }
            $lines.Add("$k = $($Values[$sec][$k])")
            $changed += "[$sec] $k : (novo) -> $($Values[$sec][$k])"
        }
    }

    [System.IO.File]::WriteAllText($Path, (($lines -join "`r`n") + "`r`n"))
    return $changed
}

function Set-IniValues {
    param([hashtable]$Data, $Values, $Preserve)
    $changed = @()
    foreach ($sec in $Values.Keys) {
        if ($sec.StartsWith("_")) { continue }
        if (-not $Data.ContainsKey($sec)) { $Data[$sec] = @{} }
        foreach ($k in $Values[$sec].Keys) {
            $want = [string]$Values[$sec][$k]
            # grupo preservado: nao mexe
            if ($Preserve -and $Preserve.Contains($sec) -and $Preserve[$sec].Contains($k)) { continue }
            $have = if ($Data[$sec].ContainsKey($k)) { [string]$Data[$sec][$k] } else { $null }
            if ($have -ne $want) {
                $Data[$sec][$k] = $want
                $changed += "[$sec] $k : $have -> $want"
            }
        }
    }
    return $changed
}

# ---------------------------------------------------------------- main

if ($Restore) {
    $base = Join-Path $PSScriptRoot "..\backups"
    if (-not (Test-Path $base)) { Write-Host "Nenhum backup encontrado."; exit 0 }
    $latest = Get-ChildItem $base -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if (-not $latest) { Write-Host "Nenhum backup encontrado."; exit 0 }
    Write-Host "Restaurando de: $($latest.FullName)" -ForegroundColor Cyan
    $root = Find-GameRoot
    $map = @{
        "user.ini"         = Join-Path $root "engine\config\platform\pc\user.ini"
        "UserSettings.json" = (Get-UserSettingsPath)
        "config.json"      = Join-Path $root "bin\x64\plugins\cyber_engine_tweaks\config.json"
    }
    foreach ($f in (Get-ChildItem $latest.FullName -File)) {
        $key = $f.Name -replace "-.*$", ""
        if ($map.ContainsKey($key)) {
            Copy-Item $f.FullName $map[$key] -Force
            Write-Host "  restaurado: $($map[$key])" -ForegroundColor Green
        }
    }
    exit 0
}

$root = Find-GameRoot
$profilePath = (Resolve-Path $Profile).Path
$prof = Get-Content $profilePath -Raw | ConvertFrom-Json

Write-Host ""
Write-Host "  Perfil : $($prof.name) v$($prof.version)" -ForegroundColor Cyan
Write-Host "  Jogo   : $root" -ForegroundColor Cyan
Write-Host "  Alvo   : $($prof.target.cpu) / $($prof.target.gpu) / meta $($prof.target.goal_fps) FPS" -ForegroundColor Cyan
if ($DryRun) { Write-Host "  MODO   : DRY-RUN (nada sera escrito)" -ForegroundColor Yellow }
Write-Host ""

Assert-GameClosed

$iniPath    = Join-Path $root "engine\config\platform\pc\user.ini"
$usPath     = Get-UserSettingsPath
$cetPath    = Join-Path $root "bin\x64\plugins\cyber_engine_tweaks\config.json"

# --- 1. user.ini ---
Write-Host "[1/4] user.ini (cvars do motor)" -ForegroundColor White
if (-not (Test-Path $iniPath)) {
    Write-Host "  !! user.ini nao encontrado em $iniPath" -ForegroundColor Red
} else {
    $ini = Read-Ini $iniPath
    $preserve = $null
    if ($prof.preserve -and $prof.preserve.userIni) {
        $preserve = ConvertTo-Hashtable $prof.preserve.userIni
    }

    $values = ConvertTo-Hashtable $prof.userIni

    # --- validacao das chaves de cvar contra o executavel ---
    if (-not $SkipBinaryCheck) {
        $exePath = Join-Path $root "bin\x64\Cyberpunk2077.exe"
        if (-not (Test-Path $exePath)) {
            Write-Host "  !! executavel nao encontrado em $exePath - validacao de chaves pulada" -ForegroundColor Yellow
        } else {
            $cvarNames = @()
            foreach ($sec in $values.Keys) {
                if ($sec.StartsWith("_")) { continue }
                foreach ($k in $values[$sec].Keys) { $cvarNames += $k }
            }
            $cvarNames = @($cvarNames | Sort-Object -Unique)
            $swBin = [Diagnostics.Stopwatch]::StartNew()
            $absent = Get-BinaryTokenSet -Path $exePath -Tokens $cvarNames
            $elapsed = [math]::Round($swBin.Elapsed.TotalSeconds,1)
            $present = $cvarNames.Count - $absent.Count
            Write-Host ("  chaves de cvar presentes no executavel: {0} de {1} ({2}s)" -f $present, $cvarNames.Count, $elapsed) -ForegroundColor DarkGray
            if ($absent.Count -gt 0) {
                foreach ($a in ($absent | Sort-Object)) {
                    if ($KnownAbsentCvars -contains $a) {
                        Write-Host "  AVISO: cvar '$a' nao existe no executavel, mas vem do user.ini original do proprio jogo (ver docs/FINDINGS.md)" -ForegroundColor Yellow
                    } else {
                        Write-Host "  AVISO: cvar '$a' NAO existe no executavel - probable erro de digitacao; o motor ignora chaves desconhecidas em silencio" -ForegroundColor Red
                    }
                }
            }
        }
    }

    # simular em memoria para so mostrar o que mudaria
    $tmp = [System.IO.Path]::GetTempFileName()
    Copy-Item $iniPath $tmp -Force
    $changes = Update-IniFile -Path $tmp -Values $values -Preserve $preserve
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    if ($changes.Count -eq 0) {
        Write-Host "  ja esta no perfil - nada a fazer" -ForegroundColor DarkGray
    } else {
        foreach ($c in $changes) { Write-Host "  $c" }
        if (-not $DryRun) {
            Backup-File -Path $iniPath -Label "user.ini" | Out-Null
            $applied = Update-IniFile -Path $iniPath -Values $values -Preserve $preserve
            Write-Host "  -> escrito ($($applied.Count) alteracoes)" -ForegroundColor Green
        }
    }
}

# --- 2. UserSettings.json ---
Write-Host "[2/4] UserSettings.json (opcoes do menu)" -ForegroundColor White
if (-not (Test-Path $usPath)) {
    Write-Host "  !! UserSettings.json nao encontrado (jogo nunca rodou?) em $usPath" -ForegroundColor Red
} else {
    # Validar os valores do perfil contra o schema do jogo.
    # O jogo reverte valores invalidos ao carregar, silenciosamente.
    $schema = Get-SettingsSchema -Root $root
    $invalid = @()
    foreach ($p in $prof.userSettings.values.PSObject.Properties) {
        if (-not $schema.ContainsKey($p.Name)) {
            $invalid += "$($p.Name): opcao nao existe no schema do jogo"
            continue
        }
        $opt = $schema[$p.Name]
        if ($opt.PSObject.Properties.Name -contains 'values' -and $opt.values) {
            $valid = @($opt.values)
            if ($valid -notcontains ([string]$p.Value)) {
                $invalid += "$($p.Name) = '$($p.Value)' invalido; aceitos: $($valid -join ', ')"
            }
        }
    }
    if ($invalid.Count -gt 0) {
        Write-Host "  !! VALORES INVALIDOS (o jogo vai reverter):" -ForegroundColor Red
        foreach ($i in $invalid) { Write-Host "     $i" -ForegroundColor Red }
    } else {
        Write-Host "  todos os valores conferem com o schema do jogo" -ForegroundColor DarkGray
    }

    $us = Get-Content $usPath -Raw | ConvertFrom-Json
    $usChanged = @()
    foreach ($grp in $us.data) {
        foreach ($opt in $grp.options) {
            if (-not $prof.userSettings.values.PSObject.Properties.Name.Contains($opt.name)) { continue }
            $want = $prof.userSettings.values.($opt.name)
            $have = $opt.value
            $same = ([string]$have) -eq ([string]$want)
            if (-not $same) {
                $usChanged += "$($opt.name) : $have -> $want"
                $opt.value = $want
            }

            # O arquivo guarda DOIS campos para opcoes de lista: `value` (o valor)
            # e `index` (a posicao na lista `values`). Eles podem divergir, e o
            # motor normaliza um para o outro no load. Escrevemos os dois de
            # forma coerente para nao deixar o indice apontando para o valor
            # anterior. (Ver docs/FINDINGS.md - o indice e um campo que o
            # instalador antigo nunca escrevia.)
            if ($opt.PSObject.Properties.Name -contains 'values' -and $opt.values) {
                $idx = -1
                $i = 0
                foreach ($v in $opt.values) {
                    if (([string]$v) -eq ([string]$want)) { $idx = $i; break }
                    $i++
                }
                if ($idx -ge 0 -and $opt.PSObject.Properties.Name -contains 'index') {
                    if ([int]$opt.index -ne $idx) {
                        $usChanged += "$($opt.name).index : $($opt.index) -> $idx"
                        $opt.index = $idx
                    }
                }
            }
        }
    }
    if ($usChanged.Count -eq 0) {
        Write-Host "  ja esta no perfil - nada a fazer" -ForegroundColor DarkGray
    } else {
        foreach ($c in $usChanged) { Write-Host "  $c" }
        if (-not $DryRun) {
            Backup-File -Path $usPath -Label "UserSettings.json" | Out-Null
            [System.IO.File]::WriteAllText($usPath, ($us | ConvertTo-Json -Depth 20))
            Write-Host "  -> escrito" -ForegroundColor Green
        }
    }
}

# --- 3. CET config.json ---
Write-Host "[3/4] cyber_engine_tweaks\config.json (patches do motor)" -ForegroundColor White
if (-not (Test-Path $cetPath)) {
    Write-Host "  !! CET nao instalado em $cetPath" -ForegroundColor Yellow
} else {
    $cet = Get-Content $cetPath -Raw | ConvertFrom-Json
    $cetChanged = @()
    foreach ($p in $prof.cetConfig.patches.PSObject.Properties) {
        $cur = $cet.patches.($p.Name)
        if ($null -eq $cur) { $cetChanged += "$($p.Name) : (ausente) -> $($p.Value)"; continue }
        if ([bool]$cur -ne [bool]$p.Value) {
            $cetChanged += "$($p.Name) : $cur -> $($p.Value)"
            $cet.patches.($p.Name) = [bool]$p.Value
        }
    }
    if ($cetChanged.Count -eq 0) {
        Write-Host "  ja esta no perfil - nada a fazer" -ForegroundColor DarkGray
    } else {
        foreach ($c in $cetChanged) { Write-Host "  $c" }
        if (-not $DryRun) {
            Backup-File -Path $cetPath -Label "config.json" | Out-Null
            [System.IO.File]::WriteAllText($cetPath, ($cet | ConvertTo-Json -Depth 20))
            Write-Host "  -> escrito" -ForegroundColor Green
        }
    }
}

# --- 4. mod in-game ---
# O painel le o profile.json que fica AO LADO do init.lua. O repositorio
# continua sendo a fonte unica: o instalador copia os dois para a pasta de
# mods do CET.
if (-not $NoMod) {
    Write-Host "[4/4] mod in-game (PerformanceProfile)" -ForegroundColor White
    $modSrc  = Join-Path $PSScriptRoot "..\mod\PerformanceProfile"
    $modDst  = Join-Path $root "bin\x64\plugins\cyber_engine_tweaks\mods\PerformanceProfile"
    $profSrc = $profilePath
    if (-not (Test-Path $modSrc)) {
        Write-Host "  !! mod nao encontrado em $modSrc" -ForegroundColor Red
    } elseif (-not (Test-Path $modDst)) {
        Write-Host "  !! pasta de mods do CET nao encontrada em $modDst" -ForegroundColor Red
    } else {
        # limpar debris de sessoes anteriores (artefatos deste projeto).
        # NUNCA em dry-run: o dry-run nao escreve nada.
        if (-not $DryRun) {
            Get-ChildItem $modDst -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match '^performanceprofile-.*\.log$' -or $_.Name -eq 'PerformanceProfile.log' -or $_.Extension -in @('.tmp', '.bak') } |
                ForEach-Object {
                    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
                    Write-Host "  removido debris: $($_.Name)" -ForegroundColor DarkGray
                }
        } else {
            $debris = @(Get-ChildItem $modDst -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match '^performanceprofile-.*\.log$' -or $_.Name -eq 'PerformanceProfile.log' -or $_.Extension -in @('.tmp', '.bak') })
            if ($debris.Count -gt 0) {
                Write-Host "  removeria {0} arquivo(s) de debris de sessao" -f $debris.Count -ForegroundColor Yellow
            }
        }

        $items = @(
            @{ From = (Join-Path $modSrc "init.lua"); To = (Join-Path $modDst "init.lua") },
            @{ From = $profSrc;                            To = (Join-Path $modDst "profile.json") }
        )
        foreach ($it in $items) {
            if (-not (Test-Path $it.From)) { continue }
            $same = $false
            if (Test-Path $it.To) {
                $same = ((Get-FileHash $it.From -Algorithm SHA256).Hash -eq (Get-FileHash $it.To -Algorithm SHA256).Hash)
            }
            if ($same) {
                Write-Host "  ja atualizado: $(Split-Path $it.To -Leaf)" -ForegroundColor DarkGray
            } elseif ($DryRun) {
                Write-Host "  copiaria: $(Split-Path $it.To -Leaf)" -ForegroundColor Yellow
            } else {
                Copy-Item $it.From $it.To -Force
                Write-Host "  copiado: $(Split-Path $it.To -Leaf)" -ForegroundColor Green
            }
        }
    }
}

Write-Host ""
if ($DryRun) {
    Write-Host "  DRY-RUN concluido. Rode sem -DryRun para aplicar." -ForegroundColor Yellow
} else {
    Write-Host "  Perfil aplicado. Reinicie o jogo." -ForegroundColor Green
    Write-Host "  Para desfazer: .\Install-Profile.ps1 -Restore" -ForegroundColor DarkGray
}
Write-Host ""
