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

### Requirement: Uma variante é um conjunto nomeado, aplicável às duas camadas

O perfil SHALL definir variantes como conjuntos nomeados, cada um com rótulo, resumo em linguagem corrente e destaques do efeito. Uma variante SHALL poder sobrescrever tanto as opções de menu quanto as chaves de cvar do motor. Ao desligar uma variante, o aplicativo SHALL restaurar, nas duas camadas, o valor padrão do esquema do jogo para as opções que ela afetou.

#### Scenario: Variante ligada

- **WHEN** o usuário liga uma variante e aplica o perfil
- **THEN** as opções de menu da variante são aplicadas por cima do perfil base
- **AND** as chaves de cvar da variante são aplicadas na seção correspondente do arquivo de cvars

#### Scenario: Variante desligada

- **WHEN** o usuário desliga uma variante e aplica o perfil
- **THEN** as opções de menu afetadas pela variante voltam ao valor padrão do esquema do jogo
- **AND** as chaves de cvar afetadas pela variante deixam de ser escritas
- **AND** as demais opções e chaves do perfil base são aplicadas normalmente

#### Scenario: Alternar a variante repetidamente

- **WHEN** o usuário liga e desliga a variante em sequência, aplicando entre uma e outra
- **THEN** o resultado final reflete o estado da variante no momento da aplicação

#### Scenario: Variante com chaves de cvar

- **WHEN** a variante declara uma chave de cvar que ainda não existe no arquivo do motor
- **THEN** a chave é acrescentada na seção correta, preservando a estrutura do arquivo
- **AND** a aplicação é concluída sem erro

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

### Requirement: A variante declara seu efeito em linguagem corrente

Cada variante SHALL declarar um rótulo, um resumo em linguagem corrente e a lista de destaques do que altera, para que o aplicativo explique a escolha sem expor identificadores técnicos. O aplicativo SHALL continuar aplicando variantes que não declaram esses campos, apresentando-as sem resumo.

#### Scenario: Variante com resumo e destaques

- **WHEN** a variante declara rótulo, resumo e destaques
- **THEN** o aplicativo apresenta o rótulo, o resumo e os destaques da variante

#### Scenario: Variante sem resumo

- **WHEN** a variante não declara rótulo, resumo nem destaques
- **THEN** o aplicativo a aplica normalmente
- **AND** a apresenta sem resumo, sem falhar a aplicação

#### Scenario: Perfil gravado no formato anterior

- **WHEN** o perfil do usuário em disco tem uma variante sem resumo e sem chaves de cvar
- **THEN** o aplicativo a lê e a aplica como uma variante do menu
- **AND** não descarta a variante nem falha ao carregar o perfil

### Requirement: O perfil curado não altera o HUD

O perfil curado SHALL NOT declarar opções de menu, cvars ou variantes cujo efeito seja alterar elementos do HUD do jogo. O mecanismo de variantes SHALL permanecer disponível, e um id de variante persistido que o perfil não declara mais SHALL ser ignorado sem erro.

#### Scenario: Perfil sem opções de HUD

- **WHEN** o perfil curado é carregado
- **THEN** ele não declara a variante `hudMinimal` nem opções como `minimap`, `quest_tracker` ou `npc_nameplates`

#### Scenario: Variante salva que o perfil não declara mais

- **WHEN** o estado salvo referencia o id `hudMinimal` e o perfil carregado não o declara
- **THEN** o aplicativo ignora esse id sem erro
- **AND** não escreve nem remove opções do jogo por causa dele
