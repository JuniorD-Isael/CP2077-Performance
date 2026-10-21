# Proposal

## Why

O projeto nasceu para tornar configurável a camada de performance do Cyberpunk 2077 que o jogo esconde, mas a interface escolhida provou ser incapaz de fazer isso. Em runtime, o sandbox Lua do Cyber Engine Tweaks confine o `io` ao diretório do mod: `os.getenv` não existe e caminho absoluto ou com `..` são bloqueados. Isso significa que o painel `MODS → Low-Spec 30 FPS` **nunca conseguiu escrever no `user.ini` nem no `UserSettings.json`** — três falhas registradas em log, e o botão de aplicar morria em silêncio. Pior: o painel exibia valores que o jogo não usava, ou seja, uma interface que confirmava mudanças inexistentes.

Ao mesmo tempo, a investigação em runtime mostrou que o caminho de configuração do jogo é mais simples do que se supunha e mais verificável do que se supunha: 34 das 37 opções de menu do perfil sobrevivem a uma sessão inteira, 13 cvars foram provados como lidos pelo motor (via o hook `HookGameOptionInit` do CET) e 56 das 57 chaves de cvar foram confirmadas como tokens existentes no executável. A camada de cvar do motor — justamente a de maior impacto e a única que o menu do jogo não expõe — nunca teve uma interface capaz de alcançá-la.

Substituir o painel e o script de instalação por um aplicativo desktop resolve as duas metades: uma interface de verdade para a camada de cvar, e um aplicador que roda fora do jogo, sem sandbox, com validação contra o schema do jogo e contra o próprio binário.

## What Changes

- **BREAKING** — Remove o painel in-game `mod/PerformanceProfile/` (a aba `MODS → Low-Spec 30 FPS` deixa de existir). A conclusão é medida, não preferencia: o ambiente Lua do CET não permite escrever fora do diretório do mod, e `docs/FINDINGS.md` §13.1 registra a prova.
- **BREAKING** — Remove o instalador de linha de comando `installer/Install-Profile.ps1` e suas flags (`-DryRun`, `-Restore`, `-SkipBinaryCheck`, `-NoMod`). A lógica dele não se perde: é portada para o núcleo do aplicativo, e vira botão.
- **BREAKING** — O formato do perfil ganha a seção `variants`, e `profile/profile.schema.json` passa a aceitá-la. Um perfil de usuário escrito contra o schema anterior deixa de validar.
- Adiciona o aplicativo desktop `CP2077-Performance` (WinForms, .NET 10), com fonte aberta em `app/` e `build.cmd` produzindo o artefato.
- O aplicativo passa a ser a única forma de aplicar o perfil. Os passos de instalação documentados no README passam a apontar para ele.
- O perfil passa a ser **editável dentro do aplicativo**; o JSON embarcado é o padrão curado do repositório e o perfil do usuário vive em `%LOCALAPPDATA%\CP2077-Performance\profile.json`.
- A variante de HUD deixa de ser um arquivo de perfil alternativo e vira uma **caixa de seleção** no aplicativo. Ao ser desligada, as opções afetadas voltam ao `default_value` do schema do jogo.
- Os backups passam a ser automáticos, versionados e limitados às **3 versões mais recentes**, guardados em `%LOCALAPPDATA%\CP2077-Performance\backups\`.
- O aplicativo segue o **idioma** (`pt-BR` → português, demais → inglês) e o **tema** (claro/escuro) do sistema operacional.
- Documentação e CHANGELOG atualizados; `docs/FINDINGS.md` e `docs/NATIVE-SETTINGS-API.md` permanecem como pesquisa de referência.

## Capabilities

### New Capabilities
- `game-installation`: como o aplicativo identifica e valida a instalação do jogo a partir do caminho informado pelo usuário, deriva os alvos de configuração de cada camada e recusa operar com o jogo em execução.
- `profile-application`: validação de um perfil contra o schema do jogo e contra o executável, escrita cirúrgica das três camadas, reconciliação de `value`/`index`, variantes (HUD) como opção liga/desliga, simulação e registro de alterações.
- `config-backup`: ciclo de vida do backup automático das configurações, com retenção das três versões mais recentes e restauração a partir de qualquer uma delas.
- `desktop-interface`: idioma e tema derivados do sistema, apresentação do perfil, do estado efetivo do jogo e do log de alterações.

### Modified Capabilities
<!-- Nenhuma: o projeto ainda não tem specs. -->

## Impact

**Arquivos removidos:** `installer/Install-Profile.ps1`, `mod/PerformanceProfile/init.lua`.

**Arquivos alterados:** `profile/profile.schema.json` (aceita `variants`), `profile/lowspec-30fps.json` (ganha a variante HUD), `README.md`, `CHANGELOG.md`, `.gitignore` (backups passam a viver fora do repositório).

**Arquivos novos:** `app/` (fonte C#), `build.cmd`, `dist/` (artefato, ignorado pelo git).

**Dependências:** .NET 10 com `Microsoft.WindowsDesktop.App` — presente nesta máquina. O `build.cmd` publica **self-contained**, de forma que o executável distribuído não dependa de runtime instalado.

**Sistemas tocados em runtime:** `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json`, `<jogo>\engine\config\platform\pc\user.ini`, `<jogo>\bin\x64\plugins\cyber_engine_tweaks\config.json`. O diretório do save é derivado do sistema e **não depende da loja** (Steam, Epic ou GOG), de modo que uma única build atende as três.

**Riscos:** o aplicativo passa a ser a peça central do projeto, e um defeito nele atinge todos os usuários — a mitigação é manter o núcleo (escrita, validação, backup) separado da interface e testável isoladamente. A escrita cirúrgica de JSON é mais delicada que um round-trip de serialização, e é mitigada por releitura e comparação após a escrita.
