# profile-application Specification

## Purpose

Define como o aplicativo valida um perfil de performance contra o schema do jogo e contra o próprio executável, e como o aplica nas três camadas de configuração preservando ao máximo o conteúdo original dos arquivos.

## Requirements

### Requirement: As opções de menu são validadas contra o schema do jogo

O aplicativo SHALL validar todo valor de opção de menu do perfil contra o esquema declarado pelo jogo antes de escrever, e SHALL recusar a aplicação quando um valor não for aceito.

#### Scenario: Valor fora da lista de aceitos

- **WHEN** o perfil define uma opção de lista com um valor que não consta no esquema do jogo
- **THEN** o aplicativo identifica a opção e o valor aceito
- **AND** recusa a aplicação, informando o valor inválido e a lista de valores válidos
- **AND** nenhuma configuração é alterada

#### Scenario: Opção inexistente no esquema

- **WHEN** o perfil define uma opção de menu que não existe no esquema do jogo
- **THEN** o aplicativo identifica a opção pelo nome
- **AND** recusa a aplicação

#### Scenario: Todos os valores conferem

- **WHEN** todos os valores de menu do perfil constam do esquema do jogo
- **THEN** o aplicativo reporta a conformidade
- **AND** prossegue para a validação das chaves de cvar

### Requirement: As chaves de cvar são validadas contra o executável

O aplicativo SHALL verificar se o nome de cada chave de cvar do perfil existe como token isolado no executável do jogo, e SHALL recusar a aplicação quando uma chave não existir, com exceção das chaves documentadas como ausentes.

#### Scenario: Chave inexistente é detectada

- **WHEN** o perfil define uma chave de cvar cujo nome não aparece no executável
- **THEN** o aplicativo identifica a chave e a seção em que foi declarada
- **AND** recusa a aplicação, informando que o motor ignora chaves desconhecidas em silêncio
- **AND** nenhuma configuração é alterada

#### Scenario: Chave documentada como ausente é aceita

- **WHEN** o perfil define uma chave de cvar listada como exceção conhecida
- **THEN** o aplicativo reporta a exceção como aviso, com a referência à documentação
- **AND** prossegue com a aplicação

#### Scenario: Todas as chaves conferem

- **WHEN** toda chave de cvar do perfil existe como token isolado no executável, exceto as exceções documentadas
- **THEN** o aplicativo reporta a contagem de chaves confirmadas e de exceções
- **AND** prossegue com a aplicação

### Requirement: A escrita das opções de menu é cirúrgica

O aplicativo SHALL alterar apenas os campos de valor e de índice das opções visadas no arquivo de opções do jogo, e SHALL manter inalterado o restante do conteúdo do arquivo.

#### Scenario: Arquivo com opções não visadas

- **WHEN** o perfil define opções de menu
- **THEN** o aplicativo reescreve no arquivo apenas os campos de valor e de índice das opções visadas
- **AND** as demais opções, a ordem dos grupos e a formatação do arquivo permanecem byte a byte iguais

#### Scenario: Verificação após a escrita

- **WHEN** o aplicativo termina de escrever o arquivo de opções do jogo
- **THEN** relê o arquivo e compara o resultado com o esperado
- **AND** em caso de divergência, informa a falha em vez de considerar a operação bem-sucedida

### Requirement: A escrita das cvars preserva a estrutura do arquivo

O aplicativo SHALL preservar os comentários, a ordem das seções e a ordem das chaves do arquivo de cvars do motor, acrescentando ao final apenas as seções e chaves que faltarem.

#### Scenario: Seção existente recebe novas chaves

- **WHEN** o perfil define uma chave nova para uma seção que já existe no arquivo
- **THEN** a chave é acrescentada à seção, sem remover as demais
- **AND** os comentários existentes da seção são mantidos

#### Scenario: Seção inexistente é criada

- **WHEN** o perfil define chaves para uma seção que não existe no arquivo
- **THEN** a seção é criada ao final do arquivo, com as chaves declaradas

#### Scenario: Comentário do motor sobrevive à aplicação

- **WHEN** o arquivo de cvars contém comentários que documentam efeitos colaterais
- **THEN** os comentários permanecem no arquivo após a aplicação

### Requirement: Valor e índice são reconciliados

O aplicativo SHALL escrever, para cada opção de lista visada, o valor e o índice de forma coerente entre si, e SHALL reconciliar o índice de opções que já estejam corretas.

#### Scenario: Opção de lista alterada

- **WHEN** o perfil define um valor diferente do atual para uma opção de lista
- **THEN** o aplicativo escreve o novo valor
- **AND** ajusta o índice para a posição desse valor na lista do esquema

#### Scenario: Valor já correto com índice desatualizado

- **WHEN** o valor de uma opção de lista já é o desejado, mas o índice aponta para outro item
- **THEN** o aplicativo ajusta apenas o índice

### Requirement: A variante de HUD é uma opção liga e desliga

O aplicativo SHALL apresentar a variante de HUD como uma opção liga e desliga, e ao desligá-la SHALL restaurar o valor padrão do esquema do jogo para as opções que a variante affecta.

#### Scenario: Variante ligada

- **WHEN** o usuário liga a variante de HUD e aplica o perfil
- **THEN** as opções da variante são aplicadas por cima do perfil base

#### Scenario: Variante desligada

- **WHEN** o usuário desliga a variante de HUD e aplica o perfil
- **THEN** as opções afetadas pela variante voltam ao valor padrão do esquema do jogo
- **AND** as demais opções do perfil base são aplicadas normalmente

#### Scenario: Alternar a variante repetidamente

- **WHEN** o usuário liga e desliga a variante em sequência, aplicando entre uma e outra
- **THEN** o resultado final reflete o estado da variante no momento da aplicação

### Requirement: A simulação não escreve nada

O aplicativo SHALL apresentar todas as alterações que uma aplicação faria, sem escrever em nenhum arquivo.

#### Scenario: Usuário solicita a simulação

- **WHEN** o usuário solicita uma simulação
- **THEN** o aplicativo lista cada arquivo que seria alterado e cada valor antes e depois
- **AND** nenhum arquivo é criado, alterado ou removido
- **AND** nenhum backup é gerado

### Requirement: As alterações aplicadas são registradas

O aplicativo SHALL registrar cada aplicação, indicando os arquivos alterados e, para cada valor, a origem do valor aplicado.

#### Scenario: Aplicação concluída

- **WHEN** uma aplicação altera pelo menos um arquivo
- **THEN** o aplicativo registra os arquivos alterados
- **AND** para cada valor alterado, registra a origem: perfil base, variante, ou valor padrão do esquema

#### Scenario: Aplicação sem mudança

- **WHEN** uma aplicação não altera nenhum arquivo
- **THEN** o aplicativo informa que não houve alteração
- **AND** não gera backup

### Requirement: O perfil é editável e persistido

O aplicativo SHALL permitir editar o perfil, SHALL persistir o perfil editado em um arquivo próprio do usuário, e SHALL entregar o perfil curado do repositório como perfil inicial.

#### Scenario: Primeira execução

- **WHEN** o usuário executa o aplicativo e ainda não há um perfil de usuário
- **THEN** o aplicativo usa o perfil curado que acompanha o aplicativo

#### Scenario: Perfil editado e salvo

- **WHEN** o usuário edita um valor do perfil e salva
- **THEN** o perfil editado é gravado no diretório de dados do usuário
- **AND** as edições sobrevivem ao fechamento do aplicativo
- **AND** o perfil curado que acompanha o aplicativo não é alterado

#### Scenario: Perfil do usuário volta ao padrão

- **WHEN** o usuário descarta as edições
- **THEN** o perfil curado que acompanha o aplicativo volta a ser o perfil em uso
