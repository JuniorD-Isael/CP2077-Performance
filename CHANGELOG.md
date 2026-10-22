# Changelog

Todas as versões notáveis deste projeto.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e
o versionamento segue [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]

O aplicativo passa a cuidar exclusivamente do **setup de performance** e de
**forçar tela cheia (`WindowMode = Fullscreen`) preservando a resolução escolhida
pelo usuário no jogo**, com uma interface enxuta inspirada na arte promocional de
**Cyberpunk: Edgerunners**. Toda manipulação e alternância de HUD saem do
aplicativo para um projeto separado (`handoff/hud-toggle/`).

### Adicionado

- **Identidade visual *Cyberpunk: Edgerunners*** (`CyberpunkPalette`,
  `MaterialTheme`, `MainForm`): paleta de quatro cores-base (`#04101c` azul-petróleo
  abissal de Night City, `#fcee0a` amarelo ácido, `#00ff66` verde neon
  *Sandevistan* e `#f4f7fa` branco-gelo), rótulo escuro sobre amarelo ácido com
  contraste 16,57:1, recortes poligonais chanfrados em 45° (`ChamferedPath`),
  barras laterais de destaque e cabeçalho estilo HUD (`MakeEdgerunnersHeader`).
- **Referência técnica na documentação** (`docs/ARQUITETURA.md` §10): tabelas
  completas das 38 opções de `UserSettings.json`, 60 cvars de `user.ini` e 4
  patches de `config.json`.

### Removido (**BREAKING**)

- **Seção "O que aplicar" e seletor de monitor/resolução**: removidos
  `_displayCard`, `_displayCheck`, `_displayMonitorBox`, `DisplayModes`,
  `DisplayMonitor`, `DisplayIndexMap` e `MaterialChoiceCard`. O aplicativo não
  enumera mais monitores nem sobrescreve `Resolution`.
- **Botão "Mostrar detalhe técnico" e as 5 abas de auditoria**: removidos
  `_revealButton`, `_detailsPanel` e `_tabs` (`Perfil`, `Motor`, `Menu`,
  `Backups`, `Registro`) da janela principal, transferindo a referência técnica
  para `docs/ARQUITETURA.md`.
- **Alternância de HUD em jogo**: removidos `HudToggle`, `GlobalHotkey`, a
  presença na bandeja, a instalação do mod Lua (`mod/CP2077Performance/init.lua`)
  e os controles de HUD da tela principal.
- **Variante `hudMinimal`**: removida de `profile/lowspec-30fps.json`. O perfil
  curado deixa de declarar qualquer opção que altere o HUD do jogo. O bloco e o
  trabalho da change `toggle-hud-in-game` foram preservados em
  `handoff/hud-toggle/`.

### Alterado

- **Tela cheia automática preservando a resolução do usuário**: ao aplicar o
  perfil, o aplicativo grava sempre `WindowMode = Fullscreen` (`index = 2`) sem
  alterar `Resolution`, preservando qualquer resolução configurada no jogo
  (`1366x768`, `1280x720`, `1600x900`, `1920x1080`, etc.) em qualquer monitor.
- **Mod antigo reportado**: `LeftoverPanelDetector` passa a detectar (sem
  apagar) o diretório `cyber_engine_tweaks\mods\CP2077Performance` e a tela
  principal informa que o mod pertence a uma versão anterior e agora vive em
  projeto separado.
- **Variante desconhecida ignorada**: `VariantResolver` ignora sem erro um id
  de variante salvo que o perfil não declara mais.

## [0.3.0-alpha] — 2026-10-21

O jogo passa a abrir **em tela cheia, na resolução do monitor**, mesmo com a
resolução interna menor. E a primeira vez que o aplicativo mexe nas opções
de exibição — as duas que o motor monta em runtime, e que o esquema do jogo
não descreve.

### Adicionado

**Modo de exibição** (`app/CP2077Performance.Core/Display`, `app/CP2077Performance.Ui`)
- O aplicativo enumera os **monitores** do sistema, com a resolução nativa
  de cada um, e oferece o monitor em que o jogo roda. O principal do sistema
  vem primeiro e é o padrão.
- A escolha **"Abrir o jogo em tela cheia na resolução do monitor"** escreve
  `WindowMode = Fullscreen` e `Resolution = <nativa do monitor>`.
- A **resolução interna não muda**: a escala dinâmica de resolução continua
  variando entre 50% e 85%, como antes.
- O **índice** das opções dinâmicas é **aprendido com o próprio jogo**: o
  aplicativo lê o par `(valor, índice)` que o motor gravou e o guarda em
  `display-index.json`. Quando o índice de uma resolução é conhecido, ele
  escreve o valor e o índice; quando não, escreve o valor — e o motor
  corrige o índice no próximo início.
- **Desligar a escolha devolve** as opções de exibição que o jogo tinha
  antes de o aplicativo mexer nelas. A primeira vez que ele mexe, guarda o
  estado anterior.

**Documentação**
- `docs/EXPERIMENTO-OPCOES-DE-EXIBICAO.md` — o procedimento de descoberta do
  contrato das opções dinâmicas.
- `docs/FINDINGS.md` §15 — o que o motor resolve, e como, medido em runtime.
- `docs/ARQUITETURA.md` §9 — a receita do modo de exibição.

### Validado

Medição em runtime, com o jogo iniciado e encerrado:

```
antes     WindowMode = Fullscreen    index = 0
          Resolution = 1600x900      index = 2

depois    WindowMode = Fullscreen    index = 2
          Resolution = 1600x900      index = 4
```

O motor **resolve pelo valor** e recalcula o índice. Com a configuração
aplicada, a janela do jogo mediu **1600x900 @ 0,0** — a área de trabalho
inteira do monitor, onde antes era uma janela de 1366×768 sem borda.

Ciclo completo verificado contra a instalação real: ligando a escolha, as
opções vão para `Fullscreen`/`1600x900`; desligando, voltam para
`BorderlessWindowed`/`1366x768`. A escrita é cirúrgica — 2768 linhas antes e
depois, e só os campos de valor e índice das opções visadas são tocados.

75 verificações passam, incluindo as da enumeração de monitores, do índice
das opções dinâmicas e da escrita com índice externo.

## [0.2.0-alpha] — 2026-10-21

A interface deixa de ser um painel de auditoria e passa a ser uma tela de
decisões. A ação **Simular** sai, a base visual entra, e a variante passa a
alcançar as duas camadas de configuração.

### Adicionado

**Tela principal** (`app/CP2077Performance.Ui`)
- A tela principal deixa de ser um painel de opções e passa a oferecer
  **escolhas**: a lista de variantes com rótulo e resumo, o **estado** da
  aplicação e as ações de **aplicar** e **reverter**.
- O **estado** é lido do disco, nunca da memória: se o perfil está aplicado,
  desde quando, e quantas configurações divergem do que o jogo está usando de
  fato. O motor recalcula parte dos valores — `HDDMode`, `HDDMode2` e
  `EnableTelemetry` —, e um estado guardado em memória diria "aplicado" para
  algo que o jogo já mudou.
- O efeito da escolha é explicado em **linguagem corrente**, com a contagem de
  configurações que serão alteradas.
- **Primeira execução** orienta para a pasta do jogo, com o caminho típico de
  cada loja, em vez de mostrar grades vazias.
- O resultado de cada operação recebe **rótulo próprio** para sucesso, falha
  parcial e erro.
- A fila de operações ficou com **Aplicar** e **Restaurar**. Descartar edições
  do perfil migrou para a revelação do detalhe técnico.

**Base Material com paleta Cyberpunk** (`app/CP2077Performance.Ui/Theme`)
- Camada de tema com papéis de cor, escala tipográfica, espaçamento de 8 px,
  raio de canto, elevação e camada de estado, construída **sobre os controles
  padrão do WinForms, sem nenhuma dependência externa**.
- Paleta de quatro cores — `f7f7ff`, `6564db`, `85ff9e`, `000d25` — em
  variante clara e escura, escolhida pela preferência de aplicativo do
  sistema. As três cores cromáticas formam um triângulo: o destaque é a
  rotação de −120° da principal, e o papel de erro é a rotação de +120°.
- A preferência do sistema continua escolhendo o **modo**; a **cor** vem da
  paleta do aplicativo. A moldura nativa da janela continua seguindo o
  sistema.
- O cartão de variante é desenhado à mão e exposto a leitores de tela como
  caixa de seleção, com rótulo e resumo.

**Variantes nas duas camadas** (`profile/`, `app/CP2077Performance.Core`)
- `variants.<id>` passou a aceitar `userIni` (seção → chave → valor), `summary`
  e `highlights`, junto de `label` e `description`.
- Uma variante agora alcança **as duas camadas**: opções de menu e chaves de
  cvar do motor. Sem isso, a escolha do usuário só mexeria nas 37 opções de
  menu, que são a menor parte do ganho.
- Ao desligar uma variante, as chaves de cvar que ela acrescentou **são
  removidas** — o padrão do jogo para uma cvar é a ausência da chave. A
  remoção só ocorre quando o valor em vigor ainda é exatamente o da variante;
  se o usuário ajustou à mão, a chave fica e a divergência é reportada.
- A curadoria do perfil ganhou `summary` e `highlights` da variante de HUD, em
  linguagem corrente.

**Documentação**
- `docs/ARQUITETURA.md` — o material de desenvolvimento que saiu do README:
  derivação de caminho, escrita cirúrgica, o modelo de opção do jogo, as
  medições, a proveniência dos valores e a base Material.
- `README.en.md` — a versão completa em inglês, separada do README principal.

### Removido (quebra)

- **A ação Simular.** A pré-visualização de "antes → depois" saía da tela
  principal. A informação não se perde: a tela principal mostra quantas
  configurações mudam, e a revelação do detalhe técnico mostra valor em
  vigor e valor desejado lado a lado, com o estado real do jogo. Perde-se o
  botão, não o dado. O cálculo do plano continua no motor — `Apply()` depende
  dele, e a contagem de alterações vem dele.

### Mudado (quebra)

- **A tela principal mudou de forma.** As cinco abas (Perfil, Motor, Menu,
  Backups, Registro) foram para uma **revelação explícita**, atrás do botão
  *Mostrar detalhe técnico*, intactas — com as colunas de procedência e a
  identificação de seção e chave de cvar preservadas. A linha de alvo do
  hardware, as linhas de origem das camadas e a nota sobre a limitação da
  validação por token saíram da tela principal e foram para a revelação.
- **O formato da variante** aceita campos novos. Os campos são aditivos, e um
  perfil gravado no formato anterior — variante sem resumo e sem chaves de
  cvar — continua sendo lido e aplicado.

### Corrigido

- A recusa por jogo em execução **orienta como recuperar**: quando o processo
  persiste depois do jogo fechado, a mensagem diz para encerrá-lo pelo
  Gerenciador de Tarefas. Antes, a recusa não tinha saída.

### Validação

Aplicação real contra a instalação do jogo, com o jogo fechado:

```
UserSettings.json   2768 linhas -> 2768 linhas, 1 alterada (só "value")
user.ini            intacto
config.json         intacto
```

Variante ligada: 23 linhas alteradas, **todas** pertencentes à variante
`hudMinimal`. Variante desligada: as 23 voltaram ao `default_value` do
esquema, e o arquivo ficou **byte a byte igual** (SHA-256 idêntico) ao estado
anterior à variante.

61 verificações passam, incluindo as da paleta: os papéis de texto têm
contraste ≥ 4.5:1 sobre o fundo nas duas variantes.

### Conhecido

- Com o preenchimento da ação principal em `6564db`, **nenhum** par das quatro
  cores-base alcança 4.5:1 com o rótulo: o mais alto é `f7f7ff` sobre
  `6564db`, que dá 4.48:1. O preenchimento mantém a cor exata da paleta, e
  esse é o teto que ela permite. Onde a principal aparece **como texto**, um
  tom derivado é usado, e esse alcança 4.5:1.

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
[0.2.0-alpha]: https://github.com/CP2077-Performance/CP2077-Performance/compare/v0.1.1_alpha...v0.2.0_alpha
[0.1.1-alpha]: https://github.com/CP2077-Performance/CP2077-Performance/compare/v0.1.0...v0.1.1_alpha
[0.1.0]: https://github.com/CP2077-Performance/CP2077-Performance/releases/tag/v0.1.0
