# Spec Delta

## Purpose

Define como o aplicativo se apresenta ao usuário: idioma e tema derivados do sistema operacional, exposição das cvars do motor com a documentação do próprio jogo, e exibição do estado que o jogo está de fato usando.

## ADDED Requirements

### Requirement: O idioma segue o sistema operacional

O aplicativo SHALL usar português quando o idioma do sistema for português do Brasil, e SHALL usar inglês em qualquer outro idioma.

#### Scenario: Sistema em português do Brasil

- **WHEN** o idioma do sistema operacional é português do Brasil
- **THEN** toda a interface do aplicativo é apresentada em português

#### Scenario: Sistema em outro idioma

- **WHEN** o idioma do sistema operacional não é português do Brasil
- **THEN** toda a interface do aplicativo é apresentada em inglês

#### Scenario: Idioma desconhecido ou não definido

- **WHEN** o idioma do sistema operacional não está definido ou é um idioma sem tradução
- **THEN** a interface é apresentada em inglês
- **AND** nenhuma string fica sem tradução ou sem traduzir

### Requirement: O tema segue o sistema operacional

O aplicativo SHALL adotar o tema claro ou escuro conforme a preferência de aplicativo do sistema operacional, e SHALL se ajustar a diferentes escalas de exibição do monitor.

#### Scenario: Sistema em tema escuro

- **WHEN** a preferência de aplicativo do sistema é escura
- **THEN** o aplicativo é apresentado com cores escuras

#### Scenario: Sistema em tema claro

- **WHEN** a preferência de aplicativo do sistema é clara
- **THEN** o aplicativo é apresentado com cores claras

#### Scenario: Monitor com escala de exibição elevada

- **WHEN** o aplicativo é executado em um monitor com escala de exibição diferente de 100%
- **THEN** os textos e controles permanecem legíveis e proporcionais

### Requirement: As cvars do motor são apresentadas com a documentação do jogo

O aplicativo SHALL apresentar as opções de cvar do motor com a descrição que o próprio jogo acompanha, quando existir, e SHALL indicar a seção a que pertencem.

#### Scenario: Cvar com comentário no jogo

- **WHEN** o arquivo de cvars do motor contém um comentário para uma opção
- **THEN** o aplicativo apresenta esse comentário como descrição da opção
- **AND** apresenta a seção da opção

#### Scenario: Cvar sem comentário no jogo

- **WHEN** não há comentário do jogo para uma opção de cvar
- **THEN** o aplicativo apresenta a opção sem descrição, em vez de inventar uma

#### Scenario: Cvar não presente no arquivo do jogo

- **WHEN** uma opção de cvar do perfil não está presente no arquivo de cvars do motor
- **THEN** o aplicativo a apresenta mesmo assim, marcada como a ser acrescentada

### Requirement: O estado efetivo do jogo é apresentado

O aplicativo SHALL apresentar o valor que o jogo está de fato usando, e não o valor desejado pelo perfil, sempre que houver divergência.

#### Scenario: Valor em vigor difere do desejado

- **WHEN** o valor em vigor no arquivo de opções do jogo difere do valor desejado pelo perfil
- **THEN** o aplicativo apresenta os dois valores lado a lado
- **AND** indica que o jogo está usando o valor em vigor

#### Scenario: Valores coincidem

- **WHEN** o valor em vigor coincide com o valor desejado pelo perfil
- **THEN** o aplicativo apresenta o valor sem divergência

#### Scenario: Camada ausente

- **WHEN** uma camada não está presente na instalação
- **THEN** o aplicativo informa que a camada está indisponível
- **AND** não apresenta valores fictícios para ela

### Requirement: As operações ficam acessíveis a partir da interface

O aplicativo SHALL oferecer, de forma explícita, as operações de aplicar, simular, restaurar backup e descartar edições do perfil.

#### Scenario: Usuário acessa as operações

- **WHEN** o aplicativo está em execução
- **THEN** as operações de aplicar, simular, restaurar backup e descartar edições estão disponíveis
- **AND** as operações que alteram o jogo são identificadas como tal

#### Scenario: Operação bloqueada

- **WHEN** uma operação não pode ser executada no estado atual
- **THEN** ela aparece desabilitada
- **AND** o motivo é apresentado ao usuário
