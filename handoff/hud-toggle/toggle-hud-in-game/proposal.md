# Proposal

## Why

A variante de HUD reduz a interface do jogo para aliviar a GPU. Ela é aplicada **uma vez, na inicialização**. Mas o jogador alterna entre dois mundos durante a sessão: precisa do HUD para ler um marcador de missão ou um valor de vida, e quer o HUD de fora quando o FPS aperta.

Hoje, voltar o HUD significa **sair do jogo, reaplicar o perfil e abrir de novo**. Isso não é um recurso; é uma inconveniência.

E o caminho óbvio — um painel dentro do jogo — **já foi tentado e provado não funcionar**. O `FINDINGS.md` §13.1 estabelece, com evidência de runtime, que o sandbox Lua do Cyber Engine Tweaks confina o `io` ao diretório do mod, que `os.getenv` não existe, e que `_G` é `nil`. O painel in-game nunca conseguiu aplicar nada, e foi removido do projeto por isso.

## What Changes

- O aplicativo passa a **capturar uma tecla global** — F8 — enquanto o jogo está em execução, e a **inverter o estado do HUD** entre mínimo e completo.
- O aplicativo passa a registrar a **intenção** num arquivo dentro do diretório do mod, onde o mod consegue ler.
- Um **mod pequeno** (Lua, CET) passa a ler essa intenção e **aplicar a mudança na memória do motor**, sem reiniciar o jogo.
- O estado do HUD passa a ser **apresentado no aplicativo**, que continua disponível na bandeja durante a sessão.
- O aplicativo passa a **instalar e atualizar os arquivos do mod** no diretório de mods do Cyber Engine Tweaks, derivando o caminho da pasta do jogo.

**O que não muda:** o perfil. A variante continua sendo a mesma, com as mesmas opções. O que muda é **quando** elas valem.

## Capabilities

### New Capabilities
- `in-game-toggles`: a tecla global, o arquivo de intenção, o mod que aplica a mudança ao vivo, e o caminho do mod derivado da pasta do jogo.

### Modified Capabilities
- `desktop-interface`: o estado do HUD e a tecla que o alterna passam a ser apresentados durante a sessão.

## Impact

**Aplicativo** (`app/CP2077Performance.Core`, `app/CP2077Performance.Ui`): captura de tecla global, presença na bandeja, o arquivo de intenção, e a instalação dos arquivos do mod.

**Mod** (`mod/CP2077Performance/`): um `init.lua` que lê a intenção e aplica a mudança no motor. **É a reintrodução de um mod**, que o projeto removeu na `0.1.1-alpha` — mas com um papel invertido: ele não aplica o perfil, ele **executa a ordem que o aplicativo dá**.

**Documentação** (`README.md`, `docs/FINDINGS.md`, `docs/ARQUITETURA.md`): o procedimento de instalação do mod, o contrato da intenção, e o resultado da prova de viabilidade.

**Risco assumido e registrado:** **o motor aplica a mudança de opção do HUD ao vivo?** Não se sabe. O header do mod antigo afirmava *"o mod NÃO altera o motor ao vivo: o REDengine lê as opções no startup"* — mas isso é a crença do mod, não uma prova. `FINDINGS.md` §13.3 provou que o motor **recalcula** três opções específicas ao encerrar, o que mostra que o motor escreve em opções em runtime; o que não se sabe é se o **HUD** relê a sua opção.

**O primeiro passo desta change é a prova.** Ela é barata — uma sessão de jogo com um mod de sondagem — e o resultado decide a forma final. **Caminho de queda:** se o motor não relê a opção do HUD, a intenção passa a ser aplicada pelo **aplicativo**, fora do jogo: ele escreve as opções do HUD no `UserSettings.json` e o jogo as assume no próximo início. O usuário continua alternando com F8, com um reinício no meio.

**Riscos:** reintroduzir um mod é uma reversão de uma decisão deliberada. O mod não escreve fora do próprio diretório — ele **lê** a intenção e **aplica em memória** —, então ele não repete o erro do painel. Mas a dependência do CET volta, e o aplicativo passa a precisar de uma tecla global, que é um privilégio de sessão única: se outro programa tiver o F8, o aplicativo avisa em vez de falhar em silêncio.
