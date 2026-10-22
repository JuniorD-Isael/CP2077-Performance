# Handoff: projeto de HUD

Material para iniciar o projeto separado que alterna o HUD do Cyber Punk 2077. Saiu do aplicativo pela change `scope-app-to-setup-and-fullscreen`.

## O que foi movido

| Arquivo | O que e |
|---|---|
| `toggle-hud-in-game/` | A change OpenSpec original (proposta, design, specs, tarefas), 0/24 tarefas, nunca aplicada nem arquivada |
| `HudToggle.cs` | Ponte aplicativo-mod: arquivo de intencao e instalacao do mod |
| `GlobalHotkey.cs` | Tecla global com `RegisterHotKey` |
| `HudToggleTests.cs` | Testes da ponte |
| `init.lua` | Mod do CET que le a intencao e aplica as opcoes do grupo `/interface/hud` na memoria do motor |
| `FINDINGS-HUD.md` | Copia das secoes 13.1 e 14.4 de `docs/FINDINGS.md`, mais os dois fatos que restringem o desenho |
| `hudMinimal.variant.json` | O bloco `variants.hudMinimal` removido do perfil, com as opcoes de HUD (`minimap`, `quest_tracker`, ...) |

## O que foi decidido

- A tecla padrao e F8.
- A fronteira e um arquivo de intencao (`intencao.json`) no diretorio do mod; o estado fica em `estado.json`.
- O mod so le e escreve no proprio diretorio.
- A tecla e capturada por um processo externo, porque o CET v1.37 nao expoe registro de tecla.

## O que ficou sem prova

As tarefas 1.1 a 1.5 da `toggle-hud-in-game`:

- O HUD le a opcao de novo durante a sessao?
- O sandbox expoe algum hook de entrada, e a tecla chega com o foco no jogo?
- Forma final: aplicacao ao vivo ou caminho de queda gravando no `UserSettings.json`.

## Observacao

O aplicativo agora reporta, sem apagar, o mod `CP2077Performance` se ele existir em `cyber_engine_tweaks\mods`. O novo projeto pode reutilizar o nome.
