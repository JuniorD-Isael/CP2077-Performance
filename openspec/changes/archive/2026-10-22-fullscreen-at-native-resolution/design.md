# Design

## Context

Ver `proposal.md` para o motivo. O que condiciona a abordagem:

**O caso concreto.** Monitor de 1600×900; o jogo em `1366x768` com
`WindowMode = BorderlessWindowed`. As duas opções vivem no grupo
`/video/display` do `UserSettings.json` e **existem no arquivo** — o
escritor do aplicativo já as alcança.

**O schema não diz quais valores são aceitos.** `WindowMode`, `Resolution` e
`ResolutionScaling` são declaradas com `"is_dynamic": true` e **sem lista de
valores**. O motor monta essa lista em runtime, a partir do monitor. O
`ProfileValidator` já aceita opções nessa situação (uma opção sem valores
aceitos não gera erro de validação), então a validação **não** é o
obstáculo. O obstáculo é o `index`.

**O `index` aponta para a lista do motor, e essa lista não é a do Windows.**
No arquivo do usuário, `Resolution` vale `1366x768` com `index=2`. A
enumeração do Windows (`EnumDisplaySettings`) coloca `1366x768` nos índices
18–20 do monitor de 1920×1080, e bem mais adiante no de 1600×900. A lista
do motor é outra, e sua ordem só é observável pelo que o próprio jogo
grava.

**O que a investigação já provou sobre `value` contra `index`.**
`FINDINGS.md` §13.3 refuta a hipótese para `HDDMode` e `HDDMode2`: o motor
**recalcula** os dois campos, porque o modo de disco é derivado do
armazenamento. Isso não diz nada sobre `Resolution` e `WindowMode`, que são
preferências do usuário. O `ROADMAP.md` §2.1 registra a questão em aberto.

**O que já funciona e não precisa ser refeito.** A escrita cirúrgica, a
reconciliação de `value`/`index` para as opções com lista conhecida, o
backup com anel de três, a recusa por jogo em execução. A camada de menu
continua exatamente como está.

## Goals / Non-Goals

**Goals:**

- O jogo abre **ocupando a tela toda**, na resolução nativa do monitor.
- A resolução **interna** do jogo não muda: o DRS continua variando entre
  50% e 85%, como hoje.
- A resolução de saída é **derivada da máquina**, nunca um valor fixo no
  perfil.
- O aplicativo **descobre o contrato** das opções dinâmicas em vez de
  adivinhá-lo.

**Non-Goals:**

- **Não tocar no DRS**, nem no `ResolutionScaling`, nem na escala de
  qualidade. A receita é de tela, não de nitidez.
- **Não mudar o perfil.** Nenhum campo novo, nenhum valor novo.
- **Não resolver a questão `value` contra `index` para as demais opções.**
  O `ROADMAP.md` §2.1 continua sendo o dono dela; esta change só precisa da
  resposta para as opções de exibição.
- **Não criar um perfil por máquina.**

## Decisions

### D1 — A resolução de saída vem do monitor, não do perfil

**Escolha.** O aplicativo enumera os monitores, resolve a resolução nativa
de cada um e oferece a escolha. O monitor principal do sistema é o padrão.

**Por quê.** A resolução de saída é uma propriedade da **máquina**, não do
perfil. Um perfil que carrega `Resolution = "1600x900"` seria errado em
qualquer outra máquina — e o perfil é a fonte única de verdade que o
aplicativo entrega ao usuário. A escolha fica no **aplicativo**, junto das
outras decisões que dependem da máquina (a pasta do jogo).

**Alternativa descartada: a resolução no perfil, com o app corrigindo.** O
perfil declararia a intenção (`"usar a resolução nativa"`) e o app
resolveria. Mais elegante no papel, mas transforma o perfil em um documento
com duas regras de interpretação, e o valor efetivo passaria a existir em dois
lugares. O app escreve o valor concreto e o registro de aplicação mostra o
que foi aplicado.

### D2 — O `index` das opções dinâmicas é aprendido com o jogo, não deduzido

**Escolha.** O aplicativo lê do `UserSettings.json` o par `(valor, índice)`
que o próprio jogo gravou, e guarda o índice num mapa persistido. Quando
escreve uma resolução cujo índice ele conhece, escreve o valor **e** o
índice; quando não conhece, escreve só o valor.

**Por quê.** A hipótese inicial — a lista do motor ser a lista de modos
distintos do monitor, em ordem decrescente — **não se confirmou**. O motor
gravou `1600x900` com índice **4**, e a regra previa 0. E o executavel
contem `1366x768`? Não: a string não existe no binário, o que mostra que a
lista de resoluções é montada em runtime, a partir do monitor, e que a
enumeração do Windows não é a mesma lista.

O que o experimento revelou é mais forte e mais simples: **o motor resolve a
opção pelo `value` e recalcula o índice**. Com o arquivo marcando
`Fullscreen` com índice 0, o jogo o regrava com índice 2; com `1600x900` com
índice 2, o jogo o regrava com índice 4. O índice é consequência, não
entrada.

**Consequência.** O índice deixa de ser uma aposta: quando o aplicativo não o
sabe, o motor o corrige no próximo início. O aplicativo escreve o índice
para deixar o arquivo coerente entre uma aplicação e outra, e **aprende** o
índice que o motor gravou, para saber na próxima vez.

### D3 — O experimento do contrato já foi feito, e a resposta é o `value`

**Escolha.** A aplicação ao vivo não depende do índice. O aplicativo escreve
o `value` de `WindowMode` e de `Resolution`, e o índice quando o conhece.

**O que o experimento mostrou.** O motor mantém o valor e recalcula o
índice. Isso vale para as duas opções dinâmicas, e é a resposta à pergunta
que o `ROADMAP.md` §2.1 deixou aberta para as demais opções.

**A resposta que sobrou.** A ordem da lista do motor continua desconhecida —
e deixou de importar. O que importa é o valor, e o valor é o que o
aplicativo escreve.

**Por que o experimento não virou spec.** Uma spec que afirmasse "o motor
lê o índice" seria uma afirmação sem evidência. O que a spec afirma é o
comportamento observável: o aplicativo escreve as opções de exibição com
valor e índice coerentes, e quando não sabe o índice, escreve o valor e
informa que a configuração entra em vigor no próximo início do jogo.

### D4 — A escrita do `index` é uma extensão do escritor, não um caso especial

**Escolha.** O `MenuOptionsWriter` passa a aceitar, para uma opção, um
`index` fornecido de fora, quando a lista de valores aceitos do schema está
vazia (opções dinâmicas).

**Por quê.** A reconciliação de `value`/`index` já existe e já foi provada
(§13.3, `ROADMAP.md` §2.1). O que falta é a **fonte** do índice. Fazer
dessa fonte um parâmetro da escrita mantém a lógica de reconciliação em um
lugar só — em vez de duplicar a escrita surgically em dois caminhos.

**Alternativa descartada: escrever as opções de exibição fora do pipeline
do perfil.** Teria dois escritores, duas políticas de backup, dois
registros. O registro de aplicação deixaria de mostrar o que aconteceu.

### D5 — A escolha vive na tela principal, ao lado das variantes

**Escolha.** O modo de exibição é uma escolha na tela principal, no mesmo
padrão das variantes: rótulo, resumo em linguagem corrente, e o efeito
explicado.

**Por quê.** A tela principal já é o lugar onde o usuário decide o que o
jogo vai ter. O modo de exibição é uma decisão do mesmo tipo — e o resumo
"o jogo abre em tela cheia na resolução do monitor; a resolução interna
continua a mesma" é o mesmo tipo de frase que o resumo de uma variante.

**Alternativa descartada: uma aba nova de configurações.** A aba
introduziria um segundo lugar para tomar decisões, e é exatamente o que a
change anterior desfez.

### D6 — Desligar a escolha devolve o que o jogo tinha

**Escolha.** A primeira vez que o aplicativo mexe nas opções de exibição, ele
guarda os valores que o jogo tinha. Desligar a escolha devolve esses
valores.

**Por quê.** Sem isso, a escolha é uma via única: ligar coloca o jogo em tela
cheia, e desligar não desfaz — o jogo continua em tela cheia e o usuário não
tem caminho, a não ser editar o arquivo por conta própria.

**Alternativa descartada: "manter como está" significa não tocar.** Era a
redação inicial, e ela é incoerente com o nome: o efeito observável de
desligar a escolha seria o jogo **continuar** com o que o aplicativo mudou.
Devolver o estado anterior é o que torna a escolha uma decisão, e não um
botão que só liga.

**De onde vem o estado anterior.** Das próprias opções do jogo, lidas antes
da primeira escrita, e guardadas na pasta de dados do aplicativo. Não vem do
backup: o backup é a rede de segurança, não um armazenamento de configuração.

## Risks / Trade-offs

- **[O motor ignorar a resolução escrita]** → **medido: o motor não ignora.**
  Com `Fullscreen` de índice 0 no arquivo, o jogo o regrava com índice 2, e
  com `1600x900` de índice 2, com índice 4: o valor foi mantido e o índice
  recalculado. O motor resolve pelo `value`.
- **[A lista do motor mudar com o driver ou com o monitor]** → deixa de
  importar para o efeito: o índice é recalculado pelo motor. O aplicativo
  escreve o índice que aprendeu, apenas para deixar o arquivo coerente entre
  uma aplicação e outra.
- **[A resolução nativa do monitor não ser um modo que o motor aceita]** → o
  aplicativo só oferece resoluções que enumerou no próprio monitor, e o
  esquema do jogo valida o resto.
- **[O jogo reescrever as opções de exibição ao encerrar]** → comportamento
  conhecido e já documentado (§13.4): o jogo normaliza o estado em memória
  ao subir e descarrega na saída. **É o que conserta o índice.** O
  aplicativo já recusa aplicar com o jogo em execução, e a janela entre o
  apply e o próximo início é o mesmo intervalo que já existe para as demais
  camadas.
- **[O modo de exibição virar uma opção que o motor recalcula]** → medido: o
  motor recalcula o **índice**, e não o valor. A configuração é respeitada.

## Open Questions

- **A lista de resoluções do motor é a lista de modos do monitor, ou uma
  lista curada?** Continua sem resposta, e **deixou de importar**: o motor
  recalcula o índice a partir do valor. Fica registrada por completeza.
- **O monitor em que o jogo roda é o principal, ou o `OutputMonitor` do
  jogo?** O `OutputMonitor` do usuário vale `1`, e a semântica do valor não
  está documentada. O aplicativo oferece a escolha explícita e usa o
  principal como padrão — o que, na máquina de referência, é o monitor onde
  o jogo rodava.
- **O DRS deve ficar desligado quando a resolução de saída é a nativa?**
  Hoje a escala dinâmica flutua entre 50% e 85% da resolução de saída. Com a
  saída maior, o teto interno sobe. O usuário **não** pediu para mudar a
  resolução interna, e a decisão registrada foi manter a variação como
  está — mas vale a pena medir depois.
