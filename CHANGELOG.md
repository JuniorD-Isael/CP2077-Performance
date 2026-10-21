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

[0.1.0]: https://github.com/CP2077-Performance/CP2077-Performance/releases/tag/v0.1.0
