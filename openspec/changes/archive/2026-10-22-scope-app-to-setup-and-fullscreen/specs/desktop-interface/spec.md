# Spec Delta

## Purpose

Define a fronteira do aplicativo: setup para máquinas fracas e tela cheia. Alternar o HUD em jogo não pertence a ele.

## ADDED Requirements

### Requirement: O aplicativo se restringe ao setup e à tela cheia

O aplicativo SHALL oferecer apenas a aplicação do perfil de setup para máquinas fracas e a escolha de tela cheia. O aplicativo SHALL NOT registrar tecla global, SHALL NOT instalar nem atualizar mods do Cyber Engine Tweaks e SHALL NOT apresentar controle ou estado de alternância de HUD em jogo.

#### Scenario: Tela principal sem controle de HUD em jogo

- **WHEN** o usuário abre a tela principal
- **THEN** ela apresenta o setup e a tela cheia
- **AND** não apresenta tecla de alternância, estado de HUD em jogo nem instalação de mod

#### Scenario: Setup sem escolha que altere o HUD

- **WHEN** o usuário abre a tela principal
- **THEN** nenhuma escolha oferecida altera o HUD do jogo

#### Scenario: Nenhuma tecla global registrada

- **WHEN** o aplicativo está em execução, com ou sem o jogo aberto
- **THEN** nenhuma tecla global é registrada pelo aplicativo

#### Scenario: Nenhuma escrita no diretório de mods

- **WHEN** o usuário aplica o perfil ou a tela cheia
- **THEN** o aplicativo não cria nem altera arquivos em `cyber_engine_tweaks\mods`

### Requirement: Um mod de HUD remanescente é reportado

O aplicativo SHALL reportar, sem remover, a presença do mod `CP2077Performance` no diretório de mods do Cyber Engine Tweaks, informando que a alternância de HUD agora pertence a outro projeto.

#### Scenario: Mod antigo encontrado

- **WHEN** o diretório do mod `CP2077Performance` existe na pasta do jogo
- **THEN** o aplicativo informa que o mod é de uma versão anterior e que o aplicativo não o gerencia mais
- **AND** não apaga nem altera o diretório
