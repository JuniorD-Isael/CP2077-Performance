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
    [string]$Profile = (Join-Path $PSScriptRoot "..\profile\lowspec-30fps.json"),
    [string]$GameRoot = "",
    [switch]$DryRun,
    [switch]$Restore,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

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
Write-Host "[1/3] user.ini (cvars do motor)" -ForegroundColor White
if (-not (Test-Path $iniPath)) {
    Write-Host "  !! user.ini nao encontrado em $iniPath" -ForegroundColor Red
} else {
    $ini = Read-Ini $iniPath
    $preserve = $null
    if ($prof.preserve -and $prof.preserve.userIni) {
        $preserve = ConvertTo-Hashtable $prof.preserve.userIni
    }

    $values = ConvertTo-Hashtable $prof.userIni
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
Write-Host "[2/3] UserSettings.json (opcoes do menu)" -ForegroundColor White
if (-not (Test-Path $usPath)) {
    Write-Host "  !! UserSettings.json nao encontrado (jogo nunca rodou?) em $usPath" -ForegroundColor Red
} else {
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
Write-Host "[3/3] cyber_engine_tweaks\config.json (patches do motor)" -ForegroundColor White
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

Write-Host ""
if ($DryRun) {
    Write-Host "  DRY-RUN concluido. Rode sem -DryRun para aplicar." -ForegroundColor Yellow
} else {
    Write-Host "  Perfil aplicado. Reinicie o jogo." -ForegroundColor Green
    Write-Host "  Para desfazer: .\Install-Profile.ps1 -Restore" -ForegroundColor DarkGray
}
Write-Host ""
