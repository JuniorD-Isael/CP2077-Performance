# Spec Delta

## Purpose

Define como o aplicativo deixa o jogo em tela cheia na resolução nativa do monitor, sem depender da resolução que o jogo estava usando.

## ADDED Requirements

### Requirement: A tela cheia independe da resolução configurada no jogo

Quando o usuário escolhe tela cheia, o aplicativo SHALL escrever o modo de janela em tela cheia e a resolução nativa do monitor do jogo, qualquer que seja a resolução ou o modo de janela que o jogo tinha antes. O aplicativo SHALL NOT condicionar a escrita a essa resolução ser menor, igual ou maior que a nativa, nem a modo anterior ser janela sem borda. A resolução interna SHALL permanecer sob a escala dinâmica de resolução, sem mudança.

#### Scenario: Jogo em resolução menor que a nativa

- **WHEN** o monitor é 1600x900, o jogo está em 1366x768 e em janela sem borda, e o usuário escolhe tela cheia
- **THEN** o aplicativo escreve o modo de tela cheia e a resolução 1600x900
- **AND** guarda as opções de exibição anteriores para poder devolvê-las

#### Scenario: Jogo já na resolução nativa em janela sem borda

- **WHEN** o jogo está na resolução nativa em janela sem borda e o usuário escolhe tela cheia
- **THEN** o aplicativo escreve o modo de tela cheia e mantém a resolução nativa

#### Scenario: Jogo em resolução maior que a nativa

- **WHEN** o arquivo do jogo traz uma resolução maior que a nativa do monitor
- **THEN** o aplicativo escreve a resolução nativa, e não a configurada

### Requirement: O monitor do jogo determina a resolução nativa

O aplicativo SHALL determinar o monitor em que o jogo roda e a resolução nativa dele, e SHALL usar esse valor na escrita de tela cheia. Quando a resolução nativa não puder ser determinada, o aplicativo SHALL NOT escrever as opções de exibição e SHALL informar o motivo.

#### Scenario: Resolução nativa indeterminada

- **WHEN** o aplicativo não consegue obter a resolução nativa do monitor
- **THEN** nenhuma opção de exibição é escrita
- **AND** o usuário é informado de que a tela cheia não foi aplicada e por quê
