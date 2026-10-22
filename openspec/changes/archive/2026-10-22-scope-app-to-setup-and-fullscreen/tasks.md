# Tasks

## 1. Preparação e handoff do trabalho de HUD

- [x] 1.1 Arquivar a change `fullscreen-at-native-resolution` (concluída, 18/18) para que a spec `display-mode` exista em `openspec/specs/`. Verificação: `openspec list --specs` lista `display-mode`.
- [x] 1.2 Criar `handoff/hud-toggle/` com cópia de `openspec/changes/toggle-hud-in-game/`, `Core/GameIntegration/HudToggle.cs`, `Ui/GlobalHotkey.cs`, `Tests/HudToggleTests.cs` e `mod/CP2077Performance/init.lua`. Verificação: todos os arquivos existem na pasta e `diff` contra a origem não mostra diferença.
- [x] 1.3 Copiar para `handoff/hud-toggle/FINDINGS-HUD.md` as seções §13.1 e §14.4 do `docs/FINDINGS.md` e registrar ali o que o novo projeto precisa saber (sandbox do CET confina o `io`, sem tecla para mods na v1.37). Verificação: o arquivo cita os dois pontos e o `FINDINGS.md` continua intacto.
- [x] 1.4 Escrever `handoff/hud-toggle/README.md` com a lista do que foi movido, o que foi decidido (F8, intenção por arquivo) e o que ficou sem prova (as tarefas 1.1 a 1.5 da `toggle-hud-in-game`). Verificação: o README existe e cada arquivo da pasta aparece na lista.
- [x] 1.5 Copiar o bloco `variants.hudMinimal` de `profile/lowspec-30fps.json` para `handoff/hud-toggle/hudMinimal.variant.json`, antes de removê-lo (tarefa 2.6). Verificação: o arquivo é JSON válido e contém as opções `minimap` e `quest_tracker`.

## 2. Remover o HUD em jogo do aplicativo

- [x] 2.1 Apagar `Core/GameIntegration/HudToggle.cs`, `Ui/GlobalHotkey.cs`, `Tests/HudToggleTests.cs` e o diretório `mod/`. Verificação: `git status` mostra a remoção e o handoff (1.2) ainda contém as cópias.
- [x] 2.2 Remover de `Ui/AppState.cs`, `Ui/MainForm.cs`, `Ui/Strings.cs`, `Ui/Strings.resx`, `Ui/Strings.pt-BR.resx` e `Tests/Program.cs` toda referência a `HudToggle`, `GlobalHotkey`, `HudState` e às strings de HUD em jogo. Verificação: `Get-ChildItem app -Recurse -Include *.cs,*.resx | Select-String 'HudToggle|GlobalHotkey|HudState'` não retorna nada.
- [x] 2.3 Remover a cópia do `mod/` do build em `App/CP2077Performance.App.csproj` (item de conteúdo). Verificação: `dotnet build app/CP2077Performance.slnx` termina sem erro e `bin/**/mod` não é gerado.
- [x] 2.4 Executar a suíte de testes. Verificação: o executável de `CP2077Performance.Tests` termina com todos os testes passando e sem testes de HUD em jogo.
- [x] 2.5 Confirmar por teste que nenhuma tecla é registrada e que nada é escrito em `cyber_engine_tweaks\mods` ao aplicar o perfil. Verificação: teste unitário aplica o perfil numa pasta temporária de jogo e confere que esse diretório não existe depois.
- [x] 2.6 Remover `variants.hudMinimal` de `profile/lowspec-30fps.json`, após a tarefa 1.5. Verificação: o perfil valida contra `profile/profile.schema.json` e `Select-String hudMinimal profile` não retorna nada.
- [x] 2.7 Ajustar `Tests/ProfileTests.cs` para não depender de `hudMinimal` (usar uma variante de teste própria onde o mecanismo for exercitado) e cobrir o id de variante salvo e desconhecido, ignorado sem erro e sem escrita. Verificação: a suíte passa e o teste do id desconhecido existe.
- [x] 2.8 Reescrever `docs/ROADMAP.md` §2.0 e remover a menção a `hudMinimal` da documentação, mantendo o relatório de auditoria arquivado como está. Verificação: `Select-String hudMinimal README.md,README.en.md,docs/ROADMAP.md` não retorna nada.

## 3. Reportar o mod remanescente

- [x] 3.1 Estender `GameInstallation/LeftoverPanelDetector.cs` para reconhecer `cyber_engine_tweaks\mods\CP2077Performance` e devolver um achado distinto do painel in-game. Verificação: teste unitário com o diretório presente confere o achado, e a ausência confere nenhum achado.
- [x] 3.2 Apresentar o achado na tela principal com o texto de que o mod é de versão anterior e não é mais gerenciado, sem apagá-lo. Verificação: teste unitário confere que o diretório continua intacto, e captura de tela mostra o aviso.

## 4. Tela cheia independente da resolução

- [x] 4.1 Fazer `Display/DisplayOptions.cs` escrever sempre tela cheia e a resolução nativa do monitor quando a escolha está ligada, sem ler a resolução nem o modo de janela anteriores para decidir. Verificação: testes em `DisplayApplyTests.cs` cobrem os três cenários do spec (menor, igual, maior que a nativa) e passam.
- [x] 4.2 Recusar a escrita quando a resolução nativa não puder ser obtida, informando o motivo. Verificação: teste unitário com monitor indeterminado confere que nenhuma opção é escrita e que a mensagem aparece.
- [x] 4.3 Manter a escolha de desligar devolvendo as opções anteriores, e confirmar que a escala dinâmica de resolução, o piso e o teto não mudam. Verificação: teste unitário compara o arquivo do jogo antes e depois do ciclo ligar e desligar.
- [x] 4.4 Documentar a receita no `docs/ARQUITETURA.md` (tela cheia sempre na nativa, motivo de D5). Verificação: o documento descreve o exemplo 1600x900 com jogo em 1366x768.

## 5. Encerrar a change `toggle-hud-in-game`

- [x] 5.1 Remover `openspec/changes/toggle-hud-in-game/` depois de confirmada a cópia da tarefa 1.2. Verificação: `openspec list --json` não lista mais a change, e a pasta de handoff continua com os artefatos.

## 6. Documentação do recorte

- [x] 6.1 Atualizar `README.md` e `README.en.md` para descrever só o setup e a tela cheia e apontar o HUD para o projeto próprio. Verificação: `Select-String 'F8'` não retorna nada nos dois arquivos, e ambos citam o projeto separado.
- [x] 6.2 Atualizar `docs/ARQUITETURA.md` e `docs/ROADMAP.md` removendo a ponte do HUD e registrando o recorte. Verificação: os documentos não citam `HudToggle` nem `intencao.json` como parte do aplicativo.
- [x] 6.3 Registrar a mudança no `CHANGELOG.md` com a marca de quebra. Verificação: a entrada nova cita a remoção do HUD em jogo e a mudança da tela cheia.

## 7. Verificação final

- [x] 7.1 Publicar o aplicativo e abrir a tela principal. Verificação: captura de tela sem controle de HUD em jogo, com o setup e a tela cheia.
- [x] 7.2 Em máquina de referência (monitor 1600x900, jogo em 1366x768 em janela sem borda), aplicar a tela cheia e abrir o jogo. Verificação: captura do jogo ocupando a tela inteira e o `UserSettings.json` com a resolução nativa.
- [x] 7.3 Validar a change. Verificação: `openspec validate scope-app-to-setup-and-fullscreen` passa.

## Workflow follow-up

- Arquivar esta change depois da revisão.
- Criar o repositório do projeto de HUD a partir de `handoff/hud-toggle/` e então apagar a pasta de handoff daqui.
