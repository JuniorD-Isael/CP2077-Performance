# display-mode Specification Delta

## MODIFIED Requirements

### Requirement: As opções de exibição são escritas com o valor e o índice do motor

O aplicativo SHALL escrever a opção de modo de janela em tela cheia (`Fullscreen`) com o campo de valor e com o campo de índice coerentes com a lista que o motor usa (`2`), e SHALL NOT alterar a opção de resolução (`Resolution`) do arquivo do jogo.

#### Scenario: Índice conhecido

- **WHEN** o aplicativo aplica o perfil e o arquivo de opções do jogo contém a opção de modo de janela
- **THEN** escreve o valor `Fullscreen` e o índice `2` no modo de janela
- **AND** mantém inalterados o valor e o índice da opção `Resolution`

#### Scenario: Índice desconhecido

- **WHEN** a opção de resolução do jogo usa um valor ou índice qualquer da máquina do usuário
- **THEN** o aplicativo não altera nem reescreve a opção `Resolution`
- **AND** escreve apenas o modo de janela `Fullscreen` com o índice `2` do motor

#### Scenario: Opção ausente no arquivo do jogo

- **WHEN** o arquivo de opções do jogo não contém a opção de modo de janela
- **THEN** o aplicativo não a acrescenta
- **AND** a aplicação prossegue com as demais camadas

### Requirement: A tela cheia independe da resolução configurada no jogo

Ao aplicar o perfil, o aplicativo SHALL escrever sempre o modo de janela em tela cheia (`Fullscreen`), sem pedir confirmação ou escolha de monitor ao usuário e sem sobrescrever a resolução configurada no jogo. A resolução configurada pelo usuário — seja menor, igual ou maior que a do monitor — e a escala dinâmica de resolução SHALL permanecer intactas.

#### Scenario: Jogo em resolução menor que a nativa

- **WHEN** o monitor é 1600x900, o jogo está em 1366x768 e em janela sem borda, e o usuário aplica o perfil
- **THEN** o aplicativo escreve o modo de tela cheia (`Fullscreen`, índice `2`)
- **AND** preserva a resolução `1366x768` sem alterá-la

#### Scenario: Jogo já na resolução nativa em janela sem borda

- **WHEN** o jogo está na resolução nativa em janela sem borda e o usuário aplica o perfil
- **THEN** o aplicativo escreve o modo de tela cheia (`Fullscreen`, índice `2`) e mantém a resolução configurada

#### Scenario: Jogo em resolução maior que a nativa

- **WHEN** o arquivo do jogo traz uma resolução maior que a nativa do monitor e o usuário aplica o perfil
- **THEN** o aplicativo escreve o modo de tela cheia (`Fullscreen`, índice `2`) e preserva a resolução configurada no jogo sem forçar outra

#### Scenario: Jogo em execução

- **WHEN** o jogo está em execução
- **THEN** o aplicativo recusa a aplicação, como nas demais camadas

## REMOVED Requirements

### Requirement: A resolução de saída é a resolução nativa de um monitor

**Reason**: Fixar ou sobrescrever a resolução do jogo com base nos monitores enumerados limita ou extrapola setups de outros usuários (como quem reduz a resolução intencionalmente para ganhar desempenho ou usa monitores de tamanhos variados).
**Migration**: O aplicativo passa a escrever apenas o modo de janela em tela cheia (`Fullscreen`) e preserva intacta a resolução já configurada no jogo.

### Requirement: O modo de exibição é uma escolha explícita

**Reason**: A aplicação de tela cheia passa a fazer parte integral da aplicação do perfil, sem caixa de seleção na interface; a reversão é feita pela restauração de backup já disponível na tela principal.
**Migration**: Acionar **Aplicar perfil** aplica sempre o modo de janela em tela cheia junto com o perfil, e **Restaurar último backup** devolve o arquivo anterior.

### Requirement: O monitor do jogo determina a resolução nativa

**Reason**: O aplicativo não força mais a resolução nativa do monitor sobre a opção `Resolution` do jogo, eliminando a dependência de enumerar monitores do sistema.
**Migration**: Nenhuma resolução é sobrescrita; apenas `WindowMode` é ajustado para `Fullscreen`.
