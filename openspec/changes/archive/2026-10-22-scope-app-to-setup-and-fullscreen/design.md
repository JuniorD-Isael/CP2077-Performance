# Design

## Context

O aplicativo (`app/`) tem três camadas: `Core` (perfil, escrita, backup, instalação, exibição, `GameIntegration`), `Ui` (WinForms) e `Tests`. O trabalho de HUD ficou como alterações ainda não commitadas:

- `Core/GameIntegration/HudToggle.cs`: o arquivo de intenção e a instalação do mod.
- `Ui/GlobalHotkey.cs`: `RegisterHotKey`.
- `Tests/HudToggleTests.cs` e a entrada em `Tests/Program.cs`.
- Trechos em `Ui/AppState.cs` e `Ui/MainForm.cs`.
- `mod/CP2077Performance/init.lua`, o mod do CET.
- A change `toggle-hud-in-game` (0/24), que planeja bandeja, F8 e um caminho de queda.

A tela cheia vem de `Core/Display/*`, da change `fullscreen-at-native-resolution` (18/18, não arquivada). Hoje ela oferece "manter como está" ou "tela cheia na resolução do monitor".

## Goals / Non-Goals

**Goals**
- O aplicativo contém só setup e tela cheia.
- A tela cheia é aplicada na resolução nativa, sem depender da resolução anterior do jogo.
- O trabalho de HUD é preservado e entregue como ponto de partida do projeto novo.

**Non-Goals**
- Criar o repositório do projeto de HUD ou escrever o mod novo.
- Desinstalar o mod de máquinas onde ele já foi instalado.
- Mudar o formato do perfil, o backup ou a escala dinâmica de resolução.

## Decisions

**D1. Remover, não ocultar.** O código de HUD é apagado do aplicativo, não escondido por flag. Uma flag manteria `GlobalHotkey` e `HudToggle` no build e a dependência do CET. O histórico de `git` basta para recuperar.

**D2. Handoff por pasta, não por repositório.** Os artefatos da `toggle-hud-in-game`, o `HudToggle.cs`, o `GlobalHotkey.cs`, o `init.lua` e os trechos relevantes de `FINDINGS.md` (§13.1, §14.4) são copiados para `handoff/hud-toggle/` e commitados na mesma change. O usuário leva essa pasta para o repositório novo. Alternativa descartada: `git filter-repo` agora, porque o código de HUD ainda não foi commitado e o histórico não o contém.

**D3. A change `toggle-hud-in-game` é removida do repositório sem arquivar.** Arquivar a marcaria como feita e sincronizaria specs `in-game-toggles` para `openspec/specs/`, o que contradiz o recorte. Os arquivos vão para o handoff (D2) antes da remoção.

**D4. A tela cheia é um comportamento do aplicativo, não do perfil.** A resolução nativa depende da máquina, então continua fora de `profile/*.json`. Quando a escolha está ligada, a escrita usa sempre a resolução nativa do monitor (`Display/DisplayModes.cs`, `DisplayMonitor.cs`), ignorando a resolução e o modo de janela que o jogo tinha. A escolha "manter como está" continua devolvendo as opções anteriores (`DisplayOptions.cs`).

**D5. Tela cheia sempre na nativa, nunca em resolução menor.** Tela cheia em resolução menor que a nativa pode trocar o modo de vídeo do monitor. A resolução interna menor continua vindo da escala dinâmica, que não muda.

**D6. O mod antigo é reportado, não removido.** `LeftoverPanelDetector` já detecta o painel in-game remanescente. Ele passa a reconhecer também o diretório `cyber_engine_tweaks\mods\CP2077Performance` e a informar, sem apagar. O motivo é que o diretório pode conter dados do usuário e o novo projeto vai querer o mesmo nome.

**D7. `hudMinimal` sai; o mecanismo de variantes fica.** O app foca em performance e tela cheia, e nada que altere o HUD pertence a ele, nem como opção aplicada no setup. O bloco `variants.hudMinimal` é removido de `profile/lowspec-30fps.json` e copiado para o handoff (D2). `VariantResolver`, `userIni` e o desligar permanecem, prontos e sem variante declarada. Alternativa descartada: remover também o mecanismo, que é uma refatoração maior, sem pedido explícito, e que `profile-application` e `desktop-interface` ainda especificam.

## Risks / Trade-offs

- **Ordem com `fullscreen-at-native-resolution`.** Esta change adiciona requisitos a `display-mode`, que ainda não está em `openspec/specs/`. Mitigação: arquivar a outra antes de arquivar esta, como primeiro passo das tarefas.
- **Perda do trabalho de HUD.** Mitigada pelo handoff (D2) feito antes de qualquer remoção.
- **Quem instalou o mod mantém um mod sem dono.** O aviso do D6 cobre; ele continua funcionando, pois o mod só lê `intencao.json` e ninguém mais o escreve.
- **Quem já aplicou `hudMinimal` mantém as opções de HUD no `UserSettings.json`.** O aplicativo passa a ignorar, sem erro, um id de variante salvo que o perfil não declara mais. Quem quiser reverter usa o backup ou o próprio jogo.
- **Pressuposto não confirmado:** a tela cheia independente da resolução quer dizer sempre a nativa do monitor, com a escolha de desligar mantida. Se a intenção era remover o desligar, a tarefa 4.3 muda.

## Open Questions

- O novo projeto de HUD manterá o nome `CP2077Performance` para o mod? O D6 assume que o nome pode ser reutilizado.
