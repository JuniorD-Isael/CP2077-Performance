# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

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

## REMOVED Requirements

### Requirement: A simulação não escreve nada

**Reason**: A simulação respondia a uma pergunta de auditoria — "o que
exatamente mudaria?" — que a interface nova responde de outro jeito: a tela
principal mostra quantas configurações mudam, e a revelação do detalhe técnico
mostra o valor em vigor ao lado do valor desejado. A informação não se perde;
perde-se o botão.

**Migration**: Não há equivalente a migrar. Quem dependia da simulação
encontra o mesmo conteúdo na tela principal (a contagem de alterações) e na
revelação (as grades completas, com valor em vigor, valor desejado e
proveniência). O cálculo do plano continua no motor, porque `Apply()` depende
dele.

## RENAMED Requirements

- FROM: `### Requirement: A variante de HUD é uma opção liga e desliga`
- TO: `### Requirement: Uma variante é um conjunto nomeado, aplicável às duas camadas`
