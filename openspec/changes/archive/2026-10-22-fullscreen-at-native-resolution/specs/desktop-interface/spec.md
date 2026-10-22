# Spec Delta

## ADDED Requirements

### Requirement: A tela principal oferece o modo de exibição

A tela principal SHALL apresentar uma escolha de modo de exibição, com o monitor de destino e a resolução de saída que serão usados, e SHALL informar que a resolução interna do jogo permanece a mesma.

#### Scenario: O usuário escolhe tela cheia

- **WHEN** o usuário escolhe a tela cheia na resolução do monitor
- **THEN** a tela principal passa a mostrar o monitor escolhido e a resolução de saída
- **AND** informa que a resolução interna do jogo permanece a mesma

#### Scenario: Monitores disponíveis

- **WHEN** o sistema tem mais de um monitor
- **THEN** a escolha permite escolher entre eles, com a resolução nativa de cada um

#### Scenario: Manter como está

- **WHEN** o usuário mantém o modo de exibição como está
- **THEN** a escolha mostra a resolução que o jogo já usa
- **AND** nenhuma opção de exibição é alterada
