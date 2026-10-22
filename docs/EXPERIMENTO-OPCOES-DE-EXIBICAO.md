# Procedimento de descoberta do contrato das opções de exibição

Este é o procedimento do **grupo 1** da change `fullscreen-at-native-resolution`.
Ele responde a três perguntas que o schema do jogo não responde, porque
`WindowMode`, `Resolution` e `ResolutionScaling` são declaradas com
`"is_dynamic": true` e **sem lista de valores**: o motor monta essa lista em
runtime, a partir do monitor.

## O que já se sabe

| fato | origem |
|---|---|
| As opções existem no `UserSettings.json` e são alcançadas pelo escritor | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` |
| O schema não declara os valores aceitos delas | `r6/config/settings/platform/pc/options.json`, `"is_dynamic": true` |
| O motor grava **valor e índice** para elas | `WindowMode = BorderlessWindowed` (índice 1), `Resolution = 1366x768` (índice 2) |
| O índice **não** é o da enumeração do Windows | `EnumDisplaySettings` põe `1366x768` nos índices 18–20 do monitor de 1920×1080 |
| O motor **reescreve** o arquivo ao subir, normalizando o estado em memória | `docs/FINDINGS.md` §13.4 |

## Hipótese a testar

A lista de resoluções do motor é a lista de **resoluções distintas do monitor
em ordem decrescente de área**. Na máquina de referência:

```
1600x900 (0)  1440x900 (1)  1366x768 (2)  1280x720 (3)  1128x634 (4)  ...
```

O par que o jogo gravou (`1366x768` com `index=2`) **confirma a posição**, mas
**um ponto não prova uma lista**: a ordem por área e a ordem por largura
coincidem aqui, e divergem em outros monitores.

## Pergunta 1 — o motor lê o `value` ou o `index`?

Este é o experimento decisivo, e **não precisa de interação com o jogo**.

1. Feche o jogo.
2. No `UserSettings.json`, escreva `Resolution` com o **valor** trocado e o
   **índice** mantido — por exemplo, `value = "1600x900"` com `index = 2`
   (índice que aponta para `1366x768`).
3. Abra o jogo e espere o estado em memória ser normalizado (§13.4: o jogo
   escreve o arquivo pouco depois de subir).
4. Leia o `UserSettings.json` de novo:

| o arquivo tem | conclusão |
|---|---|
| `value = "1600x900"` e o índice corrigido | o motor resolve pelo **valor** |
| `value = "1366x768"` | o motor resolve pelo **índice** |
| o par como estava, sem reescrita | o motor não normalizou; inconclusive |

5. Restaure o arquivo a partir do backup.

> Esta é exatamente a questão que o `docs/ROADMAP.md` §2.1 registra para as
> demais opções, aplicada às de exibição. E o `docs/FINDINGS.md` §13.3 já
> mostrou que, para `HDDMode` e `HDDMode2`, **nenhum dos dois campos importa**
> — o motor recalcula. Essa é a razão de o experimento existir.

## Pergunta 2 — qual é a ordem da lista?

1. Feche o jogo.
2. Abra o jogo, entre em **Configurações → Vídeo → Resolução**, e escolha uma
   resolução diferente da atual.
3. Saia do jogo e leia o par `(valor, índice)` no `UserSettings.json`.
4. Repita com uma terceira resolução.

Três pares bastam para decidir a regra: **por área** decrescente ou **por
largura** decrescente. Anote a tabela `(resolução, índice)` no
`docs/FINDINGS.md`.

## Pergunta 3 — o que o motor oferece, e o monitor em que o jogo roda

O enumerador do Windows e a lista do motor **não são a mesma coisa**: o
Windows expõe cada modo com várias frequências, e o motor oferece
resoluções. Confirme, na lista de Question 2:

- se o motor oferece resoluções que o monitor não suporta (ele não deveria);
- qual é o valor aceito para `WindowMode` em **tela cheia** — no arquivo do
  usuário, `BorderlessWindowed` tem índice 1, e os valores aceitos **não**
  estão no schema.

Este último é o que o aplicativo precisa escrever: `WindowMode` recebe um
valor **e** um índice, e o índice da tela cheia precisa ser observado.

## O que fazer com as respostas

| resposta | consequência no aplicativo |
|---|---|
| o motor resolve pelo **valor** | o aplicativo escreve o valor; o índice acompanha o valor |
| o motor resolve pelo **índice** | o aplicativo **precisa** do índice, e a regra da pergunta 2 vira código |
| o motor recalcula a resolução | a aplicação é inútil, e o caminho de queda assume |

O aplicativo **se auto-valida**: antes de escrever o índice, ele compara o
índice que calculou para a resolução **atual** com o índice que o **jogo
gravou**. Se não baterem, ele escreve **só o valor** e informa que a
configuração entra em vigor no próximo início do jogo. É por isso que a
regra errada não quebra nada: ela degrada para o caminho seguro.
