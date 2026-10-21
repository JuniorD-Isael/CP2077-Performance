# Changelog

Todas as versões notáveis deste projeto.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e
o versionamento segue [SemVer](https://semver.org/lang/pt-BR/).

## [0.1.0] — 2026-10-20

Primeira versão estável. Validada em runtime no hardware de referência
(AMD Ryzen 5 5500U + Radeon Vega 8 integrada, 20 GB RAM).

### Adicionado

**Perfil** (`profile/lowspec-30fps.json`)
- Fonte única de verdade, em JSON auditável, cobrindo as três camadas de
  configuração: cvars do motor, opções do menu e patches do CET.
- Seção `preserve` explícita para os valores que não podem ser alterados
  (`DistantShadows`, `GlobalIllumination`, `Hair`, Ray Tracing).

**Instalador** (`installer/Install-Profile.ps1`)
- `-DryRun` mostra o que será alterado sem escrever nada.
- `-Restore` reverte a partir do backup mais recente.
- Backup automático com timestamp antes de cada escrita.
- Recusa rodar com o jogo aberto (ele reescreve `UserSettings.json` ao sair).
- Atualização de INI que preserva ordem de seções, ordem de chaves e os
  comentários do motor (que documentam efeitos colaterais).
- Idempotente: rodar duas vezes produz o mesmo resultado.

**Painel in-game** (`mod/PerformanceProfile/init.lua`)
- Aba "Low-Spec 30 FPS" no menu de settings nativo, via Native Settings UI.
- Subcategorias: GPU / Render, Resolução / FPS, CPU / Sistema, Aplicar.
- Widgets: seletores de lista, sliders (int) e switches.
- Botão "Apply Profile" que escreve `user.ini` e `UserSettings.json` com backup.
- Log próprio com timestamp no nome (o log do CET é bufferizado e só flusha
  no fim da sessão).

**Documentação**
- `docs/FINDINGS.md` — como cada decisão foi derivada, com evidências.
- `docs/NATIVE-SETTINGS-API.md` — API de integração e armadilhas do ambiente
  Lua do CET.

### Corrigido

Bugs encontrados no `ConfigOverhaulCyberpunk.exe` (decompilado com `ilspycmd`),
que motivam não usá-lo como base:

- Toggle de RayTracing nunca é persistido — `Form1_FormClosed` escreve em
  `redConfig` em vez de `val`.
- A maioria dos grupos de cvar é inalcançável — `InitializeFeatureList` só
  cria seções para `Developer`, `Rendering` e `RayTracing`.
- O preset "Reset" apaga o `user.ini` inteiro.

### Notas técnicas

- A integração com Native Settings UI usa `GetMod("nativeSettings")`.
  `require` não atravessa mods: o CET carrega cada mod em ambiente isolado
  (`setfenv`) e restringe `LUA_PATH` ao diretório do mod.
- As abas de mods só aparecem quando se entra em Configurações pelo item
  "MODS" do menu — o NativeSettings detecta a origem pelo `label == "Mods"`.
- `debug` pode ser `nil` no ambiente Lua do CET; todo acesso precisa de guarda.

### Validação

- Perfil confirmado ativo em runtime: QPS máximo LIGADO (30), DRS ativo com
  piso 50%, `VolumetricCloudsQuality` Low, Ray Tracing desligado.
- Painel validado visualmente no jogo: todos os widgets refletem o perfil.
- Diagnóstico de gargalo: GPU a 89–105% (saturada), CPU a 43–46%,
  RAM 7,1–7,8 GB. O gargalo é a GPU integrada, não CPU nem memória.

### Limitações conhecidas

- O `user.ini` só é lido no startup do jogo; o painel exige reiniciar.
- O jogo reescreve `UserSettings.json` ao sair, sobrescrevendo alterações
  feitas por ferramentas externas durante a sessão.
- FPS absoluto não foi medido automaticamente: ETW exige privilégio de
  administrador, `DwmGetCompositionTimingInfo` falha em fullscreen e o
  PresentMon não pôde ser obtido no ambiente. As métricas de GPU/CPU vêm dos
  performance counters do Windows.
- A análise de engenharia reversa profunda do `Cyberpunk2077.exe` (Ghidra) não
  completou — os analisadores `StackVariableAnalyzer` e `EntryPointAnalyzer` são
  inviáveis nesse alvo de 60 MB. Nenhuma conclusão do projeto depende dela.

## [0.1.1-alpha] — 2026-10-21

Alpha de correção, derivada de uma sessão de jogo instrumentada (19 min de
amostragem de ~1 s + rastreamento de eventos de filesystem). Todos os itens
abaixo têm evidência em `docs/FINDINGS.md` seções 10 a 12.

### Corrigido

Painel in-game (`mod/PerformanceProfile`):

- O `user.ini` era procurado com **cinco** níveis `..` a partir do diretório
  do mod; a raiz do jogo está **seis** níveis acima. A camada de cvars do
  motor — a de maior impacto — **nunca** foi aplicada pelo painel (três
  falhas registradas em log entre 22:12 e 23:38).
- O painel aplicava só 2 das 3 camadas. Agora escreve também
  `cyber_engine_tweaks/config.json`.
- O painel tinha uma cópia hardcoded do perfil com **8 chaves a menos** que o
  `profile/lowspec-30fps.json`. Passou a ler o `profile.json` — a fonte
  única de verdade agora é real.
- O botão Apply passa a ser envolvido em `pcall` e grava o erro em
  `performanceprofile-*.log.erros` em vez de morrer em silêncio.

Instalador (`installer/Install-Profile.ps1`):

- `powershell -File Install-Profile.ps1` **não funcionava**: com
  `[CmdletBinding()]`, o PowerShell 5.1 não popula `$PSScriptRoot` nos
  defaults do `param`. O default passou a ser resolvido no corpo do script.
- O `-DryRun` apagava arquivos de debris. A limpeza só ocorre sem `-DryRun`.
- As opções de lista agora têm `value` **e** `index` reconciliados. Antes o
  instalador escrevia só `value`, e os dois campos podiam divergir (ex.:
  `HDDMode` com `value="On"` e `index=2`, que aponta para "Auto").

### Adicionado

- **Validação das chaves de cvar contra o executável**: o instalador extrai
  os tokens ASCII do `Cyberpunk2077.exe` e confere as 57 chaves do perfil
  (~2 s). Antes, erros de digitação passavam despercebidos — foi assim que
  `EnableAirResistane` entrou no perfil.
- **4 cvars que o motor lê e o perfil ignorava**: `DPL`,
  `ExposureScaleBuffer`, `ExtendedDuringSSR` e `HairClears`, todos em
  `Rendering/AsyncCompute` (passes de async compute — custo puro numa GPU
  integrada). Descobertos pelo hook `HookGameOptionInit` do CET.
- `profile/profile.schema.json`: o schema que o perfil referenciava e não
  existia. O perfil passa a ser validado contra ele.
- Passo `[4/4]` no instalador: sincroniza o mod e o `profile.json` para a
  pasta de mods do CET, e limpa os logs de sessões anteriores.
- Novas flags: `-SkipBinaryCheck` (pula o scan do executável) e `-NoMod`
  (não sincroniza o mod in-game).

### Mudado

- `EnableAirResistane` → `EnableAirResistance`. A grafia antiga não existe
  em nenhum lugar do jogo; foi herdada da `FeatureList` do ConfigOverhaul.
- Versão do perfil: `0.1.0` → `0.1.1-alpha`.

### Validação

Sessão instrumentada de 19 minutos (1061 amostras):

- **35 das 37 opções** do perfil intactas ao final da sessão; **13 das 13**
  opções de lista que divergem do default do jogo foram honradas.
- GPU: mediana 95%, p95 106%. CPU: mediana 4,2 de 12 núcleos. VRAM
  compartilhada: p95 2.504 MB. O gargalo é a GPU integrada.
- O modelo de "reversão silenciosa" foi refinado: o jogo **valida no load** e
  **descarrega a memória no exit**. Valor aplicado com o jogo fechado
  sobrevive; valor alterado por ferramenta com o jogo aberto é apagado.

### Validação (segunda sessão, com o perfil 0.1.1-alpha)

Sessão de 22 minutos com um mod de diagnóstico (`PerfProbe`) dentro do
jogo, 628 amostras de processo e 1261 amostras de FPS:

- **FPS em gameplay: mediana 24** (p25 22, p75 26, p95 30) — **91,4%** dos
  segundos abaixo do alvo de 30 FPS.
- **GPU: mediana 100%** (72,3% das amostras ≥ 95%), **CPU: mediana 5,1 de
  12 núcleos**. O limite é custo de fragmento, não de CPU.
- **34 das 37 opções** do perfil intactas. As 3 inertes: `EnableTelemetry`
  (reativado por política de conta) e `HDDMode`/`HDDMode2` (o motor computa
  o modo de disco a partir do armazenamento detectado, ignorando o
  valor e o índice).
- **Causa-raiz da morte silenciosa do botão Apply identificada:** o sandbox
  Lua do CET confina o `io` ao diretório do mod — `os.getenv` não existe,
  caminho absoluto e `..` são bloqueados, e `_G` é `nil`. O painel in-game
  não tem como escrever no jogo. Ver `docs/FINDINGS.md` §13.1.
- O painel le o `profile.json` corretamente (35 widgets registrados) e o
  `PerfProbe` mede FPS de forma confiável.

### Pendências

- A causa da morte silenciosa do botão Apply ainda não foi identificada —
  exige um boot do jogo para o diagnóstico (`PerfProbe`) rodar.
- Não está provado se o motor lê `value` ou `index` em runtime.
- Os ~25 toggles de HUD do schema continuam fora do perfil.
- A análise de xrefs do Ghidra (mapa de cvar → função C++) continua
  pendente: o script existe, o banco de dados não.

[0.1.1-alpha]: https://github.com/CP2077-Performance/CP2077-Performance/compare/v0.1.0...v0.1.1_alpha
[0.1.0]: https://github.com/CP2077-Performance/CP2077-Performance/releases/tag/v0.1.0
