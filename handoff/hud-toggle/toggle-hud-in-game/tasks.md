# Tasks

## 1. A prova de viabilidade

- [ ] 1.1 Montar o mod de sondagem: um `init.lua` mínimo, no diretório de mods do CET, que muda o valor de uma opção de HUD e registra o que fez no log. Verificação: o mod carrega sem erro e o log mostra a mudança.
- [ ] 1.2 Executar a sessão de sondagem: abrir o jogo, entrar em gameplay, acionar a mudança pelo mod e capturar a tela antes e depois. Verificação: as duas capturas existem e estão no `docs/FINDINGS.md`.
- [ ] 1.3 Responder, com a evidência da captura: o HUD relê a sua opção durante a sessão? Verificação: a resposta está escrita em `docs/FINDINGS.md`, e o design é atualizado com ela.
- [ ] 1.4 Responder: o sandbox expõe algum hook de entrada, e a tecla global chega ao jogo com o foco dele? Verificação: a resposta está escrita, e o design registra se D1 encolhe.
- [ ] 1.5 Decidir a forma final: aplicação ao vivo no mod, ou caminho de queda com o aplicativo aplicando fora do jogo. Verificação: a decisão está escrita no `design.md`, e as tasks do grupo 5 são ajustadas a ela.

## 2. A fronteira entre o aplicativo e o jogo

- [ ] 2.1 Derivar o diretório do mod e o caminho do arquivo de intenção da pasta do jogo, seguindo a regra de derivação já documentada. Verificação: teste unitário confere o caminho, e o diretório criado fica sob `cyber_engine_tweaks\mods`.
- [ ] 2.2 Implementar o arquivo de intenção: estado do HUD e as opções de menu que o definem, com data de modificação. Verificação: teste unitário confirma o formato gravado e relido.
- [ ] 2.3 Implementar a leitura da intenção pelo lado do mod, detectando a mudança pela data de modificação e não por comparação de conteúdo a cada quadro. Verificação: teste do formato do arquivo, e o log do mod mostra uma leitura por alteração.

## 3. A tecla global

- [ ] 3.1 Registrar a tecla global com `RegisterHotKey`, com a tecla configurável e F8 como padrão. Verificação: teste unitário do registro, e a tecla padrão é a documentada.
- [ ] 3.2 Tratar a falha de registro por tecla ocupada: informar o usuário e manter o recurso alternável por outro meio. Verificação: teste unitário com o registro recusado confirma a mensagem e a alternativa.
- [ ] 3.3 Manter o estado do HUD no aplicativo e invertê-lo ao acionar a tecla, com o jogo em execução ou não. Verificação: teste unitário do estado, e a tela principal reflete a mudança.

## 4. O mod

- [ ] 4.1 Escrever o `init.lua` do mod: leitura da intenção, aplicação da mudança no motor, e nada fora do próprio diretório. Verificação: o mod não referencia variável de ambiente nem caminho absoluto fora do diretório, e o log mostra o que ele fez.
- [ ] 4.2 Fazer o mod registrar a aplicação da mudança, e tratar o estado já aplicado sem alterar nada. Verificação: o log distingue "aplicado" de "já estava".
- [ ] 4.3 Instalar e atualizar os arquivos do mod pelo aplicativo, informando o usuário o que foi feito. Verificação: teste unitário da escrita dos arquivos, e o aplicativo informa o caminho e a finalidade.

## 5. A aplicação da intenção

- [ ] 5.1 Implementar a aplicação fora do jogo: escrever as opções do HUD no `UserSettings.json` quando o jogo não está em execução. Verificação: teste unitário confere a escrita, e a reaplicação de uma opção que o motor recalcula.
- [ ] 5.2 Acompanhar o processo do jogo e reaplicar a intenção depois que ele encerrar, para que o estado alternado não seja apagado pela descarga do motor. Verificação: teste unitário do acompanhamento, e o arquivo final tem o estado esperado.
- [ ] 5.3 Se a prova do grupo 1 Reprovar a aplicação ao vivo, remover do design o caminho do mod e manter só a aplicação fora do jogo, com a tecla e o estado. Verificação: o design e as tasks refletem a decisão do grupo 1.

## 6. A interface

- [ ] 6.1 Apresentar o estado do HUD e a tecla que o alterna na tela principal, inclusive com o jogo em execução. Verificação: captura de tela mostra o estado e a tecla.
- [ ] 6.2 Implementar a presença na bandeja, com o estado do HUD visível e o caminho para abrir a janela. Verificação: captura de tela da bandeja, e o aplicativo continua alternando com a tecla.
- [ ] 6.3 Tratar a tecla ocupada na tela principal: mostrar o aviso no lugar do atalho, mantendo a alternativa. Verificação: captura de tela com a tecla ocupada.

## 7. Verificação

- [ ] 7.1 Publicar o aplicativo, instalar o mod e alternar o HUD com a tecla durante uma sessão de jogo, com o jogo fechado em tela cheia. Verificação: captura de tela do HUD completo e do HUD mínimo na mesma sessão.
- [ ] 7.2 Desligar o HUD e conferir que a mudança sobrevive ao encerramento do jogo. Verificação: o `UserSettings.json` depois de sair do jogo tem o estado esperado, e a reaplicação funcionou.
- [ ] 7.3 Rodar a suíte de verificações e a validação do perfil contra o schema. Verificação: todas as verificações passam e o perfil continua válido.
- [ ] 7.4 Registrar na documentação o procedimento de instalação do mod, o contrato da intenção e o resultado da prova. Verificação: `README.md`, `docs/ARQUITETURA.md` e `docs/FINDINGS.md` contêm o procedimento e o resultado.
