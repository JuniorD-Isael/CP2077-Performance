# Spec Delta

## MODIFIED Requirements

### Requirement: As operações ficam acessíveis a partir da interface

O aplicativo SHALL oferecer, de forma explícita, na tela principal, as operações de aplicar e restaurar backup, e SHALL NOT oferecer uma operação de simulação. O acesso às demais operações, incluindo descartar edições do perfil, SHALL ficar atrás da revelação explícita do detalhe técnico.

#### Scenario: Usuário acessa as operações

- **WHEN** o aplicativo está em execução
- **THEN** as operações de aplicar e restaurar backup estão disponíveis na tela principal
- **AND** as operações que alteram o jogo são identificadas como tal

#### Scenario: Operação bloqueada

- **WHEN** uma operação não pode ser executada no estado atual
- **THEN** ela aparece desabilitada
- **AND** o motivo é apresentado ao usuário

#### Scenario: Não há operação de simulação

- **WHEN** o usuário procura uma operação de simulação na interface
- **THEN** nenhuma operação de simulação existe
- **AND** a quantidade de configurações que seriam alteradas é apresentada na tela principal

#### Scenario: Demais operações

- **WHEN** o usuário aciona a revelação do detalhe técnico
- **THEN** as operações que não estão na tela principal, incluindo descartar edições do perfil, ficam disponíveis
- **AND** nenhuma delas aparece na tela principal

## ADDED Requirements

### Requirement: A tela principal oferece escolhas, não tabelas

A tela principal SHALL apresentar as variantes do perfil como uma lista de escolhas, cada uma com rótulo e resumo, e SHALL NOT apresentar na tela principal as tabelas de opções, a coluna de procedência nem a identificação de seção e chave de cvar.

#### Scenario: Usuário abre o aplicativo

- **WHEN** o aplicativo é aberto
- **THEN** a tela principal mostra a lista de variantes, com rótulo e resumo de cada uma
- **AND** nenhuma tabela de opções está presente na tela principal

#### Scenario: Usuário escolhe uma variante

- **WHEN** o usuário marca uma variante na lista
- **THEN** a escolha fica visível e destacada na tela principal
- **AND** o efeito da escolha passa a ser explicado

### Requirement: O efeito da escolha é explicado em linguagem corrente

O aplicativo SHALL explicar, em linguagem corrente, o efeito das variantes selecionadas, usando o resumo e os destaques declarados por elas, e SHALL informar quantas configurações serão alteradas no total.

#### Scenario: Efeito explicado

- **WHEN** há uma ou mais variantes selecionadas
- **THEN** a tela principal mostra o resumo e os destaques de cada variante selecionada
- **AND** informa quantas configurações serão alteradas no total

#### Scenario: Nenhuma variante selecionada

- **WHEN** nenhuma variante está selecionada
- **THEN** a explicação vem do perfil base
- **AND** a contagem de alterações corresponde ao perfil base

### Requirement: A apresentação detalhada fica atrás de uma revelação explícita

As tabelas de opções, a coluna de procedência e a identificação de seção e chave de cvar SHALL ficar atrás de uma ação explícita de revelação, e SHALL permanecer completas quando reveladas.

#### Scenario: Tela principal

- **WHEN** o aplicativo está na tela principal
- **THEN** as tabelas de opções não estão presentes

#### Scenario: Revelação

- **WHEN** o usuário aciona a revelação do detalhe
- **THEN** as tabelas aparecem com todas as colunas
- **AND** a coluna de procedência continua disponível
- **AND** a identificação de seção e chave de cvar continua disponível

### Requirement: O estado da aplicação é apresentado na tela principal

A tela principal SHALL apresentar se o perfil está aplicado e quando foi, SHALL informar quantas opções divergem do valor que o jogo está usando de fato, e SHALL oferecer a reversão a partir do estado apresentado.

#### Scenario: Perfil aplicado

- **WHEN** o perfil está aplicado no jogo
- **THEN** a tela principal informa que está aplicado e quando foi
- **AND** oferece a reversão

#### Scenario: Divergência com o jogo

- **WHEN** o valor em vigor no jogo difere do desejado
- **THEN** a tela principal informa quantas opções divergem

#### Scenario: Perfil nunca aplicado

- **WHEN** o perfil não foi aplicado
- **THEN** a tela principal apresenta o estado vazio
- **AND** não apresenta um estado falso

### Requirement: O resultado de uma operação é apresentado de forma distinguível

O aplicativo SHALL apresentar o resultado de uma operação com um rótulo próprio que identifique sucesso, falha parcial ou erro, e SHALL apresentar a razão da falha quando houver.

#### Scenario: Sucesso

- **WHEN** uma aplicação é concluída com sucesso
- **THEN** o resultado é apresentado com o rótulo de sucesso

#### Scenario: Falha

- **WHEN** uma aplicação falha
- **THEN** o resultado é apresentado com o rótulo de erro
- **AND** a razão da falha é apresentada

### Requirement: A primeira execução orienta para a pasta do jogo

Quando nenhuma pasta de jogo foi informada, o aplicativo SHALL apresentar orientação para localizá-la em cada loja, e SHALL NOT apresentar grades de opções vazias sem explicação.

#### Scenario: Primeira execução

- **WHEN** o aplicativo é aberto e nenhuma pasta foi informada
- **THEN** a orientação para localizar a pasta por loja aparece antes das abas
- **AND** nenhuma grade de opções vazia é apresentada sem explicação

#### Scenario: Pasta já informada

- **WHEN** o aplicativo é aberto e uma pasta válida está guardada
- **THEN** a orientação de primeira execução não aparece

### Requirement: A interface segue uma base Material com paleta Cyberpunk

A interface SHALL seguir uma base Material, com hierarquia por elevação, cantos arredondados, escala tipográfica e camada de estado, e SHALL usar a paleta Cyberpunk — `f7f7ff`, `6564db`, `85ff9e` e `000d25` — como origem de todas as suas cores, em variante escura e em variante clara.

#### Scenario: Tema escuro

- **WHEN** a preferência de aplicativo do sistema é escura
- **THEN** o fundo da interface é `000d25` e o texto é `f7f7ff`
- **AND** a ação principal usa `6564db`
- **AND** o destaque usa `85ff9e`

#### Scenario: Tema claro

- **WHEN** a preferência de aplicativo do sistema é clara
- **THEN** o fundo da interface é `f7f7ff` e o texto é `000d25`
- **AND** a ação principal usa `6564db`
- **AND** o destaque usa um tom derivado de `85ff9e` que permanece legível sobre o fundo claro

#### Scenario: Nenhuma cor fora da paleta

- **WHEN** qualquer elemento da interface é pintado
- **THEN** a cor usada deriva de uma das quatro cores da paleta
- **AND** nenhuma cor arbitrária é introduzida

#### Scenario: Escala de exibição

- **WHEN** o aplicativo roda com escala de exibição diferente de 100%
- **THEN** os cantos, os espaçamentos e os textos escalam junto
