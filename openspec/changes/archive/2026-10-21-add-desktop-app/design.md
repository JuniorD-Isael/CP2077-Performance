# Design

## Context

Ver `proposal.md` para a motivação e `specs/` para os requisitos. O que condiciona a abordagem:

**O que já se sabe do jogo, medido em runtime** (`docs/FINDINGS.md` §§10–13):

- As três camadas de configuração do CP2077 são `user.ini` (cvars do motor), `UserSettings.json` (opções de menu) e `config.json` do Cyber Engine Tweaks (patches). O diretório de save é derivado do sistema e **não depende da loja**, então uma única build serve a Steam, Epic e GOG.
- O executável guarda a **seção e a chave separadas** como tokens, e o motor compõe o path `Grupo/Feature/Nome` em runtime. Consequência: para validar uma chave de cvar, procura-se o **token isolado** do nome da chave — 56 das 57 chaves do perfil confirmam.
- O motor registra a opção com **dois** campos, `value` e `index`, que podem divergir. O perfil tem de escrever os dois de forma coerente.
- O jogo **valida no load** e **descarrega a memória no exit**. Por isso valor externo só sobrevive se aplicado com o jogo fechado — e por isso o aplicativo precisa recusar a operação com o jogo rodando.
- O `options.json` do jogo é o esquema autoritativo: traz `type`, `values` (lista de aceitos) e `default_value` das 323 opções.

**Restrição do produto:** o CLI em PowerShell e o painel in-game saem do projeto. O código-fonte do aplicativo precisa existir no repositório, com script de build, e o artefato distribuído não pode exigir instalação de runtime.

## Goals / Non-Goals

**Goals:**

- Um núcleo de manipulação de configuração **sem dependência de interface**, testável sem interface gráfica.
- Escrita que altere **o mínimo possível** dos arquivos do jogador: o `UserSettings.json` é reescrito só nos campos visados, e o `user.ini` preserva comentários e ordem.
- Nenhuma dependência de instalação para o usuário final.
- O aplicativo ser a **única** superfície do projeto: sem comando separado, sem mod no jogo.

**Non-Goals:**

- Não medir FPS dentro do jogo. O `PerfProbe` foi um instrumento de investigação e sai do produto; medição de desempenho fica fora do aplicativo.
- Não vararrer o disco em busca do jogo, e não ter lógica por loja.
- Não reimplementar a engenharia reversa: o aplicativo **consome** o que a investigação já estabeleceu (schema do jogo, tokens do executável, lista de cvars do perfil).
- Não resolver a investigação do crash de saída do jogo — é pré-existente e não é do projeto. O aplicativo apenas passa a copiar o relatório de crash, porque nele sobrevive o que se perde no crash.

## Decisions

### D1 — .NET 10 com WinForms, em vez de .NET Framework 4.8

**Escolha:** `net10.0-windows`, publicado *self-contained* (artefato único, sem runtime instalado).

**Por quê:** o requisito de seguir o **tema do sistema** exige suporte nativo a dark mode. O .NET 10 WinForms tem `Application.SetColorMode`, e a preferência do sistema se lê em `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`. O .NET Framework 4.8 não tem dark mode nativo: seria preciso montar paleta própria e forçar a barra de título via DWM.

**Alternativa considerada:** .NET Framework 4.8 com `csc.exe` — um `.exe` de ~50 KB contra ~150 MB do *self-contained*. Descartada porque obriga a implementar o tema à mão, que é a exigência explícita do usuário.

**Trade-off aceito:** artefato maior. O `build.cmd` publica também *framework-dependent* (~300 KB) para quem já tiver o runtime.

### D2 — Núcleo sem WinForms, e um exe por vez

**Escolha:** `app/Core/` (biblioteca, sem referência a WinForms) + `app/Ui/` (WinForms) + `app/App/` (executável). O `Core` não conhece a interface.

**Por quê:** o aplicativo passa a ser a peça central do projeto. Com o núcleo isolado, toda a lógica de escrita, validação, backup e variante é verificável sem abrir janela — o que o projeto não tinha.

**Alternativa considerada:** um único projeto com WinForms. Descartada por acoplar a lógica verificável à interface.

### D3 — Leitura com `System.Text.Json`, escrita cirúrgica com varredura própria

**Escolha:** `System.Text.Json` (no framework, sem dependência extra) para **ler** o `options.json` do jogo e o `profile.json`. Para **escrever** o `UserSettings.json`, um localizador de campos que opera sobre o texto bruto.

**Por quê:** o requisito é que o restante do arquivo fique byte a byte igual. Um round-trip de serialização reescreve o arquivo inteiro, reformata e pode alterar a forma de números. O localizador varre o texto rastreando a estrutura `data[].options[]`, encontra o trecho de `"name"` de cada opção e, para as visadas, substitui apenas o trecho de `"value"` e de `"index"`. O que não for visado não é tocado.

**Alternativa considerada:** round-trip com `JsonNode`. É o que o instalador em PowerShell fazia (`ConvertFrom-Json` → `ConvertTo-Json`) e funcionou nesta máquina, mas reescreve o arquivo inteiro e produz um diff ilegível — o que contraria a promessa de auditabilidade do projeto.

**Mitigação de risco:** após escrever, o aplicativo relê o arquivo e compara o resultado com o esperado. Se divergir, reporta falha em vez de dar a operação por concluída.

### D4 — Validação de cvar por varredura de tokens do executável

**Escolha:** extrair os tokens ASCII imprimíveis de `bin\x64\Cyberpunk2077.exe` e conferir a presença de cada nome de chave; a seção é conferida à parte.

**Por quê:** a §13.1 do `FINDINGS.md` registra que 56 dos 57 nomes distintos de chave do perfil existem como token isolado no executável, e que `EnableAirResistane` — grafia herdada da `FeatureList` do ConfigOverhaul — não existe em lugar nenhum do jogo. A ausência de um token é sinal forte de erro de digitação, porque o motor não consegue casar um nome que não está no binário.

**Limite explícito:** presença no binário prova que o *nome* existe, não que o motor o leia como cvar. A prova definitiva exigiria o mapa de xrefs, que é outra change. Por isso a validação énecessária mas não suficiente, e o aplicativo diz isso.

**Alternativa considerada:** manter a lista de chaves validada a mão. Descartada — é exatamente o que já falhou uma vez.

### D5 — Variante como seção do perfil, com restauração pelo `default_value`

**Escolha:** o perfil ganha `variants`, e o aplicativo expõe uma caixa de seleção por variante. Ao desligar, as opções afetadas voltam ao `default_value` do esquema do jogo.

**Por quê:** o esquema do jogo é a fonte autoritativa do que "padrão" significa, e tê-lo à mão torna a restauração determinística — não depende de lembrar qual era o valor anterior. Uma variante como arquivo de perfil alternativo, por outro lado, obriga a manter dois arquivos coerentes e não tem como "voltar ao padrão" sem saber o que era.

### D6 — Perfis: curado embarcado + perfil do usuário

**Escolha:** o `profile/lowspec-30fps.json` do repositório é embarcado no executável como recurso e extraído no primeiro uso; o perfil editado pelo usuário vive em `%LOCALAPPDATA%\CP2077-Performance\profile.json`.

**Por quê:** o repositório segue sendo a fonte única de verdade do perfil curado, e o usuário tem um perfil próprio sem que a edição afete o original. O `build.cmd` injeta o JSON do repositório no recurso — a fonte não se duplica.

### D7 — Backups em `%LOCALAPPDATA%`, com anel de três

**Escolha:** `%LOCALAPPDATA%\CP2077-Performance\backups\<timestamp>\`, e o backup é criado só quando a aplicação altera algo.

**Por quê:** o diretório de dados local é gravável por definição, ao contrário de `Program Files` e do diretório do jogo — que não deve ser poluído. E o backup só quando há mudança evita que o anel de três se esgote em cópias idênticas.

**Alternativa considerada:** backup dentro da pasta do repositório, como o instalador em PowerShell fazia. Descartada: o repositório é da máquina de desenvolvimento, não do usuário final.

### D8 — Idioma por recursos satellite, neutro em inglês

**Escolha:** o assembly principal carrega o **inglês** como recurso neutro e `pt-BR` vem como satellite. O aplicativo escolhe em tempo de execução conforme `CultureInfo.CurrentUICulture`.

**Por quê:** neutro em inglês garante que um locale desconhecido receba inglês, e não uma tela com chaves vazias. O recurso satellite é o mecanismo padrão do .NET, sem código de tradução caseiro.

### D9 — Remoção do painel in-game com detecção de remanescente

**Escolha:** o painel sai do repositório, mas o aplicativo detecta o diretório do painel na instalação do jogo e oferece removê-lo, explicando que ele deixou de aplicar nada.

**Por quê:** quem já tinha o painel instalado continua com ele carregando a cada boot, sem efeito e sem saber. Como o painel **nunca** aplicou nada, não há estado a migrar — só entulho a limpar.

### D10 — O `user.ini` é escrito por manipulação de texto, não por parser de INI

**Escolha:** o `user.ini` é editado por manipulação de texto: inserir ou substituir a linha da chave dentro da seção, criando a seção ao final se faltar.

**Por quê:** o arquivo do motor carrega comentários que documentam efeitos colaterais (`;Don't disable. (breaks lightmaps)`) e a ordem das seções e chaves é informação. Um parser de INI de uso geral não preserva nada disso por padrão. É a mesma técnica do instalador em PowerShell, que preservou a estrutura ao longo de todas as aplicações.

## Risks / Trade-offs

- **[O aplicativo passa a ser a peça central]** → o núcleo fica separado da interface e verificável sem interface; o build publica testes do núcleo.
- **[A varredura de tokens do executável custa tempo e memória]** → em C# a extração de ~344 mil tokens leva bem menos que os ~6 s que a versão em PowerShell levava; o resultado é cacheado em memória durante a sessão.
- **[A escrita cirúrgica é mais delicada que um round-trip]** → releitura e comparação após a escrita; e o diff do arquivo resultante é pequeno e auditável, que é o objetivo.
- **[A lista de exceções de chaves ausentes é embutida no código]** → ela está referenciada na documentação (`docs/FINDINGS.md` §10.4) e é pequena e estável; chaves novas ausentes entram por atualização do perfil, não do código.
- **[O artefato self-contained tem ~150 MB]** → o `build.cmd` também publica a variante *framework-dependent*; o repositório guarda o fonte, não o binário.
- **[O `profile.schema.json` passa a rejeitar perfis antigos]** → é uma quebra assumida e anotada na proposta; a versão do esquema acompanha a do perfil.
- **[A variante de HUD inclui os presets nativos do jogo]** → assumption registrada: a lista exata é decisão de produto, e a implementação a lê do perfil, de modo que ajustá-la não exige mexer no código.

## Open Questions

- **A lista exata de toggles de HUD da variante.** A proposta inclui os ~22 elementos visuais de HUD mais os três presets reduzidos que o próprio jogo oferece (`FluffReduction`, `PostProcessingReduction`, `Sieja`). A lista vive no perfil, então ajustá-la é mudança de dado, não de código — pode ser decidida durante a implementação, ou depois, sem alterar especificações, abordagem ou tarefas.
- **O modo padrão do `build.cmd`.** A proposta é publicar *self-contained* por padrão e oferecer a variante *framework-dependent* por flag. É escolha de script de build, e não afeta o comportamento do aplicativo.
- **O mapa de xrefs que provaria quais cvars o motor efetivamente lê.** Depende da análise com Ghidra, que é outra change. Enquanto ela não existir, a validação por token continua sendo necessária e insuficiente — e o aplicativo declara isso ao usuário.
