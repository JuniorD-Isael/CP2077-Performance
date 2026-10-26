# Design

## Context

Ver `proposal.md` para o motivo. O que condiciona a abordagem:

**O estado atual, medido.** A interface tem 5 abas, 134 linhas e 19 colunas
(536 células). Dela, cerca de 43% é ruído para quem vai jogar: a coluna de
procedência (74 células), a seção e a chave de 60 cvars (120 células) e a
coluna "desejado", que no perfil base mostra o mesmo que "valor" em 37 de 37
linhas. Das 59 strings de texto, 22 são de desenvolvimento.

**A restrição do formato.** Hoje `variants.<id>` só alcança
`userSettings.values` — a camada de menu. As 60 chaves de cvar, onde estão o
culling, a oclusão e o async compute, **não têm como variar**. Sem isso, uma
escolha do usuário só mexeria nas 37 opções de menu, que são a menor parte do
ganho.

**A interface de hoje.** `MainForm` monta tudo com controles padrão do
WinForms — `Button`, `Label`, `CheckedListBox`, `DataGridView`,
`TableLayoutPanel` — e a única decisão de cor é `Application.SetColorMode`,
que faz o aplicativo acompanhar o tema do sistema. Não há camada de tema:
cada controle nasce com a aparência do Windows. O projeto de UI **não tem
nenhuma dependência externa**; a publicação self-contained pesa ~103 MB só
com o runtime.

**A fileira de operações.** Hoje são quatro botões — Simular, Aplicar,
Restaurar, Descartar. Simular é o único que não escreve nada: ele monta o
plano e lista o que mudaria. É o botão de quem audita, não de quem joga.

**O que já existe e funciona.** A escrita cirúrgica, a validação contra o
esquema e contra o executável, o backup com anel de três, a reconciliação de
`value`/`index` e a leitura do estado efetivo. Nada disso precisa ser
refeito — a mudança é de **apresentação e de formato**, não de motor.

**O que não se sabe.** Se um conjunto menor de cvars realmente entrega o
mesmo FPS com menos perda visual. O A/B está pendente no `docs/ROADMAP.md`
(§2.2) e é o que calibraria as variantes.

## Goals / Non-Goals

**Goals:**

- A tela principal responde, sem esforço, a três perguntas: *o jogo foi
  encontrado?*, *o que vai acontecer?*, *está aplicado?*
- A interface oferece **escolhas** (variantes com rótulo e resumo), não
  tabelas de identificadores.
- O detalhe técnico continua disponível, completo, para quem o quer.
- A forma do perfil passa a permitir que uma variante afete as duas camadas.
- O material de desenvolvimento sai da interface e da frente do README.
- A interface ganha **identidade visual própria** — base Material com paleta
  Cyberpunk — em vez de herdada do Windows.

**Non-Goals:**

- **Não calibrar variantes novas.** Nenhuma variante entra no perfil curado
  nesta change. O mecanismo é entregue; o conteúdo depende de medição.
- **Não traduzir as 97 opções** para linguagem corrente. O efeito é descrito
  por variante, não por opção.
- **Não mexer no motor de aplicação** — nem na escrita cirúrgica, nem na
  validação, nem no backup.
- **Não criar um segundo aplicativo** (launcher, agente). A interface nova
  cabe na janela existente.
- **Não introduzir biblioteca de UI de terceiros.**
- **Não manter a pré-visualização de alterações como operação.**

## Decisions

### D1 — Variante como conjunto nomeado, não como nível exclusivo

**Escolha.** O usuário **marca** variantes (caixa de seleção), e várias
podem coexistir. Uma variante é um delta sobre o perfil base.

**Por quê.** Preserva a função existente — hoje a variante de HUD é uma
caixa independente, e quem a marca continua podendo marcar outra. E o eixo
"quanto visual eu posso perder" passa a ser expresso pela **ordem da lista e
pelo resumo**, não por um radio que impediria combinações.

**Alternativa descartada: níveis mutuamente exclusivos (rádio).** Explicaria
"escolha um" com mais facilidade, mas impediria combinar "visual alto" com
"HUD mínima", e obrigaria cada nível a ser uma configuração **completa** —
exatamente a manutenção que a mudança vem removendo: acrescentar um nível
significaria redefinir as 97 opções.

### D2 — O resumo vem do perfil, não de traduzir as 97 opções

**Escolha.** Cada variante declara, no próprio perfil, `label`, `summary` e
`highlights`. A aplicação mostra esses campos.

**Por quê.** Traduzir as 97 opções (60 cvars + 37 de menu) em dois idiomas
daria 194 strings autorais dentro do aplicativo, acopladas ao perfil: cada
opção nova exigiria uma string nova. Com o resumo por variante, são 3 a 5
strings por variante, escritas por quem **calibra** a variante — que é quem
sabe o que ela faz.

**Alternativa descartada: rótulo humano por opção.** Precisa, e é melhor
granularidade. Mas o volume e o acoplamento não compensam, e a granularidade
não é o problema: o problema é a **quantidade** de identificadores técnicos
na tela principal, que a revelação resolve.

### D3 — Três níveis de revelação na mesma janela

**Escolha.** A tela principal tem o essencial; uma ação de revelação abre a
região com as 5 abas existentes, intactas.

**Por quê.** As abas já funcionam e já têm os dados certos. Recolocá-las
atrás de uma revelação **não reescreve** a interface — só muda onde elas
estão. E a revelação preserva as colunas de procedência, que são a trilha
de auditoria e não devem sumir do produto.

### D4 — O estado vem do disco, não de memória

**Escolha.** A tela principal lê os arquivos do jogo para dizer se o perfil
está aplicado, e quantas opções divergem.

**Por quê.** O motor **recalcula** parte dos valores — `HDDMode`,
`HDDMode2` e `EnableTelemetry` já foram sobrescritos por ele, com o perfil
corretamente gravado. Um estado guardado em memória diria "aplicado" para
algo que o jogo já mudou. Ler o disco é a única leitura honesta.

### D5 — Desligar uma variante remove as chaves que ela acrescentou

**Escolha.** Ao desligar uma variante que declara chaves de cvar, o
aplicativo **remove** do `user.ini` as chaves que a variante teria
acrescentado — mas apenas se o valor atual for exatamente o que a variante
escreveria.

**Por quê.** O valor padrão do jogo para uma cvar é a **ausência** da
chave: o `user.ini` original não a tem. "Restaurar o padrão" numa cvar
significa removê-la, e o escritor atual só sabe acrescentar e substituir.
A condição de valor protege o que o usuário ajustou à mão: se o valor atual
diverge do da variante, a chave fica e o aplicativo reporta a divergência.

**Alternativa descartada: a variante nunca remove.** Deixaria a chave no
disco, e desligar a variante não desligaria nada — a interface mentiria de
novo, o oposto do que a mudança quer.

### D6 — O caminho por loja é texto no aplicativo

**Escolha.** A orientação de primeira execução é uma string no aplicativo,
com o caminho de cada loja, e some assim que a pasta for encontrada.

**Por quê.** O usuário está **dentro do aplicativo** quando precisa do
caminho; mandá-lo ao README é um salto. E a lista é curta e estável: não
justifica varredura de disco nem leitura de registro.

### D7 — O README encolhe, o material vai para `docs/`

**Escolha.** O README fica com: o que é, como usar, o que isso faz com o
jogo, como desfazer, créditos. O material de desenvolvimento — derivação de
caminho, escrita cirúrgica, o modelo `GameOption`, o porquê de cada valor,
as medições — vai para `docs/`, que já tem `FINDINGS.md` e `ROADMAP.md`.

**Por quê.** O README hoje tem 23 KB, metade duplicada em PT/EN, e a
duplicação é justificável para a **interface**, que é bilíngue de verdade,
não para o texto de arquitetura, que descreve a mesma coisa duas vezes.

**Alternativa descartada: manter o README longo e linkar as seções.** A
página de um mod no Nexus Mods é lida por quem quer **usar**; o resto é
procurado por quem quer **contribuir**, e vai buscá-lo no repositório.

### D8 — A ação Simular sai da interface; o plano continua no motor

**Escolha.** O botão e o handler de simulação saem. `ProfileApplier.Plan()`
e `AppState.Plan()` permanecem, porque `Apply()` se apoia neles e porque a
tela principal precisa da contagem de alterações **antes** de o usuário
aplicar.

**Por quê.** A simulação respondia a uma pergunta de auditoria — "o que
exatamente mudaria?" — que a interface nova responde de outro jeito: a
contagem de alterações na tela principal e, na revelação, valor em vigor ao
lado do valor desejado. O botão ocupava uma área de resultado inteira para
repetir o que o grid já mostra.

**Alternativa descartada: manter a simulação atrás da revelação.** A
informação já está lá, em contexto melhor, com o estado real do jogo ao
lado. Um botão a mais na tela principal seria ruído — que é exatamente o que
a change vem removendo.

**O que NÃO é removido.** `ProfileApplier.Plan()` continua sendo a base do
`Apply()`: tirar a *operação* não é tirar o *cálculo*. O caso de teste 5.5
não desaparece — ele passa a verificar o que continua valendo, que o plano
não escreve nada.

### D9 — Base Material construída sobre o WinForms, sem biblioteca de terceiros

**Escolha.** O Material entra como **camada de tokens** no projeto de UI:
papéis de cor, escala tipográfica, espaçamento de 8 px, raio de canto,
elevação e camada de estado. E como um punhado de controles tematizados —
botão (cheio, tonal, contornado, texto), caixa de seleção, cartão,
cabeçalho, cartão de escolha para a lista de variantes, área de resultado —
sobre os controles padrão do Windows, desenhados à mão onde o padrão não
alcança.

**Por quê.** O que o Material traz aqui é uma **linguagem visual**, não uma
biblioteca de botões. O projeto não tem dependência externa nenhuma, e a
publicação self-contained já pesa ~103 MB; trocar isso por um framework de UI
deixaria o aplicativo maior, com uma superfície de manutenção que não existe
hoje. E a alternativa mais óbvia não está disponível: **MaterialSkin2 não tem
versão para .NET 10** e está sem manutenção desde 2022. O **Krypton Toolkit**
tem `net10.0-windows` e licença BSD-3, mas não é Material — é um toolkit
Fluent/Spark, que seria adotado para em seguida ser re-tematizado, e a troca
obrigaria a reconstruir os cinco grids, que é justamente o que se quer
preservar.

**Contrapartida aceita.** Sem biblioteca, a ondulação (*ripple*) e a sombra
de elevação de verdade não vêm de graça: são desenho próprio. Fica
registrado como pergunta em aberto se a base Material precisa de ondulação e
sombra de elevação reais, ou se os tokens de elevação e a camada de estado
de hover e foco bastam.

### D10 — Paleta Cyberpunk de quatro cores, em variante clara e escura

**Escolha.** As quatro cores são a **origem de toda a cor da interface**:

| papel | variante escura | variante clara |
|---|---|---|
| fundo | `000d25` | `f7f7ff` |
| texto | `f7f7ff` | `000d25` |
| ação principal | `6564db` | `6564db` |
| destaque | `85ff9e` | tom derivado de `85ff9e` |

O `85ff9e` é legível sobre `000d25` e ilegível sobre `f7f7ff`; no modo
claro ele é escurecido até alcançar contraste suficiente. As superfícies
intermediárias (cartão, elevação 1, elevação 2) derivam do `000d25`
elevado. Nenhuma cor de fora da paleta entra na interface.

**O triângulo.** As três cores cromáticas formam um triângulo: o destaque é
a rotação de −120° da principal, e o papel de erro é a rotação de +120°. Isso
não é enfeite — é o que faz a regra "nenhuma cor fora da paleta" verificável.
A cor de erro não é escolhida: ela **deriva** da principal, e o mesmo caminho
gera o destaque. Verificação: `docs/ARQUITETURA.md` §6 e os testes de tema.

**O teto do par principal.** Com o preenchimento da ação principal em
`6564db`, nenhum par das quatro cores-base alcança 4.5:1 com o rótulo: o mais
alto é `f7f7ff` sobre `6564db`, que dá 4.48:1. O preenchimento mantém a cor
exata da paleta — é isso que a spec exige — e 4.48:1 é o teto que ela permite.
Onde a principal aparece **como texto**, um tom derivado é usado, e esse
alcança 4.5:1. O texto suave também é derivado por contraste, porque a
mistura direta com o fundo não o alcançava.

**Por quê.** A escolha da variante continua sendo a do sistema: o requisito
existente — *o tema segue o sistema operacional* — **continua verdadeiro sem
modificação**. O que muda é a origem da cor: em vez de o Windows decidir,
decide o aplicativo, e o sistema decide apenas o modo. A moldura nativa da
janela continua acompanhando o sistema (`Application.SetColorMode`), para
que as caixas de diálogo do Windows não destoem.

**Alternativa descartada: uma paleta só, escura, fixa.** É o que "tema
Cyberpunk" sugere à primeira vista, mas quebraria o requisito vigente do
tema e deixaria a janela clara num monitor claro.

## Risks / Trade-offs

- **[Um resumo mal escrito é pior do que nenhum]** → o resumo é escrito junto
  com a calibração da variante, e uma variante sem resumo é apresentada sem
  resumo — o que a spec já exige.
- **[A revelação esconde informação relevante]** → a tela principal sempre
  informa o **número** de alterações e o **número** de divergências; quem
  precisa do detalhe abre.
- **[A remoção de chaves de cvar pode apagar o que o usuário ajustou]** → a
  remoção só ocorre quando o valor atual é exatamente o da variante; caso
  contrário, a chave fica e a divergência é reportada.
- **[O formato do perfil muda]** → os campos novos são aditivos e a spec
  exige que um perfil gravado no formato anterior continue sendo lido.
- **[A calibração depende de uma medição que não existe]** → por isso
  nenhuma variante nova entra no perfil curado nesta change. Se a medição
  vier antes, a change seguinte acrescenta variantes sem tocar no aplicativo.
- **[Reorganizar a interface de um aplicativo em alpha]** → é o momento
  certo: ainda não há usuários dependendo do formato antigo da tela.
- **[Tirar a simulação tira a pré-visualização]** → a informação não se
  perde: a tela principal mostra quantas configurações mudam, e a revelação
  mostra valor em vigor e valor desejado lado a lado. Perde-se o botão, não
  o dado.
- **[A base Material feita à mão custa mais do que uma biblioteca]** → o
  escopo é o que a interface usa: tokens, um punhado de controles e o grid.
  Sem dependência nova, sem mudança na cadeia de suprimento, e o
  `DataGridView` dos cinco grids continua funcionando — o que uma troca de
  biblioteca quebraria.
- **[A paleta tem só quatro cores]** → as superfícies e o tom do destaque no
  modo claro são derivados das quatro cores-base, e a derivação fica escrita
  no arquivo de tema, não espalhada pelos controles.

## Open Questions

- **Quantas variantes, e com que conteúdo.** Depende do A/B do ROADMAP §2.2.
  Resposta parcial nesta change: o mecanismo e o perfil base atual.
- **Se "visual alto" é uma variante de verdade.** Exigiria saber quais cvars
  são **invisíveis** — o que também é medição, não dedução.
- **Se o estado lido do disco vira um número** ("37 de 37 em vigor") ou
  permanece um rótulo. O número é mais informativo e é trivial de calcular;
  o rótulo é mais leve.
- **Se a lista de variantes é ordenada por custo visual** ou por
  recomendação. A primeira é mais informativa; a segunda é mais segura para
  quem não sabe o que escolher.
- **Se a base Material precisa de ondulação (*ripple*) e sombra de elevação
  de verdade**, ou se os tokens de elevação e a camada de estado de hover e
  foco bastam. Isso decide se a camada de tema precisa de desenho próprio
  por controle ou se basta estilo e cantos.
