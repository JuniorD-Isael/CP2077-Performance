# Spec Delta

## MODIFIED Requirements

### Requirement: A origem da instalação é apresentada ao usuário

O aplicativo SHALL apresentar a origem de cada alvo derivado, para que o usuário possa conferir de onde cada arquivo vem, e SHALL apresentar o diretório do mod e o arquivo de intenção junto com os demais alvos.

#### Scenario: Usuário confere a origem dos alvos

- **WHEN** o aplicativo apresenta as camadas disponíveis
- **THEN** mostra o caminho completo de cada arquivo alvo
- **AND** indica a pasta do jogo como origem das camadas de motor e de patches
- **AND** indica o diretório de dados local do sistema como origem da camada de menu

#### Scenario: Diretório do mod

- **WHEN** o aplicativo apresenta o diretório do mod e o arquivo de intenção
- **THEN** indica a pasta do jogo como origem dos dois
- **AND** informa a finalidade de cada um
