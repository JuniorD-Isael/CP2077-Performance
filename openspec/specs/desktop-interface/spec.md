# desktop-interface Specification

## Purpose

Define como o aplicativo se apresenta ao usuário: idioma e tema derivados do sistema operacional, exposição das cvars do motor com a documentação do próprio jogo, e exibição do estado que o jogo está de fato usando.

## Requirements

### Requirement: O idioma segue o sistema operacional

O aplicativo SHALL usar português quando o idioma do sistema for português do Brasil, e SHALL usar inglês em qualquer outro idioma.

#### Scenario: Sistema em português do Brasil

- **WHEN** o idioma do sistema operacional é português do Brasil
- **THEN** toda a interface do aplicativo é apresentada em português

#### Scenario: Sistema em outro idioma

- **WHEN** o idioma do sistema operacional não é português do Brasil
- **THEN** toda a interface do aplicativo é apresentada em inglês

#### Scenario: Idioma desconhecido ou não definido

- **WHEN** o idioma do sistema operacional não está definido ou é um idioma sem tradução
- **THEN** a interface é apresentada em inglês
- **AND** nenhuma string fica sem tradução ou sem traduzir

### Requirement: O tema segue o sistema operacional

O aplicativo SHALL adotar o tema claro ou escuro conforme a preferência de aplicativo do sistema operacional, e SHALL se ajustar a diferentes escalas de exibição do monitor.

#### Scenario: Sistema em tema escuro

- **WHEN** a preferência de aplicativo do sistema é escura
- **THEN** o aplicativo é apresentado com cores escuras

#### Scenario: Sistema em tema claro

- **WHEN** a preferência de aplicativo do sistema é clara
- **THEN** o aplicativo é apresentado com cores claras

#### Scenario: Monitor com escala de exibição elevada

- **WHEN** o aplicativo é executado em um monitor com escala de exibição diferente de 100%
- **THEN** os textos e controles permanecem legíveis e proporcionais

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

### Requirement: O estado da aplicação é apresentado na tela principal

A tela principal SHALL apresentar se o perfil está aplicado e quando foi, SHALL informar quantas opções divergem do valor que o jogo está usando de fato, e SHALL oferecer a reversão a partir do estado apresentado.

#### Scenario: Perfil aplicado

- **WHEN** o perfil está aplicado no jogo
- **THEN** a tela principal informa que está aplicado e quando foi
- **AND** oferece a reversão

#### Scenario: Divergência com o jogo

- **WHEN** o valor em vigor no jogo difere do desejado
- **THEN** a tela principal informa quantas opções divergem

#### Scenario: Perfil nunca aplicado

- **WHEN** o perfil não foi aplicado
- **THEN** a tela principal apresenta o estado vazio
- **AND** não apresenta um estado falso

### Requirement: O resultado de uma operação é apresentado de forma distinguível

O aplicativo SHALL apresentar o resultado de uma operação com um rótulo próprio que identifique sucesso, falha parcial ou erro, e SHALL apresentar a razão da falha quando houver.

#### Scenario: Sucesso

- **WHEN** uma aplicação é concluída com sucesso
- **THEN** o resultado é apresentado com o rótulo de sucesso

#### Scenario: Falha

- **WHEN** uma aplicação falha
- **THEN** o resultado é apresentado com o rótulo de erro
- **AND** a razão da falha é apresentada

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

### Requirement: Um mod de HUD remanescente é reportado

O aplicativo SHALL reportar, sem remover, a presença do mod `CP2077Performance` no diretório de mods do Cyber Engine Tweaks, informando que a alternância de HUD agora pertence a outro projeto.

#### Scenario: Mod antigo encontrado

- **WHEN** o diretório do mod `CP2077Performance` existe na pasta do jogo
- **THEN** o aplicativo informa que o mod é de uma versão anterior e que o aplicativo não o gerencia mais
- **AND** não apaga nem altera o diretório

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
