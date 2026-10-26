# desktop-interface Specification Delta

## ADDED Requirements

### Requirement: A interface segue a identidade visual Cyberpunk Edgerunners

A interface SHALL adotar uma identidade visual geométrica inspirada na arte promocional de *Cyberpunk: Edgerunners*, com painéis e botões de cantos chanfrados em 45°, cabeçalho estilo HUD com faixa e marcadores técnicos, e SHALL usar a paleta *Edgerunners* — amarelo ácido `#fcee0a`, superfície clara `#f4f7fa`, verde neon Sandevistan `#00ff66` e azul-petróleo escuro `#04101c` — como origem de todas as suas cores, em variante escura e em variante clara.

#### Scenario: Tema escuro

- **WHEN** a preferência de aplicativo do sistema é escura
- **THEN** o fundo da interface usa `#04101c` e o texto usa `#f4f7fa`
- **AND** a ação principal e os destaques estruturais do cabeçalho usam `#fcee0a` com texto escuro `#04101c`
- **AND** o destaque de sucesso/estado ativo usa `#00ff66`

#### Scenario: Tema claro

- **WHEN** a preferência de aplicativo do sistema é clara
- **THEN** o fundo da interface usa `#f4f7fa` e o texto usa `#04101c`
- **AND** os tons de destaque derivados de `#fcee0a` e `#00ff66` mantêm contraste legível sobre o fundo claro

#### Scenario: Geometria chanfrada e sem tabelas técnicas

- **WHEN** a tela principal é desenhada
- **THEN** os cartões e botões principais usam recorte poligonal chanfrado em 45° em vez de cantos arredondados
- **AND** o cabeçalho exibe a moldura visual inspirada em *Cyberpunk: Edgerunners*

#### Scenario: Escala de exibição

- **WHEN** o aplicativo roda com escala de exibição diferente de 100%
- **THEN** os chanfros, os espaçamentos e os textos escalam proporcionalmente

### Requirement: O resumo do perfil é explicado na tela principal

O aplicativo SHALL explicar, em linguagem corrente, na tela principal, que aplica o perfil de desempenho junto com o modo de tela cheia mantendo a resolução do jogo, e SHALL informar quantas configurações serão alteradas no total.

#### Scenario: Resumo e contagem na tela principal

- **WHEN** o usuário abre a tela principal com uma pasta de jogo válida
- **THEN** a tela principal apresenta o resumo em linguagem corrente do que será aplicado
- **AND** informa quantas configurações serão alteradas no total

## MODIFIED Requirements

### Requirement: As operações ficam acessíveis a partir da interface

O aplicativo SHALL oferecer, de forma explícita, na tela principal, apenas as operações de validar a pasta do jogo, aplicar o perfil (incluindo tela cheia) e restaurar o último backup, e SHALL NOT oferecer operação de simulação nem botão ou painel de detalhe técnico.

#### Scenario: Usuário acessa as operações

- **WHEN** o aplicativo está em execução
- **THEN** as operações de aplicar o perfil e restaurar o último backup estão disponíveis diretamente na tela principal
- **AND** as operações que alteram o jogo são identificadas como tal

#### Scenario: Operação bloqueada

- **WHEN** uma operação não pode ser executada no estado atual
- **THEN** ela aparece desabilitada
- **AND** o motivo é apresentado ao usuário

#### Scenario: Não há operação de simulação

- **WHEN** o usuário procura uma operação de simulação na interface
- **THEN** nenhuma operação de simulação existe
- **AND** a quantidade de configurações que serão alteradas é apresentada na tela principal

#### Scenario: Demais operações

- **WHEN** o usuário observa a barra de ações da tela principal
- **THEN** não existe botão "Mostrar detalhe técnico" nem abas de tabelas na janela
- **AND** a referência técnica das camadas fica concentrada na documentação do projeto

### Requirement: A primeira execução orienta para a pasta do jogo

Quando nenhuma pasta de jogo foi informada, o aplicativo SHALL apresentar orientação para localizá-la em cada loja diretamente na tela principal.

#### Scenario: Primeira execução

- **WHEN** o aplicativo é aberto e nenhuma pasta foi informada
- **THEN** a orientação para localizar a pasta por loja aparece na tela principal

#### Scenario: Pasta já informada

- **WHEN** o aplicativo é aberto e uma pasta válida está guardada
- **THEN** a orientação de primeira execução não aparece

### Requirement: O aplicativo se restringe ao setup e à tela cheia

O aplicativo SHALL oferecer apenas a aplicação direta do perfil de setup para máquinas fracas em conjunto com o modo de janela em tela cheia. O aplicativo SHALL NOT exibir seção "O que aplicar" com seletor de monitores/resoluções, SHALL NOT exibir botão ou abas de detalhe técnico, SHALL NOT registrar tecla global, SHALL NOT instalar nem atualizar mods do Cyber Engine Tweaks e SHALL NOT apresentar controle ou estado de alternância de HUD em jogo.

#### Scenario: Tela principal sem controle de HUD em jogo

- **WHEN** o usuário abre a tela principal
- **THEN** ela apresenta apenas o cabeçalho visual, a seleção da pasta do jogo, o cartão de status e resumo do perfil e os botões de aplicar e restaurar backup
- **AND** não apresenta a seção "O que aplicar", seletor de monitor, botão "Mostrar detalhe técnico", tecla de alternância nem estado de HUD em jogo

#### Scenario: Setup sem escolha que altere o HUD

- **WHEN** o usuário abre a tela principal
- **THEN** nenhuma escolha de variante ou de HUD é oferecida na interface

#### Scenario: Nenhuma tecla global registrada

- **WHEN** o aplicativo está em execução, com ou sem o jogo aberto
- **THEN** nenhuma tecla global é registrada pelo aplicativo

#### Scenario: Nenhuma escrita no diretório de mods

- **WHEN** o usuário aplica o perfil
- **THEN** o aplicativo não cria nem altera arquivos em `cyber_engine_tweaks\mods`

## REMOVED Requirements

### Requirement: As cvars do motor são apresentadas com a documentação do jogo

**Reason**: As tabelas detalhadas de cvars do motor deixam de ocupar a interface do aplicativo e passam a constar na documentação do projeto.
**Migration**: Consultar `README.md`, `README.en.md` e `docs/ARQUITETURA.md` para a tabela completa de cvars e descrições.

### Requirement: O estado efetivo do jogo é apresentado

**Reason**: A aba técnica de comparação individual de opções de menu foi removida da interface; a tela principal mantém apenas o estado consolidado e a contagem de opções divergentes.
**Migration**: O estado aplicado e o número de divergências continuam visíveis no cartão de status da tela principal.

### Requirement: A tela principal oferece escolhas, não tabelas

**Reason**: A seção "O que aplicar" foi removida porque o aplicativo aplica sempre o perfil completo em tela cheia, sem escolhas intermediárias na interface.
**Migration**: O resumo do perfil e a contagem de alterações são exibidos diretamente no cartão de status da tela principal.

### Requirement: O efeito da escolha é explicado em linguagem corrente

**Reason**: Como não há mais seleção de variantes na interface, a explicação condicional por escolha foi substituída pelo resumo direto do perfil na tela principal.
**Migration**: Substituído pelo requisito `O resumo do perfil é explicado na tela principal`.

### Requirement: A apresentação detalhada fica atrás de uma revelação explícita

**Reason**: O botão "Mostrar detalhe técnico" e as 5 abas (`Perfil`, `Motor`, `Menu`, `Backups`, `Registro`) foram removidos do aplicativo para manter a interface limpa, movendo a referência técnica para a documentação.
**Migration**: Os detalhes técnicos das 3 camadas ficam documentados no repositório.

### Requirement: A interface segue uma base Material com paleta Cyberpunk

**Reason**: O visual Material genérico com cantos arredondados e paleta roxa (`#6564db`) foi substituído pela identidade visual geométrica chanfrada inspirada em *Cyberpunk: Edgerunners*.
**Migration**: Substituído pelo requisito `A interface segue a identidade visual Cyberpunk Edgerunners`.

### Requirement: A tela principal oferece o modo de exibição

**Reason**: O cartão de escolha de modo de exibição e o seletor de monitores/resoluções na tela principal foram removidos para não forçar resoluções específicas nem pedir validação desnecessária ao usuário.
**Migration**: O modo de janela em tela cheia (`WindowMode = Fullscreen`) é aplicado automaticamente junto com o perfil, preservando a resolução configurada no jogo.
