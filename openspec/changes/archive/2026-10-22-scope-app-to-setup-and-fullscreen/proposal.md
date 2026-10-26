# Proposal

## Why

O aplicativo acumulou três responsabilidades que não pertencem juntas: aplicar o setup para máquinas fracas, forçar tela cheia e alternar o HUD durante a sessão. A terceira é a que mais pesa. Ela trouxe para o aplicativo uma tecla global (`GlobalHotkey`), um arquivo de intenção, a instalação de um mod do CET (`mod/CP2077Performance/init.lua`) e a dependência do CET. Nada disso é setup. Hoje a change `toggle-hud-in-game` (0/24 tarefas) ainda planeja aprofundar esse acoplamento, com uma tecla F8, presença na bandeja e um caminho de queda que escreve opções de HUD.

O usuário decidiu o recorte: o aplicativo fica com **o setup para PC/notebook fraco** e **a tela cheia independente da resolução**. Tudo o que mexe no HUD vira **um projeto separado**.

## What Changes

- **BREAKING** O aplicativo deixa de oferecer alternância de HUD em jogo: sai a tecla global, o `HudToggle` (arquivo de intenção e instalação do mod), a presença do estado do HUD na tela principal e os testes correspondentes.
- O mod Lua (`mod/CP2077Performance/`) sai deste repositório e passa a ser a base do novo projeto de HUD. O aplicativo para de instalá-lo e de escrever no diretório de mods do CET.
- A change `toggle-hud-in-game` é **encerrada neste repositório**: seus artefatos (proposta, design, specs, tarefas) são preservados como material de partida do novo projeto, e a change é removida daqui sem ser aplicada nem arquivada.
- A tela cheia passa a valer **independente da resolução configurada no jogo**: o aplicativo escreve o modo de janela em tela cheia e a resolução nativa do monitor, e não depende de o jogo estar em uma resolução menor ou igual à nativa. (Ex.: monitor 1600x900 e jogo em 1366x768 abre em tela cheia 1600x900.)
- **BREAKING** A variante `hudMinimal` é removida do perfil: `variants.hudMinimal` sai de `profile/lowspec-30fps.json` e o perfil curado deixa de declarar qualquer opção que altere o HUD. O mecanismo de variantes permanece, sem variante declarada. O bloco removido é preservado no handoff do projeto de HUD.
- Documentação (`README.md`, `README.en.md`, `docs/ARQUITETURA.md`, `docs/ROADMAP.md`, `CHANGELOG.md`) passa a descrever o recorte e aponta o HUD para o projeto próprio.

**O que não muda:** o formato de `profile/*.json` (só o conteúdo perde a variante), o backup, a localização do jogo e a escala dinâmica de resolução.

## Capabilities

### New Capabilities
- `display-mode`: tela cheia na resolução nativa do monitor, independente da resolução que o jogo tinha. A spec nasce da change `fullscreen-at-native-resolution` (concluída, ainda não arquivada); esta change acrescenta o requisito de independência.

### Modified Capabilities
- `desktop-interface`: o aplicativo declara a fronteira do seu escopo e deixa de apresentar qualquer controle de HUD em jogo ou escolha que altere o HUD.
- `profile-application`: o perfil curado não declara opções que alterem o HUD, e um id de variante salvo que o perfil não declara mais é ignorado sem erro.

## Impact

**Aplicativo** (`app/CP2077Performance.Core`, `app/CP2077Performance.Ui`, `app/CP2077Performance.Tests`): remoção de `GameIntegration/HudToggle.cs`, `Ui/GlobalHotkey.cs`, `Tests/HudToggleTests.cs` e dos trechos de `AppState.cs`, `MainForm.cs`, `Strings*.resx` e `Program.cs` de testes que os usam; ajuste da escrita de exibição para tela cheia independente da resolução.

**Perfil e testes:** `profile/lowspec-30fps.json` perde `variants.hudMinimal`; `app/CP2077Performance.Tests/ProfileTests.cs` deixa de depender dela (linhas 33, 80, 130-131, 150, 238, 258, 373 e 415); `docs/ROADMAP.md` §2.0 é reescrito.

**Repositório:** `mod/` sai; `openspec/changes/toggle-hud-in-game/` sai (preservado em handoff).

**Novo projeto (fora deste repositório):** recebe o mod, a captura de tecla e o design de HUD. Criar o repositório é fora de escopo aqui; esta change só entrega o material.

**Dependência de ordem:** `fullscreen-at-native-resolution` deve ser arquivada antes desta, para que a spec `display-mode` exista em `openspec/specs/`.

**Riscos:** quem já instalou o mod via aplicativo fica com `CP2077Performance` em `cyber_engine_tweaks/mods`. O aplicativo já detecta painel remanescente (spec `game-installation`); a change registra se ele deve também sinalizar o mod antigo. A tela cheia em resolução menor que a nativa pode mudar o modo de vídeo do monitor, por isso o aplicativo sempre usa a nativa.
