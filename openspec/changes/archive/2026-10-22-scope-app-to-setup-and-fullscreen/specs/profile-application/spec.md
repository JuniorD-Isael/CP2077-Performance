# Spec Delta

## Purpose

Define que o perfil curado do aplicativo trata só de performance e não altera o HUD do jogo.

## ADDED Requirements

### Requirement: O perfil curado não altera o HUD

O perfil curado SHALL NOT declarar opções de menu, cvars ou variantes cujo efeito seja alterar elementos do HUD do jogo. O mecanismo de variantes SHALL permanecer disponível, e um id de variante persistido que o perfil não declara mais SHALL ser ignorado sem erro.

#### Scenario: Perfil sem opções de HUD

- **WHEN** o perfil curado é carregado
- **THEN** ele não declara a variante `hudMinimal` nem opções como `minimap`, `quest_tracker` ou `npc_nameplates`

#### Scenario: Variante salva que o perfil não declara mais

- **WHEN** o estado salvo referencia o id `hudMinimal` e o perfil carregado não o declara
- **THEN** o aplicativo ignora esse id sem erro
- **AND** não escreve nem remove opções do jogo por causa dele
