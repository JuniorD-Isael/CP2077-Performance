# game-installation Specification

## Purpose

Define como o aplicativo identifica e valida a instalação do Cyberpunk 2077 a partir do caminho informado pelo usuário, deriva os alvos de configuração de cada camada do jogo, e recusa operar enquanto o jogo estiver em execução.

## Requirements

### Requirement: O usuário informa a pasta do jogo

O aplicativo SHALL aceitar a pasta de instalação do jogo informada pelo usuário, SHALL persistir essa escolha entre execuções, e SHALL NOT varrer o sistema de arquivos em busca de instalações.

#### Scenario: Usuário informa o caminho pela primeira vez

- **WHEN** o usuário executa o aplicativo pela primeira vez
- **THEN** o aplicativo apresenta um campo para a pasta do jogo, vazio
- **AND** nenhuma busca por instalações é realizada no disco

#### Scenario: Caminho persistido entre execuções

- **WHEN** o usuário informou uma pasta válida e executa o aplicativo novamente
- **THEN** o campo da pasta do jogo já vem preenchido com o valor anterior
- **AND** o aplicativo revalida a pasta em vez de confiar no valor salvo

#### Scenario: Caminho inválido é recusado com explicação

- **WHEN** o usuário informa uma pasta que não contém uma instalação do jogo
- **THEN** o aplicativo informa que a pasta não é uma instalação válida
- **AND** aponta o arquivo esperado que não foi encontrado
- **AND** nenhuma alteração de configuração é tentada

### Requirement: A instalação é validada pela presença do executável

O aplicativo SHALL considerar uma pasta uma instalação válida quando o executável principal do jogo existe dentro dela, e SHALL reportar essa verificação ao usuário.

#### Scenario: Pasta contém o executável

- **WHEN** a pasta informada contém `bin\x64\Cyberpunk2077.exe`
- **THEN** o aplicativo considera a instalação válida
- **AND** habilita a operação sobre as camadas

#### Scenario: Pasta não contém o executável

- **WHEN** a pasta informada não contém `bin\x64\Cyberpunk2077.exe`
- **THEN** o aplicativo considera a instalação inválida
- **AND** informa o caminho relativo que deveria existir

### Requirement: Os alvos de cada camada são derivados da pasta do jogo

O aplicativo SHALL derivar da pasta do jogo o caminho da camada de cvars do motor e da camada de patches do Cyber Engine Tweaks, e SHALL derivar da pasta de dados local do sistema operacional o caminho da camada de opções de menu.

#### Scenario: As três camadas estão presentes

- **WHEN** a instalação possui `engine\config\platform\pc\user.ini`, o diretório local de dados do usuário possui o arquivo de opções do jogo, e o Cyber Engine Tweaks está instalado
- **THEN** o aplicativo identifica as três camadas como disponíveis
- **AND** deriva o caminho de cada uma a partir da pasta informada

#### Scenario: Cyber Engine Tweaks não está instalado

- **WHEN** a instalação não possui o diretório de plugins do Cyber Engine Tweaks
- **THEN** o aplicativo identifica a camada de patches como indisponível
- **AND** informa que a camada será ignorada
- **AND** as demais camadas seguem operacionais

#### Scenario: O diretório de save é o mesmo para qualquer loja

- **WHEN** o usuário aponta a instalação de uma loja qualquer
- **THEN** o caminho do arquivo de opções de menu é derivado do diretório de dados local do sistema operacional
- **AND** não depende do nome da loja nem do caminho da instalação

### Requirement: A operação é recusada com o jogo em execução

O aplicativo SHALL recusar aplicar o perfil enquanto o processo do jogo estiver em execução, e SHALL explicar o motivo. Quando o processo não encerrar por conta própria, a mensagem SHALL orientar como encerrá-lo.

#### Scenario: Jogo em execução no momento de aplicar

- **WHEN** o usuário tenta aplicar o perfil e o processo do jogo está em execução
- **THEN** o aplicativo recusa a operação
- **AND** informa que o jogo sobrescreve as opções de menu ao encerrar, anulando a aplicação
- **AND** nenhuma configuração é alterada

#### Scenario: Jogo encerrado entre a checagem e a aplicação

- **WHEN** o jogo está em execução na checagem e termina antes da aplicação
- **THEN** a operação prossegue normalmente
- **AND** o estado do jogo é verificado de novo imediatamente antes de escrever

#### Scenario: Processo que não encerra

- **WHEN** o jogo já foi fechado mas o processo ainda consta como em execução
- **THEN** a mensagem de recusa orienta a encerrar o processo pelo Gerenciador de Tarefas
- **AND** a operação continua recusada

### Requirement: A origem da instalação é apresentada ao usuário

O aplicativo SHALL apresentar a origem de cada alvo derivado, para que o usuário possa conferir de onde cada arquivo vem.

#### Scenario: Usuário confere a origem dos alvos

- **WHEN** o aplicativo apresenta as camadas disponíveis
- **THEN** mostra o caminho completo de cada arquivo alvo
- **AND** indica a pasta do jogo como origem das camadas de motor e de patches
- **AND** indica o diretório de dados local do sistema como origem da camada de menu

### Requirement: Um painel in-game remanescente é detectado e reportado

O aplicativo SHALL detectar a presença de um painel in-game anterior na instalação do jogo e SHALL reportá-lo ao usuário, informando que ele não aplica mais nada e pode ser removido.

#### Scenario: Painel antigo ainda instalado

- **WHEN** a instalação do jogo contém o diretório do painel in-game anterior
- **THEN** o aplicativo informa que encontrou esse painel
- **AND** informa que ele deixou de ser funcional e que a configuração passa a ser feita pelo aplicativo
- **AND** oferece a remoção do painel remanescente

#### Scenario: Painel já removido

- **WHEN** a instalação do jogo não contém o diretório do painel in-game anterior
- **THEN** o aplicativo não reporta nada a respeito dele
