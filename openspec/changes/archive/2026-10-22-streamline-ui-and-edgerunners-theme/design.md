# Design

## Context

Ver `proposal.md` — Why para a motivação. Atualmente:
- `Core/Display/` contém `DisplayModes.cs`, `DisplayMonitor.cs`, `DisplayIndexMap.cs`, `DisplayModeIndex.cs` e `DisplayOptions.cs`, que enumeram monitores do Windows via `EnumDisplaySettings` e gravam a resolução nativa do monitor na opção `Resolution` do `UserSettings.json`.
- `MainForm.cs` constrói 6 linhas no `TableLayoutPanel` raiz: `BuildPathRow`, `BuildOrientation`, `BuildChoices` ("O que aplicar", com lista de variantes, checkbox de tela cheia e `ComboBox` de monitores), `BuildActions` (`Aplicar perfil`, `Restaurar último backup` e `Mostrar detalhe técnico`), `BuildResult` e `BuildDetails` (5 abas `DataGridView`: `Perfil`, `Motor`, `Menu`, `Backups` e `Registro`).
- `Theme/CyberpunkPalette.cs` e `Theme/MaterialTheme.cs` usam cantos arredondados (`BaseCornerRadius = 12`) e uma paleta com primária roxa (`#6564db`) que não remete à identidade visual de *Cyberpunk: Edgerunners*.

## Goals / Non-Goals

**Goals:**
- Garantir que `ProfileApplier.Plan` e `ProfileApplier.Apply` incluam sempre `WindowMode = "Fullscreen"` (`index = 2`) quando `WindowMode` existir em `UserSettings.json`, sem tocar na opção `Resolution` e sem enumerar monitores do sistema.
- Enxugar `MainForm` e `AppState` removendo a seção "O que aplicar" (checkbox de tela cheia, `ComboBox` de monitores e cartões de variante) e o botão/painel "Mostrar detalhe técnico" (as 5 abas `DataGridView`).
- Redesenhar o tema visual (`CyberpunkPalette` e `MaterialTheme`) com a estética da arte promocional de *Cyberpunk: Edgerunners*: paleta amarelo ácido (`#FCEE0A`), azul-petróleo escuro (`#04101C`), verde neon Sandevistan (`#00FF66`) e superfície clara (`#F4F7FA`), cartões e botões com recorte poligonal chanfrado em 45° e cabeçalho estilo HUD.
- Documentar em `README.md`, `README.en.md` e `docs/ARQUITETURA.md` as tabelas completas de opções de menu, cvars do motor (com os comentários do próprio jogo) e patches do CET que antes apareciam nas abas técnicas.

**Non-Goals:**
- Não alterar os valores do perfil curado `profile/lowspec-30fps.json` nem o mecanismo interno de `VariantResolver` no `Core`.
- Não introduzir bibliotecas externas de UI ou dependências de terceiros (continua 100% WinForms + GDI+ em `System.Drawing.Drawing2D`).

## Decisions

### D1: Aplicação incondicional de `WindowMode = "Fullscreen"` (`index = 2`) sem alterar `Resolution`

- **Escolha**: `DisplayOptions` passa a expor `DisplayOptions.FullscreenOnly` (`WindowMode = "Fullscreen"`, `WindowModeIndex = 2`, `Resolution = ""`, `ResolutionIndex = null`) como padrão usado automaticamente por `ProfileApplier.Plan`, `ProfileApplier.Apply` e `AppState.ReadAppliedState`. Os arquivos `DisplayModes.cs`, `DisplayMonitor.cs` e `DisplayIndexMap.cs` são removidos.
- **Racional**: Conforme medido em `docs/FINDINGS.md` §15.3–§15.4, a lista interna do motor para `WindowMode` é fixa no próprio jogo (`Windowed = 0`, `BorderlessWindowed = 1`, `Fullscreen = 2`) e o motor resolve pelo `value`. Escrever `WindowMode = "Fullscreen"` com `index = 2` sem tocar em `Resolution` garante que o jogo abra em tela cheia respeitando qualquer resolução que o usuário tenha escolhido no menu do jogo (ex.: `1366x768` num monitor `1600x900` ou `1920x1080`), sem extrapolar nem limitar setups de outros monitores.
- **Alternativas descartadas**:
  - *Continuar gravando a resolução nativa do monitor em `Resolution`*: descartado porque força o jogo a sair da resolução escolhida pelo usuário e amarra o comportamento ao monitor detectado.
  - *Colocar `WindowMode` dentro de `profile/lowspec-30fps.json`*: `WindowMode` é declarada no `options.json` do jogo com `"is_dynamic": true` e sem array `values`, portanto `ProfileValidator` recusaria uma opção `string_list` sem `values` no schema estático; mantê-la em `DisplayOptions` preserva a validação estrita do schema para `lowspec-30fps.json`.

### D2: Estrutura enxuta da `MainForm` (sem "O que aplicar" e sem "Mostrar detalhe técnico")

- **Escolha**: A janela principal passa a ter altura fixa compacta e 5 blocos verticais:
  1. **Cabeçalho HUD *Edgerunners*** (`BuildHeader`): faixa superior com barra lateral/superior em amarelo ácido (`#FCEE0A`), título em caixa alta, subtítulo do perfil (`Low-Spec 30 FPS`) e detalhe linear verde neon (`#00FF66`).
  2. **Linha da pasta do jogo** (`BuildPathRow`): rótulo, caixa de caminho, botões `Procurar...` e `Validar`.
  3. **Painel de orientação de primeira execução** (`BuildOrientation`): visível apenas quando nenhuma instalação válida está configurada.
  4. **Cartão de resumo e status** (`BuildStatusCard`): cartão chanfrado exibindo a explicação em linguagem corrente do que o aplicativo aplica (perfil de desempenho + tela cheia mantendo a resolução do jogo), a contagem de configurações que serão alteradas (`_effectLabel`) e o estado atual no disco / divergências / aviso de mod antigo (`_stateLabel`).
  5. **Barra de ações e resultado** (`BuildActions` + `BuildResult`): botão primário em destaque `Aplicar perfil` (`ButtonVariant.Filled` em `#FCEE0A` com texto `#04101C`), botão secundário `Restaurar último backup` (`ButtonVariant.Outlined`) e cartão de resultado da operação.
- **Racional**: Remove toda a carga cognitiva de opções redundantes e tabelas técnicas que não mudam o fluxo de uso do aplicativo, deixando as duas únicas ações reais (`Aplicar perfil` e `Restaurar último backup`) imediatamente visíveis.

### D3: Identidade visual *Cyberpunk: Edgerunners* (paleta + geometria chanfrada)

- **Escolha**:
  - **Quatro cores-base em `CyberpunkPalette`**:
    - `BaseLight = #F4F7FA` (texto principal sobre fundo escuro / fundo da variante clara)
    - `BasePrimary = #FCEE0A` (amarelo ácido *Edgerunners* — preenchimento da ação principal, moldura do cabeçalho e contornos em destaque)
    - `BaseAccent = #00FF66` (verde neon Sandevistan — indicadores de estado aplicado/sucesso e linha de energia do cabeçalho)
    - `BaseDark = #04101C` (azul-petróleo profundo da silhueta de Night City — fundo escuro e texto sobre preenchimento amarelo)
  - **Contraste sobre amarelo ácido (`#FCEE0A`)**: Como `#FCEE0A` tem alta luminância ($\approx 0{,}83$), o texto sobre `Primary` (`OnPrimary`) usa `BaseDark` (`#04101C`), alcançando razão de contraste $> 15:1$ (muito acima de 4,5:1). Na variante clara, `PrimaryText` e `Outline` usam tons escurecidos derivados por `EnsureContrast` para manter contraste $\ge 4{,}5:1$ sobre `#F4F7FA`.
  - **Geometria chanfrada em 45° (`ChamferedPath`)**: Em `MaterialTheme`, substituir `RoundedPath` (arcos circulares) por um polígono chanfrado com corte de 45° nos cantos superior-esquerdo e inferior-direito (`BaseCornerRadius = 10` convertido em tamanho do chanfro), além de marcadores técnicos laterais e faixa de acento à esquerda nos cartões (`MaterialCard`).

## Risks / Trade-offs

- **[Risk] Testes existentes em `ThemeTests.cs` verificam as hexadecimais antigas (`#6564db`, `#000d25`, `#f7f7ff`, `#85ff9e`) e `OnPrimary == OnSurface`** → **Mitigation**: Atualizar `ThemeTests.cs` para verificar as 4 novas cores-base (`#f4f7fa`, `#fcee0a`, `#00ff66`, `#04101c`), o contraste de `OnPrimary` (`#04101c`) sobre `Primary` (`#fcee0a`) e a ausência dos controles removidos na `MainForm`.
- **[Risk] Usuários avançados que consultavam a lista de cvars ou opções pela aba técnica** → **Mitigation**: Documentar integralmente as tabelas de opções de menu, cvars do motor e patches do CET em `README.md`, `README.en.md` e `docs/ARQUITETURA.md`.
