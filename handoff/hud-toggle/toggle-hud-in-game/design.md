# Design

## Context

Ver `proposal.md` para o motivo. O que condiciona a abordagem:

**A fronteira do sandbox é a chave do desenho.** `FINDINGS.md` §13.1 traz o
inventário do ambiente Lua do CET, medido em runtime:

```
os.getenv                          -> nil
os = clock date difftime remove rename time
_G                                 -> nil
io.open caminho com .. (1 nivel)    -> nil
io.open ABSOLUTO (qualquer)        -> nil
io.open CWD relativo               -> grava no CWD (o diretório do mod)
```

O mod **não escreve fora do próprio diretório** e **não lê variáveis de
ambiente**. Mas **lê e escreve no próprio diretório**. Essa é a fronteira
que o desenho usa: o aplicativo escreve do lado de fora, o mod lê do lado de
dentro.

**A conclusão de arquitetura que o projeto já reached.** §13.1 fecha com:
*"o painel in-game não pode ser o aplicador. Ele pode (e deve) ser coletor
de intenção + medidor; a aplicação tem de acontecer fora do jogo."* O
desenho desta change é a mesma conclusão, com o sentido invertido: o
aplicativo é quem **escreve a intenção**, e o mod é quem a **executa dentro
do motor**.

**A questão em aberto.** O header do mod antigo afirmava *"O mod NÃO altera
o motor ao vivo: o REDengine lê as opções no startup"*. Isso é a crença do
mod, não uma prova. O que §13.3 prova é o contrário em parte: o motor
**recalcula** `HDDMode`, `HDDMode2` e `EnableTelemetry` — ou seja, o motor
**escreve em opções em runtime**. O que ninguém mediu é se o **HUD relê a
sua opção** durante a sessão.

**O que já funciona e não precisa ser refeito.** O escritor cirúrgico, o
`UserSettings.json` com 328 opções registradas, a variante `hudMinimal` com
suas 24 opções, a reconciliação de `value`/`index`, o backup com anel de
três, a recusa por jogo em execução.

## Goals / Non-Goals

**Goals:**

- O HUD alterna **durante a sessão**, sem sair do jogo.
- A tecla de alternância é capturada **fora** do jogo, onde não há sandbox.
- O mod **não escreve fora do próprio diretório** — ele não repete o erro do
  painel.
- Se a aplicação ao vivo não for possível, **o recurso ainda funciona**, com
  um reinício no meio.

**Non-Goals:**

- **Não fazer o mod aplicar o perfil.** O perfil é aplicado pelo
  aplicativo, fora do jogo. O mod não escreve em `UserSettings.json` nem em
  `user.ini`.
- **Não transformar a tecla em configuração avançada.** Uma tecla
  configurável, com um padrão. O resto é escolha do SO.
- **Não reintroduzir o painel in-game.** A aba de settings dentro do jogo foi
  removida e não volta.
- **Não prometer o resultado da prova antes de fazê-la.**

## Decisions

### D1 — A tecla é capturada pelo aplicativo, não pelo mod

**Escolha.** O aplicativo registra a tecla global (F8) e escreve a intenção
num arquivo no diretório do mod. O mod **não** captura tecla nenhuma.

**Por quê.** A tecla global é uma coisa trivial e confiável em um processo
Windows normal: `RegisterHotKey` resolve, e a falha é detectável
(`ERROR_HOTKEY_ALREADY_REGISTERED`). O sandbox do CET, ao contrário, tem um
inventário de API **desconhecido** — o projeto só conseguiu sondar nomes
conhecidos (§13.1), e `onKeyPress` não está no que foi medido.

**Alternativa descartada: o mod captura a tecla.** Se o sandbox expuser
`onKeyPress`, o mod poderia fazer tudo sozinho e o aplicativo nem precisaria
estar rodando. Isso é **melhor** — e é por isso que a prova (§13.1 do
desenho) verifica. Mas o desenho **não pode depender** de uma API que
ninguém mediu. A arquitetura escolhida funciona nos dois casos; se o mod
conseguir capturar a tecla, o aplicativo deixa de precisar e a change
encolhe.

**Contrapartida aceita.** O aplicativo precisa estar **rodando** para a tecla
funcionar. Isso traz a presença na bandeja, que é uma novidade.

### D2 — A intenção é um arquivo, e o mod não conhece o perfil

**Escolha.** O arquivo de intenção carrega o **estado** (HUD mínimo ou
completo) e as **opções de menu que o definem**. O mod lê esse arquivo e
aplica; ele não lê o perfil, não conhece variantes e não sabe de onde veio
aquilo.

**Por quê.** O mod vive dentro do sandbox e não tem acesso ao perfil — ele
está num diretório do jogo, e o perfil é um documento do repositório. Se o
mod precisasse entender o perfil, ele passaria a duplicar a lógica de
resolução de variantes, e as duas cópias divergiriam.

**Por que um arquivo e não um soquete.** O sandbox bloqueia `io.open` fora
do diretório, e não há nada de rede no inventário. Um arquivo no diretório
do mod é o **único** canal que a fronteira permite — e é o mesmo canal que
o §13.1 indica: *"o painel pode gravar a intenção no próprio diretório"*.

**Contrapartida aceita.** A latência é o intervalo de polling do mod. Com um
intervalo curto, é imperceptível.

### D3 — O mod lê a intenção por alteração, não por tempo

**Escolha.** O mod detecta a mudança pela **data de modificação** do
arquivo, e não por comparar o conteúdo a cada quadro.

**Por quê.** O sandbox expõe `os.time` e `os.clock`, e `os.date` — o
inventário de §13.1 mostra `os = clock date difftime remove rename time`.
Comparar o conteúdo exige reler e desserializar o JSON a cada quadro; a data
de modificação exige uma chamada de sistema barata e só relê quando mudou.

### D4 — A prova vem antes da forma final

**Escolha.** O primeiro trabalho é uma **sessão de sondagem**: um mod
mínimo que muda a opção de um elemento do HUD e verifica, por captura de
tela, se o HUD mudou.

**Por quê.** É a diferença entre um recurso e uma afirmação. O projeto já
pagou esse preço uma vez: o painel in-game foi construído sobre a crença de
que o sandbox permitia ler e escrever, e **nunca aplicou nada** — três
falhas registradas em log, todas com a mesma causa (§11). A prova de D1 é
barata: uma sessão de jogo, uma captura de tela, uma resposta.

**A prova precisa responder três perguntas:**

1. **O HUD relê a sua opção durante a sessão?** Se não, a aplicação ao vivo
   está descartada e o caminho de queda assume.
2. **A mudança de valor da `GameOption` é o bastante, ou o HUD só reage a
   um evento específico?** O hook `HookGameOptionInit(struct GameOption *)`
   entrega o ponteiro da opção no registro; a prova verifica se escrever no
   valor da struct chega ao HUD.
3. **O sandbox expõe algum hook de entrada?** Se sim, D1 encolhe e o
   aplicativo deixa de precisar estar rodando.

### D5 — O caminho de queda mantém a tecla e move a aplicação

**Escolha.** Se a aplicação ao vivo for impossível, a intenção passa a ser
aplicada **pelo aplicativo, fora do jogo**: ele escreve as opções do HUD no
`UserSettings.json`, e o jogo as assume no próximo início.

**Por quê.** O recurso continua sendo "alternar o HUD com F8". O que muda é
que há um reinício no meio. E a aplicação fora do jogo é a **única** forma
escrita que este projeto provou funcionar (§13.1: o painel nunca conseguiu
escrever; o instalador consegue).

**O detalhe que fecha o ciclo:** o jogo **reescreve** o `UserSettings.json`
ao encerrar, se o estado em memória divergir do arquivo (§13.4). Então, com o
jogo aberto, uma escrita externa seria apagada na saída. Por isso, no
caminho de queda, o aplicativo **reescreve a intenção depois que o jogo
encerra** — ele acompanha o processo e reaplica. Sem isso, o F8 alternaria
para sempre e o estado nunca valeria.

### D6 — O aplicativo instala o mod, e diz o que fez

**Escolha.** O aplicativo escreve os arquivos do mod no diretório de mods do
CET e **informa** o usuário.

**Por quê.** O diretório do mod é derivado da pasta do jogo, como os demais
alvos (§14.1). E o perfil **já assume o CET presente** — a terceira camada do
perfil é o `config.json` do CET. Escrever um mod ali é a mesma natureza do que
o aplicativo já faz.

**Alternativa descartada: o mod fica no repositório e o usuário o instala.** O
aplicativo é *self-contained*, sem instalador: pedir que o usuário copie
arquivos Lua manualmente seria a primeira vez que o produto exige uma ação
manual, e o erro de destino seria invisível.

### D7 — A tecla global é um privilégio de sessão única

**Escolha.** O aplicativo registra a tecla com `RegisterHotKey` e, se
falhar, **informa** que a tecla está ocupada e oferece outro meio de
alternar.

**Por quê.** Uma tecla global é exclusiva: outro programa com o F8 impede o
registro. Falhar em silêncio seria exatamente o modo de falha que o §13.1
criticou no painel — *"o botão Apply morria ali, sem log e sem erro no log
do CET"*.

## Risks / Trade-offs

- **[O motor não relê a opção do HUD]** → é a primeira pergunta da prova.
  O caminho de queda (D5) mantém o recurso, com um reinício.
- **[A aplicação ao vivo exigiria mais do que escrever o valor]** → a segunda
  pergunta da prova. Se o HUD só reage a um evento, o mod passa a emitir
  esse evento, ou o caminho de queda assume.
- **[O CET não estar instalado]** → o perfil já depende dele; a ausência é
  detectada na validação da instalação e reportada.
- **[A tecla global conflitar]** → o aplicativo informa e oferece o botão.
- **[O mod reintroduzir um componente que o projeto removeu]** → com um
  papel invertido e restrito: ele não escreve fora do próprio diretório e
  não aplica o perfil. O §13.1 é o teste, e ele é a razão de o desenho não
  pedir nada ao mod que o painel pedia.
- **[O jogo reescrever o arquivo e apagar a intenção]** → o D5 cobre: o
  aplicativo reaplica depois que o processo encerra.
- **[O aplicativo precisar ficar rodando]** → presente na bandeja, com o
  estado do HUD visível. É uma novidade de comportamento que o usuário
  precisa entender, e a tela principal diz.

## Open Questions

- **Qual elemento do HUD serve de sonda na prova?** O mais barato de
  observar é o `minimap`; o mais evidente é a barra de vida. A prova define.
- **O `HUD` do jogo relê a opção, ou o motor empurra o valor para a
  interface uma vez no startup?** Se for a segunda, a aplicação ao vivo está
  descartada e o caminho de queda assume sem rodeio.
- **A tecla global funciona com o jogo em tela cheia e com foco exclusivo?**
  Depende de como o jogo captura entrada. `RegisterHotKey` é global, mas o
  jogo pode consumir a tecla antes. **Vira uma pergunta da prova.**
- **O mod deve ser instalado junto com a primeira aplicação do perfil, ou
  sob demanda?** Instalar junto é mais simples; sob demanda evita um
  arquivo no diretório do jogo para quem não usa o recurso.
