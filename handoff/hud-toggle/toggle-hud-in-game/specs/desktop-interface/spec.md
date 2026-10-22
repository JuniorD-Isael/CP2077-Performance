# Spec Delta

## ADDED Requirements

### Requirement: O estado do HUD é apresentado durante a sessão

O aplicativo SHALL apresentar o estado atual do HUD e a tecla que o alterna, inclusive enquanto o jogo está em execução e o aplicativo está na bandeja.

#### Scenario: Jogo em execução

- **WHEN** o jogo está em execução
- **THEN** o aplicativo indica o estado atual do HUD e a tecla que o alterna

#### Scenario: Estado invertido

- **WHEN** o estado do HUD é invertido
- **THEN** o aplicativo reflete o novo estado

#### Scenario: Tecla ocupada

- **WHEN** a tecla de alternância não pôde ser reservada
- **THEN** a tela principal informa isso no lugar do atalho
- **AND** o estado do HUD continua sendo alternável por outro meio
