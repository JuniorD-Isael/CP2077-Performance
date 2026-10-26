# Proposal

## Why

O aplicativo despeja a mesma informação para toda pessoa que o abre: **536 células em 19 colunas e 134 linhas** (abas Perfil, Motor e Menu), das quais **cerca de 43% são ruído** para quem vai jogar — a coluna de procedência de cada valor, a seção e a chave de 60 cvars internas do motor, e uma coluna "desejado" que, no perfil base, mostra exatamente o mesmo que a coluna "valor", em 37 de 37 linhas.

O problema não é o layout: é a ausência de uma **tela que responda "funcionou?"**. Não há estado, não há confirmação, não há saída visível, e as cinco abas são todas de auditoria. A fileira de botões — simular, aplicar, restaurar, descartar — oferece **operações**, não **decisões**, e uma delas (simular) serve a quem quer auditar a mudança, não a quem quer jogar. Pior, o material de desenvolvimento — o hardware em que o perfil foi testado, a procedência de cada valor, a intenção de quem escreveu o perfil — está **dentro da interface**, onde pertence à documentação.

E há uma assimetria: **o aplicativo serve o usuário final; o repositório serve o contribuidor.** Hoje estão trocados. O README tem 23 KB (metade duplicada em PT/EN), e a interface tem 22 das 59 strings de texto dedicadas a procedência e identificadores técnicos.

Por fim, a interface **não tem identidade**: são controles padrão do Windows empilhados em tabelas, e a única decisão de cor é acompanhar o tema do sistema.

## What Changes

- O formato da variante no perfil passa a cobrir **as duas camadas**: uma variante nomeada pode sobrescrever tanto as opções de menu quanto as chaves de cvar do motor. Hoje `variants` só alcança a camada de menu, e é por isso que a escolha do usuário não teria efeito sobre onde está o ganho.
- Cada variante passa a declarar **rótulo, resumo em linguagem corrente e destaques** do que altera, para que o aplicativo explique a escolha sem expor identificadores técnicos.
- A **tela principal** deixa de ser um painel de opções e passa a oferecer **escolhas**: a lista de variantes com rótulo e resumo, o estado da aplicação, e as ações de **aplicar** e **reverter**. As tabelas de opções saem da tela principal, e a operação de descartar edições do perfil migra para a revelação do detalhe técnico.
- A ação **Simular** sai da interface. O cálculo do plano continua no motor, porque é dele que sai a contagem de alterações que a tela principal mostra antes de o usuário aplicar.
- As tabelas, a coluna de procedência e a identificação de seção e chave passam a ficar **atrás de uma revelação explícita**, intactas, para quem quiser o detalhe.
- O efeito da escolha passa a ser explicado em **linguagem corrente**, com a contagem de configurações alteradas.
- A interface passa a ter uma **base Material** — elevação, cantos arredondados, escala tipográfica e camada de estado — com uma **paleta Cyberpunk** de quatro cores: `f7f7ff`, `6564db`, `85ff9e` e `000d25`, em variante clara e em variante escura.
- A **primeira execução** passa a orientar para a pasta do jogo, com instruções por loja, em vez de mostrar grades vazias.
- A mensagem de recusa por jogo em execução passa a dizer **como recuperar** quando o processo não encerra.
- A área de resultado passa a distinguir visualmente **sucesso, falha parcial e erro**.
- O **README** é enxuto: o que é, como usar, o que isso faz com o jogo, como desfazer, créditos. O material de desenvolvimento vai para `docs/`.

**O que quebra:** a ação Simular deixa de existir, e a interface muda de forma. São duas quebras visíveis, em um aplicativo ainda em alpha, que é o momento certo para elas.

**O que não quebra:** o formato do perfil. Os campos novos da variante são aditivos, e um perfil de usuário gravado no formato anterior — variante sem resumo e sem chaves de cvar — **continua sendo lido**.

## Capabilities

### New Capabilities
<!-- Nenhuma: a change reformula capacidades existentes. -->

### Modified Capabilities
- `profile-application`: a variante deixa de ser um item específico de HUD e passa a ser um conjunto nomeado, aplicável às duas camadas, com resumo em linguagem corrente; a simulação deixa de ser uma operação exposta ao usuário.
- `desktop-interface`: a tela principal passa a oferecer escolhas e estado, o detalhe técnico passa a ficar atrás de uma revelação explícita, a operação de simulação sai, e a interface ganha uma base Material com paleta Cyberpunk.
- `game-installation`: a recusa por jogo em execução passa a incluir o caminho de recuperação.

## Impact

**Formato do perfil** (`profile/lowspec-30fps.json`, `profile/profile.schema.json`): `variants.<id>` ganha `userIni`, `summary` e `highlights`. O schema aceita os campos novos; perfis antigos continuam válidos.

**Aplicativo** (`app/CP2077Performance.Core`, `app/CP2077Performance.Ui`): o modelo de variante e o resolvedor passam a operar sobre as duas camadas; `MainForm` é reestruturado em tela principal + revelação; a ação de simulação e suas strings saem; entra uma camada de tema Material com a paleta Cyberpunk. **Sem pacote novo**: o projeto de UI hoje não tem nenhuma dependência externa, e a base Material é construída sobre os controles do próprio WinForms.

**Documentação** (`README.md`, `docs/`): o README encolhe e o material de desenvolvimento migra.

**Compatibilidade:** o perfil de usuário em `%LOCALAPPDATA%\CP2077-Performance\profile.json` continua sendo lido, inclusive com variantes no formato anterior.

**Risco assumido e registrado:** o **número de variantes e o conteúdo de cada uma** dependem de uma medição que ainda não existe — o A/B entre o perfil completo e o perfil sem os quatro cvars novos, que está no `docs/ROADMAP.md` (§2.2). Esta change entrega o **mecanismo**; a **calibração** das variantes é trabalho de dados posterior, e o perfil curado continua sendo um único conjunto até que existam medições que justifiquem variantes adicionais. Nenhuma variante nova entra no perfil curado nesta change.

**Riscos:** a reestruturação da interface é uma mudança visível em um aplicativo ainda em alpha, o que é o momento certo para fazê-la. A separação entre "resumo da variante" e "lista técnica" depende de o resumo ser escrito com cuidado — um resumo ruim é pior que nenhum, porque o usuário perde a noção do que a variante faz. Tirar a simulação tira a pré-visualização de "antes para depois" fora da tela principal; a compensação é a contagem de alterações e a revelação, que mostra valor em vigor e valor desejado lado a lado.
