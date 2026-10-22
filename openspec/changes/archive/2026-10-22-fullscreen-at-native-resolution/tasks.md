# Tasks

## 1. O contrato das opções de exibição (experimento)

- [x] 1.1 Montar o procedimento de descoberta: o jogo abre, o usuário escolhe três resoluções diferentes na interface do próprio jogo, e o `UserSettings.json` é lido depois de cada escolha para extrair o par `(valor, índice)`. Verificação: o procedimento está escrito, com o que ler e o que anotar.
- [x] 1.2 Executar o procedimento na máquina de referência e registrar a tabela `(valor, índice)` do motor para as três resoluções, mais o par do modo de janela. Verificação: a tabela tem pares de resolução e de modo de janela, com o valor e o índice anotados lado a lado, em `docs/FINDINGS.md` §15.
- [x] 1.3 Determinar, com a tabela em mãos, se o motor resolve a resolução pelo `value` ou pelo `index`: escrever um `value` com o `index` de outra resolução, abrir o jogo e ver qual resolução ele assume. Verificação: o resultado é anotado em `docs/FINDINGS.md` §15.4, e a conclusão é aplicada ao requisito da spec `display-mode` que depende dela.
- [x] 1.4 Determinar a ordem da lista de resoluções do motor: comparar a tabela obtida com a lista de modos do próprio monitor. Verificação: a ordem está escrita, e a regra que a reproduz — se houver uma — está escrita junto.

## 2. Os modos do display

- [x] 2.1 Implementar a enumeração dos monitores do sistema e da resolução nativa de cada um, com o principal marcado como padrão. Verificação: teste unitário sobre a máquina de referência confere a resolução nativa do monitor principal, e o teste roda em uma máquina sem monitor não derruba.
- [x] 2.2 Implementar a leitura dos modos de exibição de um monitor, com a resolução de saída derivada do modo de maior área. Verificação: teste unitário confere que a resolução de saída de um monitor é a sua maior resolução.
- [x] 2.3 Implementar a leitura do par `(valor, índice)` da resolução em uso a partir do `UserSettings.json` do jogo. Verificação: teste unitário com um arquivo de exemplo confere a extração do par, e a ausência da opção é tratada sem erro.

## 3. A escrita das opções de exibição

- [x] 3.1 Estender o `MenuOptionsWriter` para aceitar um `index` fornecido de fora, usado quando a opção não tem lista de valores no schema. Verificação: teste unitário confirma que a opção é escrita com o `value` e o `index` fornecidos, e que o resto do arquivo fica byte a byte igual.
- [x] 3.2 Implementar a escrita das opções de exibição: `WindowMode` e `Resolution`, com o valor e o índice do motor quando conhecidos, e apenas o valor quando não. Verificação: teste unitário cobre os dois caminhos, e a ausência da opção no arquivo do jogo não impede a aplicação das demais camadas.
- [x] 3.3 Validar a resolução contra os modos do monitor escolhido, recusando um valor que o monitor não suporta. Verificação: teste unitário confirma que a recusa acontece antes de escrever qualquer arquivo.
- [x] 3.4 Validar a lista de modos do motor contra o par que o jogo gravou, e recusar o `index` quando não houver correspondência. Verificação: teste unitário com um par divergente confirma que só o `value` é escrito.

## 4. A escolha na interface

- [x] 4.1 Adicionar a escolha de modo de exibição à tela principal, com o monitor de destino, a resolução de saída e o aviso de que a resolução interna não muda. Verificação: captura de tela mostra a escolha, e o texto do aviso está presente.
- [x] 4.2 Mostrar o monitor e a resolução de saída escolhidos, e a resolução que o jogo já usa quando a escolha é "manter como está". Verificação: captura de tela nos dois estados.

## 5. Verificação

- [x] 5.1 Publicar o aplicativo e aplicar contra a instalação real, com o jogo fechado, em tela cheia na resolução do monitor. Verificação: o `UserSettings.json` mostra o modo de janela em tela cheia e a resolução nativa, e a contagem de alterações da tela principal bate com o que foi escrito.
- [x] 5.2 Abrir o jogo e conferir que ele ocupa a tela toda, com a resolução interna variando como antes. Verificação: captura de tela do jogo em gameplay, e o `UserSettings.json` após o encerramento, para confirmar que o motor não descartou a configuração.
- [x] 5.3 Desligar a escolha de modo de exibição, aplicar e confirmar que a janela volta ao tamanho anterior. Verificação: captura de tela e comparação do `UserSettings.json`.
- [x] 5.4 Rodar a suíte de verificações e a validação do perfil contra o schema. Verificação: todas as verificações passam e o perfil continua válido.
- [x] 5.5 Registrar na documentação a receita de tela cheia, o contrato das opções dinâmicas e a tabela de modos do motor. Verificação: `docs/ARQUITETURA.md` e `docs/FINDINGS.md` contêm o procedimento e a tabela.
