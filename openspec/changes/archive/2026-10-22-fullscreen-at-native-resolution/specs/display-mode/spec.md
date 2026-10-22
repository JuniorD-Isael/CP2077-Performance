# Spec Delta

## Purpose

Define como o aplicativo determina o monitor em que o jogo roda, a resolução de
saída que ele deve usar, e como escreve as opções de exibição de forma
consistente com o que o motor espera.

## ADDED Requirements

### Requirement: A resolução de saída é a resolução nativa de um monitor

O aplicativo SHALL enumerar os monitores do sistema, SHALL determinar a resolução nativa de cada um, e SHALL oferecer a escolha do monitor em que o jogo roda. O monitor principal do sistema SHALL ser o padrão da escolha.

#### Scenario: Monitor único

- **WHEN** o sistema tem um monitor
- **THEN** o aplicativo usa a resolução nativa desse monitor como resolução de saída

#### Scenario: Vários monitores

- **WHEN** o sistema tem mais de um monitor
- **THEN** o aplicativo lista os monitores com a resolução nativa de cada um
- **AND** o principal do sistema vem primeiro e vem marcado como o padrão

#### Scenario: Monitor fora do alcance

- **WHEN** o monitor em que o jogo roda não corresponde a nenhum monitor que o aplicativo conhece
- **THEN** o aplicativo mantém a resolução que o jogo já usa
- **AND** não escreve a opção de resolução

### Requirement: As opções de exibição são escritas com o valor e o índice do motor

O aplicativo SHALL escrever a opção de modo de janela e a de resolução com o campo de valor e com o campo de índice coerentes com a lista que o motor usa. Quando o índice não puder ser determinado, o aplicativo SHALL escrever apenas o valor e SHALL informar que a configuração entra em vigor no próximo início do jogo.

#### Scenario: Índice conhecido

- **WHEN** o aplicativo conhece o índice da resolução no monitor escolhido
- **THEN** escreve o valor e o índice da resolução, e o valor e o índice do modo de janela

#### Scenario: Índice desconhecido

- **WHEN** o aplicativo não consegue determinar o índice da resolução
- **THEN** escreve apenas o valor da resolução
- **AND** informa que a resolução entra em vigor no próximo início do jogo

#### Scenario: Opção ausente no arquivo do jogo

- **WHEN** o arquivo de opções do jogo não contém a opção de resolução
- **THEN** o aplicativo não a acrescenta
- **AND** a aplicação prossegue com as demais camadas

### Requirement: O modo de exibição é uma escolha explícita

O aplicativo SHALL oferecer ao usuário a escolha de deixar o modo de exibição do jogo como ele estava, ou de abrir o jogo em tela cheia na resolução do monitor escolhido. Ao deixar como estava, o aplicativo SHALL devolver as opções de exibição que o jogo tinha antes de o aplicativo mexer nelas, e SHALL não escrever nada se nunca mexeu nelas. A escolha SHALL preservar a resolução interna do jogo: a escala dinâmica de resolução SHALL continuar variando como antes.

#### Scenario: Deixar como estava

- **WHEN** o usuário desliga a escolha de tela cheia
- **THEN** o aplicativo devolve ao jogo as opções de exibição que ele tinha antes de o aplicativo mexer nelas
- **AND** não escreve nada quando o aplicativo nunca mexeu nelas

#### Scenario: Tela cheia na resolução do monitor

- **WHEN** o usuário escolhe tela cheia na resolução do monitor
- **THEN** o aplicativo escreve o modo de janela em tela cheia e a resolução nativa do monitor
- **AND** guarda as opções de exibição anteriores, para poder devolvê-las
- **AND** não altera a escala dinâmica de resolução, nem seu piso, nem seu teto

#### Scenario: Jogo em execução

- **WHEN** o jogo está em execução
- **THEN** o aplicativo recusa a aplicação, como nas demais camadas
