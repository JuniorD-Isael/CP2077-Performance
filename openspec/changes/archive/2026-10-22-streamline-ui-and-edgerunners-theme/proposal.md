# Proposal

## Why

A interface principal ainda expõe duas áreas desnecessárias e confusas — a seção "O que aplicar" (com caixa de seleção de tela cheia e lista de monitores/resoluções da máquina atual, que força uma resolução específica no jogo e limita ou extrapola o setup de outros usuários) e o botão "Mostrar detalhe técnico" (cujas 5 abas de tabelas pertencem à documentação do projeto, não à tela do aplicativo) — além de apresentar um visual Material genérico que não reflete a identidade de *Cyberpunk: Edgerunners*. O aplicativo deve aplicar sempre o perfil de desempenho junto com o modo de janela em tela cheia (`WindowMode = Fullscreen`), sem alterar a resolução escolhida pelo usuário no jogo e sem pedir confirmação extra, em uma interface enxuta inspirada na arte promocional de *Cyberpunk: Edgerunners*.

## What Changes

- **BREAKING**: Remover da tela principal a seção **"O que aplicar"** (o cartão de escolha de modo de exibição, o seletor de monitores com resoluções nativas e a área de variantes) e aplicar sempre `WindowMode = "Fullscreen"` (`index = 2`) junto com o perfil ao acionar **Aplicar perfil**, preservando intacta a `Resolution` configurada pelo usuário no jogo (mesmo quando menor ou maior que a do monitor).
- **BREAKING**: Remover a enumeração de monitores (`DisplayModes`, `DisplayMonitor`), a persistência de seleção de monitor/resolução (`DisplaySelection`, `display-index.json`, `displayPrevious`) e qualquer escrita na opção `Resolution` do `UserSettings.json`.
- **BREAKING**: Remover o botão **"Mostrar detalhe técnico"** e o painel expansível com as 5 abas (`Perfil`, `Motor`, `Menu`, `Backups` e `Registro`) da interface do aplicativo, transferindo a referência completa das opções de menu, cvars do motor e patches do CET para a documentação (`README.md`, `README.en.md` e `docs/`).
- Redesenhar a identidade visual da janela principal inspirada na arte promocional de *Cyberpunk: Edgerunners*:
  - Nova paleta **Edgerunners**: amarelo ácido cyber (`#FCEE0A`) como cor de destaque estrutural e ação principal, azul-petróleo abissal de Night City (`#04101C` / `#081B2B`) nas superfícies escuras, verde neon Sandevistan (`#00FF66`) para estado ativo/sucesso, vermelho cyberware (`#FF334B`) para erro/alerta e superfície clara (`#F4F7FA`).
  - Substituir os cartões arredondados do Material (`CornerRadius = 12`) por painéis e botões geométricos com **cantos chanfrados em 45°**, cabeçalho estilo HUD *Edgerunners* com faixa amarela, linhas diagonais técnicas e marcadores laterais.

## Capabilities

### New Capabilities

<!-- Nenhuma capacidade nova; o comportamento é refinado nas capacidades existentes. -->

### Modified Capabilities

- `display-mode`: deixa de enumerar monitores, de oferecer escolha na interface e de sobrescrever `Resolution`; passa a aplicar sempre `WindowMode = "Fullscreen"` (`index = 2`) junto com o perfil, preservando qualquer `Resolution` e escala dinâmica de resolução configuradas no jogo.
- `desktop-interface`: remove a seção "O que aplicar" (escolha de exibição/monitor e lista de variantes) e o botão/painel "Mostrar detalhe técnico" (abas `Perfil`, `Motor`, `Menu`, `Backups`, `Registro`), mantendo a tela principal enxuta (cabeçalho HUD, pasta do jogo, resumo/estado, aplicar perfil, restaurar último backup e resultado) e substituindo a base Material arredondada pela estética geométrica chanfrada e paleta de *Cyberpunk: Edgerunners*.

## Impact

- **Núcleo (`app/CP2077Performance.Core`)**:
  - `Display/`: simplificação de `DisplayOptions` para produzir sempre `WindowMode = "Fullscreen"` (`index = 2`) sem tocar em `Resolution`; remoção de `DisplayModes.cs`, `DisplayMonitor.cs` e `DisplayIndexMap.cs`.
  - `Application/ProfileApplier.cs`: aplica e verifica `WindowMode = "Fullscreen"` automaticamente quando a opção existe no `UserSettings.json`, sem alterar `Resolution`.
- **Interface (`app/CP2077Performance.Ui`)**:
  - `MainForm.cs` e `AppState.cs`: remoção de `BuildChoices()` (seletor de monitor, checkbox de tela cheia e cartões de variante) e de `BuildDetails()` (botão `Mostrar detalhe técnico` e as 5 abas `DataGridView`); card único de status/resumo do perfil.
  - `Theme/CyberpunkPalette.cs` e `Theme/MaterialTheme.cs`: atualização das cores-base para a paleta *Cyberpunk: Edgerunners* (`#FCEE0A`, `#04101C`, `#00FF66`, `#F4F7FA`), recortes chanfrados em 45°, moldura/cabeçalho HUD com detalhes geométricos e remoção de `MaterialChoiceCard.cs`.
- **Testes (`app/CP2077Performance.Tests`)**:
  - Atualização de `DisplayTests.cs`, `DisplayApplyTests.cs` e `ThemeTests.cs` para validar a aplicação incondicional de `WindowMode = "Fullscreen"` com preservação de `Resolution`, a ausência do seletor de monitor e do detalhe técnico, e a nova paleta/geometria *Edgerunners*.
- **Documentação (`README.md`, `README.en.md`, `docs/ARQUITETURA.md`, `CHANGELOG.md`)**:
  - Inclusão das tabelas técnicas completas (opções do `UserSettings.json`, cvars do `user.ini` com descrições do motor e patches do `config.json`) anteriormente exibidas nas abas do detalhe técnico.
