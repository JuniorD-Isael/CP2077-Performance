# Roteiro

O que já está resolvido, e o que ainda não está. Cada item diz **o que se sabe**,
**o que falta** e **como resolver**.

---

## Resolvido

| tema | estado |
|---|---|
| A camada de menu do perfil é honesta | 37 de 37 opções sobreviveram a uma sessão de jogo |
| A camada de cvars é aceita pelo motor | 56 das 57 chaves confirmadas como tokens do executável; a 57ª é uma exceção documentada |
| A escrita é cirúrgica | `UserSettings.json`: 2768 → 2768 linhas, 26 alteradas, todas `value`/`index`. `user.ini`: comentários do motor intactos |
| O painel in-game pode aplicar | **não** — o sandbox do CET confina o `io` ao diretório do mod. Motivo em `FINDINGS.md` §13.1 e §14.4 |
| Onde está o gargalo | GPU a 95% de mediana, CPU folgada. É custo de fragmento, não de CPU |
| A variante de HUD é segura | opt-in, e desligada devolve as opções ao `default_value` do esquema |

---

## Ponto 2 — pendências conhecidas

### 2.1 `value` vs `index`: qual campo o motor lê?

**O que se sabe.** O arquivo de opções do jogo guarda **dois** campos para
opções de lista: `value` (o valor) e `index` (a posição na lista `values`).
O instalador antigo escrevia só `value`, e os dois podiam divergir — o caso
concreto era `HDDMode` com `value="On"` e `index=2`, que aponta para "Auto".
O aplicativo agora escreve os dois, coerentes.

**O que falta.** Não se sabe qual dos dois o motor consulta em runtime, nem se
consulta os dois. As observações não decidem: `HDDMode` e `HDDMode2` foram
sobrescritos pelo motor mesmo com os dois campos coerentes, o que sugere
recomputação, não precedência de campo.

**Como resolver.** Um experimento controlado, com o jogo fechado:
escrever `value="Off"` com `index=0` e `value="On"` com `index=1` para a
mesma opção, em execuções distintas, e ver qual valor o jogo mostra no
menu. Ou: com o jogo **em execução**, alterar apenas `index` e observar
se o valor exibido muda.

### 2.2 A/B dos 4 cvars novos

**O que se sabe.** `DPL`, `ExposureScaleBuffer`, `ExtendedDuringSSR` e
`HairClears` foram adicionados ao perfil em `Rendering/AsyncCompute`,
descobertos pelo hook `HookGameOptionInit` do CET — o motor os registra
como `GameOption`, logo o `user.ini` os controla. Estão aplicados e
confirmados no `user.ini`.

**O que falta.** O efeito individual. Nenhuma medição isola esses quatro
do resto do perfil, e o perfil inteiro ainda não foi medido contra um
perfil vazio.

**Como resolver.** Duas sessões jogadas na **mesma rota**, com o mesmo
cena e o mesmo estilo de jogo, alternando só o que se quer medir:
perfil completo → perfil sem os 4 cvars.

> **Pré-requisito: um medidor de FPS.** O caminho externo natural seria a
> temporização de composição do DWM, mas
> `DwmGetCompositionTimingInfo` **não funciona** para a janela do jogo
> nesta máquina (erro ≠ 0, com ou sem `hwnd`). As saídas possíveis:
> (a) o usuário lê o contador do Game Bar (Win+G) nos pontos de
> checkpoint; (b) um mod de instrumentação **temporário** dentro do jogo,
> instalado só com aval e removido depois — foi o que mediu a mediana de 24
> FPS em gameplay; (c) `PresentMon`, que exige elevação de privilégio.

**O que falta.** O efeito individual. Nenhuma medição isola esses quatro
do resto do perfil, e o perfil inteiro ainda não foi medido contra um
perfil vazio.

**Como resolver.** Duas sessões jogadas na **mesma rota**, com o mesmo
cena e o mesmo estilo de jogo, alternando só o que se quer medir:
perfil completo → perfil sem os 4 cvars. A instrumentação externa já
existe (GPU, CPU, VRAM, I/O a ~1 s).

### 2.3 O mapa de xrefs: quais cvars o motor realmente lê

**O que se sabe.** A validação por token prova que o **nome** existe no
executável, não que o motor o leia como cvar. `EnableAirResistance` aparece
na região de curvas de física de veículo, o que sugere que pode ser
propriedade de curva em vez de cvar. São 4 casos ambíguos.

**O que falta.** O mapa entre a função C++ que registra a opção e a string.

**Como resolver.** A análise com Ghidra que nunca rodou. O script já
existe: `C:\MCOC\ghidra-scripts\ReaExtractSettings.java` mapeia strings de
cvar → função, com xrefs e as classes RTTI de Settings/Option/Feature. O
`ReaPreRun.java` desliga 18 analisadores caros antes da análise, o que
resolve o gargalo de 43 minutos na fase `Stack`. O que falta é a Infra
estrutura: o projeto Ghidra persistente ficou com **0 bytes**, e a última
execução morreu na fase de análise, antes de gravar o banco.

### 2.4 O crash de saída do jogo

**O que se sabe.** Três ocorrências de `EXCEPTION_ACCESS_VIOLATION` na
saída do jogo, todas com `Engine/RequestExit: true` e
`IsLoadingSavedSession: true`, em `03_night_city`. As duas primeiras são
de **20/10, antes de qualquer código deste projeto**, o que descarta o
painel in-game como causa. Consequência prática: **o log do CET e o do
RED4ext são bufferizados e se perdem no crash** — o crash é a única
testemunha que sobrevive, e é onde o `ReportQueue` do REDengine guarda o
estado do engine no momento da falha.

**O que falta.** A causa. E o efeito colateral: sem o log nativo, a
análise de qualquer sessão que termina em crash fica sem o registro do
engine.

**Como resolver.** O `dmp` do crash está em
`%LOCALAPPDATA%\REDEngine\ReportQueue\Cyberpunk2077-<timestamp>\`. Abrir no
WinDbg ou no Ghidra e buscar a origem do salto inválido.

### 2.5 O encerramento natural

**O que se sabe.** O teste de integração usou encerramento forçado, então o
*flush de saída* do jogo não rodou. Já se sabe, de sessões anteriores, que
ele **recalcula** `HDDMode`, `HDDMode2` e `EnableTelemetry` — o que
significa que esses três valores do perfil são, na prática, inertes.

**O que falta.** A confirmação com encerramento natural e a lista
definitiva de opções que o motor recalcula.

**Como resolver.** Uma sessão com saída natural, comparando o
`UserSettings.json` antes e depois.

---

## Ordem sugerida

1. **2.5** — é barato e fecha a lista de opções inertes.
2. **2.1** — um experimento de duas execuções, com o jogo fechado.
3. **2.2** — precisa de duas sessões jogadas; o custo maior, e o único que
   pode render mais FPS do que a perda de qualidade visual.
4. **2.3** — o mais caro (infraestrutura de análise), e o que mais
   strengthenia a base de evidências do projeto.
5. **2.4** — change própria, por ser um problema de estabilidade do jogo
   e não de performance.
