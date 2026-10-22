# Tasks

## 1. Tela cheia automática sem sobrescrever a resolução

- [x] 1.1 Simplificar `Core/Display/` para aplicar sempre `WindowMode = "Fullscreen"` com `index = 2` (`DisplayOptions.FullscreenOnly`), sem alterar `Resolution`, e remover `DisplayModes.cs`, `DisplayMonitor.cs` e `DisplayIndexMap.cs`. Verificação: `dotnet build app/CP2077Performance.Core/CP2077Performance.Core.csproj` compila sem erro e `Get-ChildItem app/CP2077Performance.Core/Display` lista apenas `DisplayModeIndex.cs` e `DisplayOptions.cs`.
- [x] 1.2 Fazer `ProfileApplier.Plan` e `ProfileApplier.Apply` usarem `DisplayOptions.FullscreenOnly` como padrão quando `display` não for informado, e fazer `AppState` incluir `WindowMode = "Fullscreen"` na contagem de alterações pendentes e na leitura de estado aplicado, removendo `DisplaySelection`, `Monitors`, `DisplayIndexes` e `PreviousDisplayOptions`. Verificação: testes unitários em `DisplayTests.cs` e `DisplayApplyTests.cs` confirmam que aplicar o perfil escreve `WindowMode = "Fullscreen"` (`index = 2`) e mantém intacto qualquer valor de `Resolution` (`1366x768`, `1280x720`, `1920x1080`, etc.).

## 2. Identidade visual Cyberpunk: Edgerunners

- [x] 2.1 Atualizar `Ui/Theme/CyberpunkPalette.cs` para as quatro cores-base *Edgerunners* (`BaseLight = #f4f7fa`, `BasePrimary = #fcee0a`, `BaseAccent = #00ff66`, `BaseDark = #04101c`), com `OnPrimary = BaseDark` sobre o amarelo ácido e derivação de contraste $\ge 4{,}5:1$ nas variantes escura e clara. Verificação: testes em `ThemeTests.cs` conferem as 4 cores-base, os contrastes WCAG e a derivação das superfícies.
- [x] 2.2 Atualizar `Ui/Theme/MaterialTheme.cs` para recortar cartões e botões com polígono chanfrado em 45° (`ChamferedPath`), estilizar contornos e marcadores visuais estilo HUD *Edgerunners* e remover `Ui/Theme/MaterialChoiceCard.cs`. Verificação: `dotnet build app/CP2077Performance.Ui/CP2077Performance.Ui.csproj` compila sem erro e o caminho geométrico gerado por `MaterialTheme` possui vértices chanfrados.

## 3. Enxugar a janela principal (`MainForm`)

- [x] 3.1 Remover de `Ui/MainForm.cs`, `Ui/Strings.resx` e `Ui/Strings.pt-BR.resx` a seção "O que aplicar" (`BuildChoices`, `_displayCard`, `_displayCheck`, `_displayMonitorBox`, `_variantsPanel`) e o botão/painel "Mostrar detalhe técnico" (`_revealButton`, `_detailsPanel`, `_tabs` e as 5 abas `DataGridView`). Verificação: `Select-String -Path app/CP2077Performance.Ui/MainForm.cs -Pattern '_displayMonitorBox|_revealButton|_detailsPanel|BuildDetails|BuildChoices'` não retorna nada.
- [x] 3.2 Construir em `Ui/MainForm.cs` o cabeçalho visual *Edgerunners* (`BuildHeader`) e o cartão chanfrado de status e resumo do perfil (`BuildStatusCard`), mantendo apenas a linha da pasta do jogo, a orientação de primeira execução, o resumo/estado, os botões `Aplicar perfil` e `Restaurar último backup` e o cartão de resultado. Verificação: teste unitário em `ThemeTests.cs` renderiza a `MainForm`, confirma a ausência de `ComboBox`, `CheckBox` e `TabControl` e gera captura de tela da nova UI.

## 4. Documentação técnica e verificação final

- [x] 4.1 Transferir as informações técnicas que saíram do botão "Mostrar detalhe técnico" (tabela das opções de menu de `UserSettings.json` + `WindowMode = Fullscreen`, tabela das 60 cvars de `user.ini` com descrições do motor e os 3 patches de `config.json`) para `README.md`, `README.en.md`, `docs/ARQUITETURA.md` e registrar a mudança em `CHANGELOG.md`. Verificação: os documentos listam as três camadas completas e descrevem que a resolução do usuário no jogo é preservada.
- [x] 4.2 Executar a suíte completa de testes, publicar o executável (`build.cmd`) e validar a change com `openspec validate streamline-ui-and-edgerunners-theme`. Verificação: todos os testes de `CP2077Performance.Tests` passam, `dist\CP2077-Performance.exe` é gerado e `openspec validate` passa sem erros.

## Workflow follow-up

- Arquivar a change `streamline-ui-and-edgerunners-theme` após a revisão da nova interface.
