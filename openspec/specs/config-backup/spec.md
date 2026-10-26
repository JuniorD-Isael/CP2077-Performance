# config-backup Specification

## Purpose

Define o ciclo de vida do backup automático das configurações do jogo: quando um backup é criado, onde fica, quantas versões são mantidas, e como uma versão anterior é restaurada.

## Requirements

### Requirement: O backup é automático e precede toda alteração

O aplicativo SHALL criar um backup automático dos arquivos que vai alterar, antes de escrever qualquer um deles, e SHALL criar o backup apenas quando a aplicação alterar ao menos um arquivo.

#### Scenario: Aplicação que altera arquivos

- **WHEN** o usuário aplica um perfil que altera ao menos um arquivo
- **THEN** o aplicativo cria um backup dos arquivos que serão alterados antes de escrever
- **AND** o backup contém o estado anterior de cada um desses arquivos

#### Scenario: Aplicação que não altera nada

- **WHEN** o usuário aplica um perfil cujos valores já estão em vigor
- **THEN** o aplicativo não cria backup
- **AND** informa que não houve alteração

#### Scenario: Falha durante a aplicação

- **WHEN** a escrita de um dos arquivos falha no meio da operação
- **THEN** o backup criado antes da operação permanece disponível para restauração

### Requirement: Cada backup é um instantâneo do conjunto de arquivos da operação

O aplicativo SHALL registrar, em cada backup, o conjunto completo de arquivos que aquela operação tocou, de modo que a restauração devolva o estado anterior do conjunto.

#### Scenario: Backup registra o conjunto de arquivos

- **WHEN** uma operação altera o arquivo de cvars e o arquivo de opções de menu
- **THEN** o backup contém o estado anterior de ambos
- **AND** registra quais arquivos foram alterados nessa operação

#### Scenario: Backup registra o que mudou

- **WHEN** o usuário consulta um backup
- **THEN** o aplicativo informa os arquivos contidos nele e as alterações que eles tinham sofrido

### Requirement: São mantidas as três versões mais recentes

O aplicativo SHALL manter as três versões de backup mais recentes e SHALL apagar a mais antiga sempre que uma nova versão for criada, de modo que a quantidade de versões guardadas nunca exceda três.

#### Scenario: Criação da segunda versão

- **WHEN** existe um backup e uma nova aplicação altera arquivos
- **THEN** o novo backup é criado
- **AND** o backup anterior é mantido

#### Scenario: Criação da terceira versão

- **WHEN** existem dois backups e uma nova aplicação altera arquivos
- **THEN** o novo backup é criado
- **AND** os dois anteriores são mantidos

#### Scenario: Criação da quarta versão

- **WHEN** existem três backups e uma nova aplicação altera arquivos
- **THEN** o novo backup é criado
- **AND** o backup mais antigo é apagado
- **AND** restam três backups

#### Scenario: A rotação continua

- **WHEN** uma quinta aplicação altera arquivos
- **THEN** o backup mais antigo entre os três existentes é apagado
- **AND** restam três backups, sendo o mais recente o último criado

### Requirement: Qualquer versão mantida pode ser restaurada

O aplicativo SHALL permitir restaurar uma das versões de backup mantidas, devolvendo os arquivos ao estado que tinham naquele momento.

#### Scenario: Restauração de uma versão existente

- **WHEN** o usuário escolhe um backup e solicita a restauração
- **THEN** os arquivos contidos nele voltam ao estado guardado
- **AND** o aplicativo informa quais arquivos foram restaurados

#### Scenario: Arquivo de backup ausente na restauração

- **WHEN** um arquivo registrado no backup não existe mais no disco
- **THEN** o aplicativo informa o arquivo ausente
- **AND** não prossegue com a restauração daquele backup

### Requirement: Os backups vivem fora do repositório e do jogo

O aplicativo SHALL guardar os backups no diretório de dados local do usuário, em uma pasta dedicada à ferramenta, e SHALL NOT gravá-los dentro da instalação do jogo nem dentro da árvore do repositório.

#### Scenario: Local dos backups

- **WHEN** um backup é criado
- **THEN** ele fica no diretório de dados local do usuário, em uma pasta dedicada à ferramenta
- **AND** nenhum arquivo é criado dentro da instalação do jogo

#### Scenario: Aplicativo em pasta somente-leitura

- **WHEN** o executável do aplicativo está em uma pasta onde não se pode gravar
- **THEN** a criação de backup continua funcionando
- **AND** a gravação de preferência do perfil continua funcionando
