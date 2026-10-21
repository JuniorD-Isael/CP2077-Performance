# Tasks

## 1. Estrutura e build

- [x] 1.1 Criar a solução em `app/` com três projetos: `CP2077Performance.Core` (biblioteca, sem referência a WinForms), `CP2077Performance.Ui` (WinForms) e `CP2077Performance.App` (executável), todos em `net10.0-windows`. Verificação: `dotnet build app/` compila sem erro e o projeto Core não referencia assemblies de interface.
- [x] 1.2 Escrever `build.cmd` com dois alvos: `self-contained` (padrão, `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true` para `dist/`) e `framework-dependent` (para `dist/framework-dependent/`). Verificação: rodar `build.cmd` gera o executável em `dist/` e o executável inicia mostrando a janela.
- [x] 1.3 Embutir `profile/lowspec-30fps.json` como recurso do projeto App e extraí-lo para `%LOCALAPPDATA%\CP2077-Performance\profile.json` na primeira execução, sem sobrescrever um perfil de usuário já existente. Verificação: apagar o perfil do usuário, executar o aplicativo, confirmar que o perfil extraído é idêntico ao do repositório; executar de novo e confirmar que o perfil do usuário não foi sobrescrito.
- [x] 1.4 Configurar o `.gitignore` para ignorar `dist/`, `%LOCALAPPDATA%` não se aplica ao repositório, e o `backups/` que hoje está versionado sai do repositório. Verificação: `git status` não lista `dist/` nem `backups/`.

## 2. Localização e validação da instalação

- [x] 2.1 Implementar no Core o resolvedor de alvos: a partir da pasta do jogo, derivar o caminho de `engine\config\platform\pc\user.ini` e de `bin\x64\plugins\cyber_engine_tweaks\config.json`; a partir de `Environment.GetFolderPath(LocalApplicationData)`, derivar `CD Projekt Red\Cyberpunk 2077\UserSettings.json`. Verificação: teste unitário confirma os três caminhos para uma raiz de jogo fictícia, e confirma que o caminho do save não depende do caminho da instalação.
- [x] 2.2 Implementar a validação da pasta de instalação: exigir `bin\x64\Cyberpunk2077.exe` e reportar a camada de patches como indisponível quando o diretório do CET não existir, sem falhar. Verificação: teste unitário com raiz válida e com raiz sem o diretório do CET.
- [x] 2.3 Implementar a guarda de jogo em execução: detectar o processo do jogo, recusar a operação e explicar que o jogo sobrescreve as opções de menu ao encerrar; repetir a checagem imediatamente antes de escrever. Verificação: teste unitário com o processo presente e ausente; teste de integração com o jogo aberto confirma a recusa.
- [x] 2.4 Adicionar a detecção de painel in-game remanescente na pasta de mods do CET, com a informação de que ele deixou de aplicar nada e a opção de removê-lo. Verificação: teste unitário com e sem o diretório remanescente; a remoção apaga o diretório e o log confirma.
- [x] 2.5 Documentar em `docs/FINDINGS.md` as regras de derivação de caminho e o fato de o diretório de save ser independente da loja. Verificação: a seção nova existe e é referenciada pelo README.

## 3. Leitura do schema e validação de valores

- [x] 3.1 Implementar no Core o leitor do schema do jogo (`r6\config\settings\options.json` e `r6\config\settings\platform\pc\options.json`), expondo por opção o tipo, a lista de valores aceitos e o valor padrão. Verificação: teste unitário confirma que uma opção de lista devolve a lista de aceitos e o `default_value` de uma opção booleana.
- [x] 3.2 Implementar a validação das opções de menu do perfil contra o schema, reportando opção e valor aceito quando um valor for inválido, e opção inexistente quando o nome não constar do schema. Verificação: teste unitário para valor fora da lista, para opção inexistente e para o caso conforme.
- [x] 3.3 Implementar a extração de tokens ASCII imprimíveis de `bin\x64\Cyberpunk2077.exe` e a validação das chaves de cvar contra esses tokens. Verificação: teste unitário confirma que uma chave presente é aceita e uma chave inventada é recusada.
- [x] 3.4 Implementar a lista de exceções de chaves ausentes conhecidas, referenciando `docs/FINDINGS.md` §10.4, e reportar cada exceção como aviso em vez de erro. Verificação: teste unitário confirma que a chave `GlassUseMipChain` produz aviso e segue, e que uma chave fora da lista produz erro.
- [x] 3.5 Documentar em `docs/FINDINGS.md` o que a validação por token prova e o que ela não prova, e o porquê de as chaves serem strings separadas no executável. Verificação: a seção nova existe e cita a limitação.

## 4. Escrita das três camadas

- [x] 4.1 Implementar o localizador de campos do arquivo de opções do jogo: varrer o texto rastreando a estrutura de grupos e opções, e devolver o trecho exato do campo de valor e do campo de índice de cada opção, sem alterar o texto. Verificação: teste unitário localiza corretamente os trechos de uma opção conhecida em um exemplar real do arquivo, e confirma que o texto fora dos trechos é idêntico.
- [x] 4.2 Implementar a escrita cirúrgica: substituir apenas os trechos de valor e de índice das opções visadas, deixando o restante do arquivo byte a byte igual. Verificação: teste unitário compara o arquivo antes e depois e confirma que apenas os trechos visados mudaram; teste de integração aplica um perfil e confirma que o jogo lê os valores.
- [x] 4.3 Implementar a reconciliação de valor e índice para opções de lista, incluindo o caso em que o valor já está correto e apenas o índice está desatualizado. Verificação: teste unitário para os dois casos.
- [x] 4.4 Implementar a manipulação de texto do arquivo de cvars do motor: inserir ou substituir a chave dentro da seção, criar a seção ao final quando faltar, e preservar comentários, ordem de seções e ordem de chaves. Verificação: teste unitário sobre um exemplar real do arquivo confirma que os comentários do motor sobreviveram e que a ordem das seções existentes não mudou.
- [x] 4.5 Implementar a escrita da camada de patches do Cyber Engine Tweaks, tratando a ausência do arquivo como camada indisponível e não como falha. Verificação: teste unitário confirma a escrita e o caso de ausência.
- [x] 4.6 Implementar a verificação pós-escrita: reler os arquivos escritos e comparar com o esperado, reportando divergência como falha da operação. Verificação: teste unitário injeta uma divergência e confirma que a operação é reportada como falha.
- [x] 4.7 Documentar em `docs/FINDINGS.md` a técnica de escrita cirúrgica e por que ela substitui o round-trip de serialização. Verificação: a seção nova existe.

## 5. Perfil, variantes e simulação

- [x] 5.1 Atualizar `profile/profile.schema.json` para aceitar a seção `variants`, com a mesma forma de `userSettings.values` dentro da variante, e validar o perfil curado contra o schema atualizado. Verificação: o perfil valida contra o schema; um perfil com a seção `variants` inválida é recusado.
- [x] 5.2 Implementar no Core o modelo de perfil e variante, e a leitura do perfil curado embarcado e do perfil do usuário. Verificação: teste unitário carrega um perfil de exemplo com variante e confirma a estrutura lida.
- [x] 5.3 Implementar a aplicação de uma variante ligada: as opções da variante são aplicadas por cima do perfil base. Verificação: teste unitário confirma a precedência da variante sobre o perfil base.
- [x] 5.4 Implementar a restauração de uma variante desligada: as opções afetadas voltam ao `default_value` do schema do jogo. Verificação: teste unitário confirma que a opção volta ao padrão do schema e que as demais opções do perfil base permanecem aplicadas.
- [x] 5.5 Implementar a simulação: produzir a lista de alterações sem escrever em nenhum arquivo e sem gerar backup. Verificação: teste unitário confirma que a simulação produz a lista e que nenhum arquivo do diretório de teste foi alterado.
- [x] 5.6 Implementar o registro de alterações: para cada aplicação, registrar os arquivos alterados e a origem de cada valor (perfil base, variante ou padrão do schema). Verificação: teste unitário confirma o conteúdo do registro.
- [x] 5.7 Adicionar a variante de HUD ao perfil curado, com a lista de toggles de HUD e os presets reduzidos do próprio jogo, e bump da versão do perfil. Verificação: o perfil valida contra o schema e a variante aparece na simulação.

## 6. Backup e restauração

- [x] 6.1 Implementar a criação de backup antes de toda aplicação que altera ao menos um arquivo, copiando o estado anterior dos arquivos que a operação toca, e não criando backup quando nada muda. Verificação: teste unitário confirma a criação no caso que altera e a ausência no caso que não altera.
- [x] 6.2 Implementar o registro do conjunto de arquivos por backup, para que a restauração devolva o estado anterior do conjunto. Verificação: teste unitário confirma que o backup de uma operação de dois arquivos contém os dois.
- [x] 6.3 Implementar a retenção das três versões mais recentes, apagando a mais antiga a cada criação nova, de modo que nunca existam mais de três. Verificação: teste unitário cria cinco backups e confirma que restam três, sendo o mais antigo o quarto criado.
- [x] 6.4 Implementar a restauração a partir de uma versão mantida, com aviso e recusa quando um arquivo registrado no backup não exista mais no disco. Verificação: teste unitário confirma a restauração e o caso de arquivo ausente.
- [x] 6.5 Implementar a localização dos backups no diretório de dados local do usuário, em pasta dedicada à ferramenta, sem gravar dentro da instalação do jogo. Verificação: teste unitário confirma o caminho e que nada foi criado na pasta do jogo.

## 7. Interface

- [x] 7.1 Implementar a detecção do tema do sistema pela preferência de aplicativo e a aplicação via `Application.SetColorMode`, além de escala por monitor. Verificação: executar com o tema do sistema em escuro e em claro e conferir a inversão das cores; a captura de tela da janela confirma.
- [x] 7.2 Implementar a internacionalização com recurso neutro em inglês e recurso satellite `pt-BR`, escolhidos por `CurrentUICulture`. Verificação: executar com o idioma do sistema em português do Brasil e confirmar a interface em português; forçar um locale desconhecido e confirmar que a interface fica em inglês, sem chaves vazias.
- [x] 7.3 Implementar a janela principal: campo da pasta do jogo com validação, indicação de disponibilidade de cada camada e a origem de cada alvo. Verificação: apontar uma instalação real e conferir os caminhos exibidos na janela.
- [x] 7.4 Implementar a aba de cvars do motor, exibindo seção e descrição a partir do comentário do próprio jogo, marcando as opções que serão acrescentadas e as que não têm descrição. Verificação: apontar a instalação real e conferir que as descrições vêm do `user.ini` do jogo.
- [x] 7.5 Implementar a aba de opções de menu, exibindo o valor desejado pelo perfil ao lado do valor em vigor, e a caixa de seleção da variante de HUD. Verificação: apontar a instalação real e conferir que a divergência entre desejado e em vigor aparece.
- [x] 7.6 Implementar as ações de aplicar, simular, restaurar backup e descartar edições, com as ações que alteram o jogo identificadas e as operações indisponíveis desabilitadas com o motivo apresentado. Verificação: acionar cada ação e conferir o efeito; com o jogo aberto, a ação de aplicar aparece desabilitada com o motivo.
- [x] 7.7 Implementar a exibição do registro de alterações e a informação de que a validação por token não prova que o motor leia a chave. Verificação: aplicar um perfil e conferir o registro exibido.

## 8. Remoção dos artefatos antigos e documentação do produto

- [x] 8.1 Remover `installer/Install-Profile.ps1` e `mod/PerformanceProfile/init.lua` do repositório, e remover a pasta do painel in-game da instalação do jogo usada no desenvolvimento. Verificação: `git status` mostra as remoções; o aplicativo detecta que o painel se foi e não reporta nada a respeito dele.
- [x] 8.2 Reescrever o README: instalação pelo aplicativo, remoção do capítulo do painel in-game e do instalador de linha de comando, derivados do caminho do jogo, e as opções do aplicativo. Verificação: seguir o README do zero em uma máquina limpa e o fluxo funciona.
- [x] 8.3 Registrar no CHANGELOG a entrada da versão, com as remoções marcadas como quebra e a origem delas. Verificação: a entrada lista o que foi removido e o motivo.
- [x] 8.4 Registrar em `docs/FINDINGS.md` a conclusão de que o sandbox do CET impede o painel in-game de aplicar, e que essa foi a razão da remoção. Verificação: a seção existe e é referenciada pelo README.

## 9. Verificação de integração

- [x] 9.1 Publicar o aplicativo e executá-lo contra a instalação real, aplicando o perfil com o jogo fechado, e confirmar que as três camadas foram escritas, que o `UserSettings.json` mudou só nos campos visados e que o `user.ini` preservou os comentários do motor. Verificação: comparar os arquivos antes e depois e conferir o resultado esperado em cada camada.
- [x] 9.2 Subir o jogo e conferir que os valores aplicados estão em vigor, comparando com o estado efetivo lido pelo próprio aplicativo. Verificação: os valores lidos pelo aplicativo conferem com o que foi aplicado, salvo as opções que o motor recalcula.
- [x] 9.3 Executar o aplicativo em tema escuro e idioma português do Brasil, capturando a tela da janela, e conferir a apresentação final. Verificação: a captura mostra a interface em português e com as cores do tema escuro.
