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

Substitui o instalador de linha de comando e o painel in-game por um
aplicativo desktop. A troca não é estética: a investigação mostrou que o
painel in-game **nunca conseguiu aplicar nada**, e o aplicativo assume o
papel com acesso real aos arquivos.

### Adicionado

**Aplicativo desktop** (`app/`, `build.cmd`)
- WinForms sobre .NET 10, publicado *self-contained*: um `.exe`, sem runtime
  a instalar. `build.cmd framework-dependent` publica a variante pequena.
- Núcleo (`CP2077Performance.Core`) sem dependência de interface, verificável
  sem abrir janela.
- Segue o **idioma** do sistema (recurso neutro em inglês, satellite `pt-BR`)
  e o **tema** (claro/escuro), com escala por monitor.
- O usuário informa a **pasta do jogo**; todo o resto é derivado. Sem
  varredura de disco e sem lógica por loja: o diretório de save é definido
  pela CD PROJEKT RED, não pela loja, então uma build serve Steam, Epic e GOG.
- **Validação** de cada valor de menu contra o esquema do jogo e de cada chave
  de cvar contra as strings do executável. Erro impede a aplicação.
- **Simulação** que lista o que seria alterado sem escrever nada.
- **Escrita cirúrgica**: só os campos de valor e de índice das opções
  visadas são tocados; o resto do arquivo fica byte a byte igual.
- **Backup automático** antes de alterar, com retenção das **três versões mais
  recentes** (a quarta descarta a mais antiga, a quinta a segunda, e assim por
  diante) e restauração a partir de qualquer uma delas.
- **Verificação pós-escrita**: os arquivos são relidos e comparados com o
  esperado; divergência é reportada como falha, não como sucesso silencioso.
- **Registro de alterações** com a origem de cada valor (perfil, variante ou
  padrão do jogo).
- Abas: Perfil, Motor (60 cvars com o comentário do próprio jogo), Menu
  (desejado × em vigor), Backups e Registro.

**Perfil** (`profile/lowspec-30fps.json`, `profile/profile.schema.json`)
- Schema do formato de perfil, que o perfil referenciava e não existia.
- Seção `variants`, com a **variante de HUD** como opção liga/desliga:
  desliga minimapa, marcadores, nomes, barra de vida e rastreador, e liga os
  presets de HUD reduzido que o próprio jogo oferece. Desligada, as opções
  afetadas voltam ao `default_value` do esquema.
- 4 cvars novos em `Rendering/AsyncCompute` — `DPL`, `ExposureScaleBuffer`,
  `ExtendedDuringSSR` e `HairClears` — descobertos pelo hook
  `HookGameOptionInit` do CET, que expõe as opções que o motor registra.
- `EnableAirResistane` → `EnableAirResistance`: a grafia antiga não existe
  nem como ASCII nem como UTF-16 em nenhum lugar do jogo; era herdada da
  `FeatureList` do ConfigOverhaul.

**Verificações** (`app/CP2077Performance.Tests`)
- Runner próprio, sem dependência de pacote externo, para que o projeto
  construa e teste offline. 45 verificações cobrindo localização, validação
  de schema, tokens do executável, escrita cirúrgica, variantes e backup.

### Removido (quebra)

- **O instalador de linha de comando** (`installer/Install-Profile.ps1`).
  A lógica dele foi portada para o núcleo do aplicativo; o que se perde é
  a automação por script, e o que se ganha é validação, backup versionado e
  estado efetivo visível.
- **O painel in-game** (`MODS → Low-Spec 30 FPS`). O sandbox Lua do Cyber
  Engine Tweaks confina o `io` ao diretório do mod: `os.getenv` não existe, e
  caminho absoluto ou com `..` são bloqueados. As três falhas registradas em
  log tinham a mesma causa — o botão Apply morria em
  `os.getenv("LOCALAPPDATA")`, sem log e sem erro no log do CET. Além disso,
  o painel podia **mentir**: exibia o valor desejado, não o valor em vigor.

### Validação

Aplicação real contra a instalação do jogo, com o jogo fechado:

```
UserSettings.json   2768 linhas -> 2768 linhas, 26 alteradas
                    (todas campos "value" ou "index")
user.ini            5 seções -> 15 seções, ordem das 5 originais preservada
                    e todos os comentários do motor intactos
config.json         3 patches mudados, os outros 2 intocados
```

Sessão instrumentada de 19 minutos: GPU com mediana 95% (72% das amostras
acima de 95%), CPU com mediana 4,2 de 12 núcleos, VRAM compartilhada com
mediana 2.528 MB, e **FPS com mediana 24 em gameplay** contra 29 no menu.
**O limite é custo de fragmento na GPU, não CPU.**
[0.1.1-alpha]: https://github.com/CP2077-Performance/CP2077-Performance/compare/v0.1.0...v0.1.1_alpha
[0.1.0]: https://github.com/CP2077-Performance/CP2077-Performance/releases/tag/v0.1.0
