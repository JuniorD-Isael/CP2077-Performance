# Proposal

## Why

O jogo abre numa **janela que não preenche a tela**. Na máquina de referência: monitor de **1600×900**, e o jogo em `1366x768` com `WindowMode = BorderlessWindowed` — uma janela sem borda de 1366×768 no meio de um monitor de 1600×900, com o resto preto.

Isso é uma escolha do usuário, feita dentro do jogo, porque **o aplicativo não alcança as opções de exibição**. E não alcança por um motivo técnico preciso: `WindowMode` e `Resolution` são declaradas pelo esquema do jogo como **`is_dynamic: true`**, sem lista de valores. A lista de modos aceitos é montada pelo motor em runtime, a partir do monitor. O schema do jogo, que é a única fonte de verdade que o aplicativo consulta, não a contém.

Resultado: das 328 opções que o jogo registra, as duas que decidem como a imagem ocupa a tela são exatamente as duas que o perfil não alcança.

O efeito é maior do que parece. A resolução de saída é o que determina **quantos pixels o motor tem para renderizar** e, com o DRS ligado, o teto da resolução interna. Uma janela de 1366×768 numa tela de 1600×900 desperdiça 36% da área — e, no sentido inverso, obriga o usuário a escolher entre a tela cheia e a nitidez.

## What Changes

- O aplicativo passa a **determinar o monitor em que o jogo roda** e a resolução nativa desse monitor.
- O aplicativo passa a **escrever as opções de exibição** — `WindowMode` e `Resolution` — de forma consistente com o que o motor espera, e não apenas o `value`.
- A **escrita de `index`** deixa de depender exclusivamente da lista de valores do schema: as opções dinâmicas recebem o índice que o motor usa.
- O aplicativo passa a **oferecer o modo de exibição** como escolha explícita: manter como está, ou abrir em **fullscreen na resolução do monitor**.
- A resolução **interna** não muda: o DRS continua variando entre 50% e 85%, como hoje. O que muda é que a saída passa a ocupar a tela toda.

**Sem quebra de formato:** o perfil não ganha nenhum campo novo. O modo de exibição é uma escolha do **aplicativo**, porque a resolução de saída depende da máquina e não pode ser um valor fixo no perfil.

## Capabilities

### New Capabilities
- `display-mode`: como o aplicativo determina o monitor do jogo, a resolução nativa, e como escreve as opções de exibição de forma consistente com o motor.

### Modified Capabilities
- `desktop-interface`: a tela principal passa a oferecer o modo de exibição.

## Impact

**Aplicativo** (`app/CP2077Performance.Core`, `app/CP2077Performance.Ui`): uma fonte nova de modes do display (`EnumDisplaySettings`), a escrita das opções dinâmicas com índice, e a escolha na tela principal.

**Documentação** (`README.md`, `docs/ARQUITETURA.md`): a receita de fullscreen em resolução nativa, e o contrato das opções dinâmicas.

**Perfil** (`profile/lowspec-30fps.json`): **inalterado**. O DRS, o `ResolutionScaling` e a escala de qualidade continuam como estão.

**Risco assumido e registrado:** o motor lê o campo `value` ou o campo `index` das opções dinâmicas? `FINDINGS.md` §13.3 **refuta** a hipótese para `HDDMode` e `HDDMode2` — o motor recalcula os dois. Mas para `Resolution` e `WindowMode` a questão está **aberta**, e é o `ROADMAP.md` §2.1 aplicado às opções de exibição. O valor de `index` no arquivo do usuário (`1366x768` com `index=2`) **não** corresponde à ordem da enumeração do Windows, o que prova que a lista do motor é outra — e que ela não é derivável do schema. **O primeiro passo desta change é descobrir esse contrato com um experimento controlado**, não deduzi-lo.

**Riscos:** a lista de modos do motor só é observável pelo que o próprio jogo grava. Sem esse conhecimento, o aplicativo pode escrever a resolução e o índice errado, e o jogo pode ignorar a opção. O desenho registra o caminho de queda: o aplicativo escreve o `value` sempre, e o `index` só quando ele é conhecido — com a validação de que o app consegue ler e escrever, e o jogo continua aceitando o perfil.
