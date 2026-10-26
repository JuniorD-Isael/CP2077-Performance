# Tasks

## 1. Formato da variante: as duas camadas

- [x] 1.1 Estender `profile/profile.schema.json` para aceitar, dentro de `variants.<id>`, os campos `userIni` (seção → chave → valor), `summary` e `highlights`, mantendo `label` e `description`. Verificação: o perfil curado continua validando contra o schema, e um perfil de teste com os campos novos também valida.
- [x] 1.2 Estender `ProfileVariant` e `ProfileModel.Variants` no Core para ler `userIni`, `summary` e `highlights`, com `userIni` e `summary` opcionais. Verificação: teste unitário carrega um perfil de exemplo com uma variante completa e outra apenas com `userSettings`, e confirma que as duas são lidas.
- [x] 1.3 Estender `VariantResolver.Resolve` para produzir, além dos valores de menu, as chaves de cvar por variante, preservando a precedência da variante sobre o perfil base. Verificação: teste unitário confirma que a chave de cvar da variante substitui a do perfil base e que a variante sem `userIni` não altera a camada de motor.
- [x] 1.4 Implementar a remoção das chaves de cvar quando a variante é desligada, **somente** quando o valor em vigor for exatamente o que a variante escreveria; caso contrário, manter a chave e reportar a divergência. Verificação: teste unitário cobre os três casos: variante desligada remove a chave, valor divergente preserva a chave, e o valor padrão do motor (ausência da chave) é restaurado.
- [x] 1.5 Fazer o aplicador escrever as chaves de cvar da variante na seção correta do `user.ini`, preservando comentários e ordem, e validar essas chaves contra o executável como as do perfil base. Verificação: teste unitário confirma a escrita na seção correta e que uma chave de cvar inexistente na variante é recusada na validação.

## 2. A tela principal

- [x] 2.1 Montar a tela principal com o caminho do jogo, a lista de variantes com rótulo e resumo, e as ações de **aplicar** e **reverter**, sem nenhuma tabela de opções visível. Verificação: captura de tela da janela mostra a lista de variantes e nenhuma grade.
- [x] 2.2 Implementar a leitura do estado do disco: o perfil está aplicado, quando foi, e quantas opções divergem do valor em vigor, lendo os arquivos do jogo. Verificação: teste unitário com os arquivos de um jogo fictício, nos casos aplicado, nunca aplicado e divergente.
- [x] 2.3 Implementar a explicação do efeito em linguagem corrente, usando o resumo e os destaques das variantes selecionadas, com a contagem total de alterações. Verificação: teste unitário confirma que a contagem corresponde ao plano e que variantes sem resumo não quebram a explicação.
- [x] 2.4 Implementar a ação de reverter na tela principal, reaproveitando o `BackupService` existente. Verificação: teste unitário confirma que a reversão devolve o arquivo ao estado do backup e que a tela reflete o novo estado.
- [x] 2.5 Esvaziar a lista de variantes quando o perfil em uso não declarar nenhuma. Verificação: captura de tela com um perfil sem variantes mostra a lista vazia com a explicação de por quê, e não uma lista quebrada.
- [x] 2.6 Remover a ação **Simular**: o botão, o handler `OnSimulate`, o texto de habilitação e desabilitação, e as strings `Simulate` e `Simulated` nos dois idiomas. O cálculo do plano (`ProfileApplier.Plan`) **permanece**, porque `Apply` depende dele e a contagem de alterações vem dele. Verificação: nenhuma string `Simulate` ou `Simulated` permanece nos dois `.resx`, a interface não oferece simulação, e o caso de teste 5.5 passa a verificar que o plano não escreve nada.

## 3. Revelação do detalhe técnico

- [x] 3.1 Mover as 5 abas existentes para uma região expansível, acionada por uma ação explícita na tela principal, preservando as colunas de procedência e a identificação de seção e chave. Mover para essa região a operação de descartar edições do perfil, que sai da fila de ações da tela principal. Verificação: captura de tela antes e depois da revelação, e teste confirmando que as colunas continuam presentes e que a fila de ações da tela principal tem apenas Aplicar e Restaurar.
- [x] 3.2 Remover da tela principal a linha de alvo do hardware, as linhas de origem das camadas e a nota sobre a limitação da validação por token, mantendo-as na revelação. Verificação: a tela principal não contém nenhuma dessas strings, e a revelação contém.

## 4. Resultado e orientação

- [x] 4.1 Apresentar o resultado das operações com rótulo próprio para sucesso, falha parcial e erro, com a razão da falha quando houver. Verificação: teste unitário cobre os três desfechos, e captura de tela mostra os rótulos distintos.
- [x] 4.2 Acrescentar à mensagem de recusa por jogo em execução a orientação para encerrar o processo pelo Gerenciador de Tarefas. Verificação: teste unitário confirma que a mensagem menciona o Gerenciador de Tarefas quando o processo persiste.
- [x] 4.3 Implementar a orientação de primeira execução, com o caminho da pasta por loja, exibida antes das abas enquanto nenhuma pasta estiver informada, e dispensada assim que houver uma válida. Verificação: captura de tela da primeira execução mostra a orientação, e a captura seguinte, com pasta válida, não a mostra.

## 5. Base Material e paleta Cyberpunk

- [x] 5.1 Criar a camada de tokens do tema no projeto de UI: papéis de cor derivados de `f7f7ff`, `6564db`, `85ff9e` e `000d25`, escala tipográfica, espaçamento de 8 px, raio de canto, elevação e camada de estado de hover, foco e pressionado. Verificação: teste unitário confirma que cada papel resolve a cor da paleta na variante clara e na escura.
- [x] 5.2 Implementar a variante clara e a escura da paleta, escolhidas pela preferência de aplicativo do sistema, mantendo `Application.SetColorMode` para a moldura nativa da janela e para as caixas de diálogo do Windows. No modo claro, escurecer o destaque até que ele fique legível sobre `f7f7ff`. Verificação: teste unitário confirma que `ThemeHelper.SystemPrefersDark()` seleciona a variante, e captura de tela nas duas.
- [x] 5.3 Temabilizar os controles em uso: botão (cheio, tonal, contornado e texto), caixa de seleção, cartão, cabeçalho, cartão de escolha da lista de variantes e área de resultado. Verificação: captura de tela mostra os controles com cantos arredondados, elevação e camada de estado, e nenhuma cor fora da paleta.
- [x] 5.4 Temabilizar o grid das cinco abas: cabeçalho, linhas alternadas, sem linhas de grade e cores da paleta, preservando as colunas e a ordenação atuais. Verificação: captura de tela da revelação e teste confirmando que as colunas continuam presentes.
- [x] 5.5 Aplicar o tema à tela principal e à revelação, sem introduzir dependência externa no projeto de UI. Verificação: o `.csproj` de UI continua sem `PackageReference`, e o tema está aplicado nas duas regiões.

## 6. Perfil curado

- [x] 6.1 Adicionar ao perfil curado o `summary` e os `highlights` da variante de HUD existente, escritos em linguagem corrente, descrevendo o efeito sobre a interface do jogo. Verificação: a tela principal mostra o resumo e os destaques da variante.
- [x] 6.2 Manter o perfil curado com **uma única** variante nesta change, e registrar no `docs/ROADMAP.md` que as demais dependem do A/B pendente. Verificação: o perfil curado declara uma variante, e o ROADMAP cita a dependência da medição.

## 7. Documentação

- [x] 7.1 Enxugar o README: o que é, como usar, o que isso faz com o jogo, como desfazer, e créditos, nas duas línguas. Verificação: o README passa de 23 KB para menos de 10 KB, e mantém as seções de uso.
- [x] 7.2 Mover para `docs/` o material de desenvolvimento hoje no README: derivação de caminho, escrita cirúrgica, o modelo `GameOption`, as medições e a proveniência dos valores. Verificação: o README não cita mais `GameOption` nem a derivação de caminho, e `docs/` contém o material.
- [x] 7.3 Registrar no CHANGELOG a mudança de forma da interface, com o que saiu da tela principal e o que foi para a revelação, a remoção da ação Simular e a nova base visual, e marcar as duas quebras. Verificação: a entrada lista as duas quebras e aponta a spec correspondente.

## 8. Verificação

- [x] 8.1 Publicar o aplicativo e conferir, em português do Brasil, a tela principal, a revelação e a orientação de primeira execução, **no tema escuro e no tema claro**. Verificação: as seis capturas de tela, com o que foi pedido em cada uma.
- [x] 8.2 Aplicar o perfil pela interface nova contra a instalação real e conferir que as três camadas foram escritas e que a escrita continua cirúrgica. Verificação: comparação byte a byte do `UserSettings.json` e do `user.ini` com o estado anterior, mais o diff de arquivos da sessão.
- [x] 8.3 Desligar a variante pela interface nova e conferir que as opções de menu e as chaves de cvar afetadas voltaram ao padrão do jogo. Verificação: o `user.ini` e o `UserSettings.json` ficam com os valores esperados e o registro da aplicação mostra a remoção.
- [x] 8.4 Conferir que a fila de operações da tela principal tem apenas **Aplicar** e **Restaurar**, e que o **Restaurar** devolve o jogo ao estado anterior. Verificação: captura de tela da fila de botões e o estado do `UserSettings.json` antes e depois de restaurar.
- [x] 8.5 Rodar a suíte de verificações e a validação do perfil contra o schema. Verificação: todas as verificações passam e o perfil valida contra `profile/profile.schema.json`.
