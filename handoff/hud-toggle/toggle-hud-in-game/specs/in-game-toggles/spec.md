# Spec Delta

## Purpose

Define como o HUD do jogo é alternado durante a sessão: a tecla global que
inverte o estado, o arquivo de intenção que atravessa a fronteira entre o
aplicativo e o jogo, e o mod que aplica a mudança no motor em memória.

## ADDED Requirements

### Requirement: O estado do HUD é alternado por uma tecla global

O aplicativo SHALL capturar uma tecla global enquanto o jogo está em execução e, ao acioná-la, SHALL inverter o estado do HUD entre mínimo e completo. A tecla SHALL ser configurável e o aplicativo SHALL informar quando ela não puder ser registrada.

#### Scenario: Tecla pressionada com o jogo em execução

- **WHEN** o jogo está em execução e o usuário pressiona a tecla de alternância
- **THEN** o estado do HUD é invertido
- **AND** a intenção é registrada para o jogo

#### Scenario: Tecla pressionada com o jogo fechado

- **WHEN** o jogo não está em execução e o usuário pressiona a tecla de alternância
- **THEN** o estado do HUD é invertido
- **AND** as opções do HUD são aplicadas ao arquivo do jogo

#### Scenario: Tecla ocupada por outro programa

- **WHEN** a tecla de alternância já está registrada por outro programa
- **THEN** o aplicativo informa que a tecla não pôde ser reservada
- **AND** o estado do HUD continua alternável por outro meio

### Requirement: A intenção atravessa a fronteira entre o aplicativo e o jogo

O aplicativo SHALL registrar a intenção de HUD num arquivo no diretório do mod, derivado da pasta do jogo, e o mod SHALL ler esse arquivo. O arquivo SHALL registrar o estado do HUD e as opções que o definem, de modo que o mod não precise conhecer o perfil.

#### Scenario: Intenção gravada

- **WHEN** o estado do HUD é invertido
- **THEN** o aplicativo grava no arquivo de intenção o estado novo e as opções de menu que definem o HUD
- **AND** o mod é notificado da mudança

#### Scenario: Mod ausente

- **WHEN** o arquivo de intenção é gravado e o mod não está instalado
- **THEN** o aplicativo informa que o mod está ausente
- **AND** o estado do HUD continua alternável no aplicativo

#### Scenario: Jogo fechado

- **WHEN** o estado do HUD é invertido e o jogo não está em execução
- **THEN** o aplicativo escreve as opções do HUD no arquivo de opções do jogo
- **AND** o jogo assume o novo estado no próximo início

### Requirement: O mod aplica o HUD no motor em memória

O mod SHALL ler a intenção e SHALL aplicar as opções do HUD no motor durante a sessão, sem exigir o encerramento do jogo. O mod SHALL ler apenas do próprio diretório e SHALL escrever apenas no próprio diretório.

#### Scenario: Mudança aplicada em gameplay

- **WHEN** o mod lê uma intenção nova e o jogo está em gameplay
- **THEN** os elementos do HUD ligados e desligados mudam sem o jogo ser reiniciado

#### Scenario: Estado já aplicado

- **WHEN** o mod lê uma intenção que corresponde ao estado que já está em vigor
- **THEN** nada é alterado

#### Scenario: O mod não escreve fora do diretório

- **WHEN** o mod é executado
- **THEN** ele não abre nenhum arquivo fora do próprio diretório
- **AND** ele não depende de variável de ambiente

### Requirement: O estado do HUD é apresentado durante a sessão

O aplicativo SHALL apresentar o estado atual do HUD e a tecla que o alterna, inclusive enquanto o jogo está em execução e o aplicativo está na bandeja.

#### Scenario: Jogo em execução

- **WHEN** o jogo está em execução
- **THEN** o aplicativo indica o estado atual do HUD e a tecla que o alterna

#### Scenario: Estado invertido

- **WHEN** o estado do HUD é invertido
- **THEN** o aplicativo reflete o novo estado
