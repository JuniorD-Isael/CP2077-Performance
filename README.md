# CP2077-Performance

**[Português (Brasil)](#pt-br)** · **[English](#en)**

Perfil de performance para **Cyberpunk 2077** em hardware de baixo desempenho.
Objetivo: **30 FPS jogáveis** em GPU integrada, com nada de mods removidos.

**Versão atual: `0.1.0`** — veja o [CHANGELOG](CHANGELOG.md).

Desenvolvido e validado em: **AMD Ryzen 5 5500U (6C/12T, 2,1 GHz) + AMD Radeon
Vega 8 integrada (VRAM compartilhada) + 20 GB RAM**, com o jogo na versão
`3.0.80.51928`.

O projeto **não remove nenhum mod** e **preserva o cabelo** (`Hair = True`),
porque o jogador usa um mod de terceira pessoa e valoriza o visual do cabelo.

---

# README em Português (PT-BR)

<a id="pt-br"></a>

## 1. Do que se trata

O Cyberpunk 2077 expõe três camadas de configuração independentes, e o jogo
lê todas elas no startup:

| Camada | Arquivo | O que controla |
|---|---|---|
| **Opções do menu** | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` | Qualidade gráfica, resolução dinâmica, limite de FPS, áudio, I/O, serviços |
| **cvars do motor** | `engine/config/platform/pc/user.ini` | Features do RED engine: oclusão, culling de streaming, animação, física, cabelo |
| **Patches do motor** | `bin/x64/plugins/cyber_engine_tweaks/config.json` | Anti-aliasing, async compute, vignette, vsync |

Um jogador que quer 30 FPS num Vega 8 enfrenta três obstáculos:

1. **O menu de settings esconde as opções que importam.** As features do RED
   engine (que controlam o custo real de GPU e CPU) **não aparecem na
   interface** — o motor as lê de um arquivo INI. A ferramenta mais conhecida
   para isso ([ConfigOverhaul](#creditos)) tem bugs que limitam o que ela
   alcança.
2. **O jogo reverte valores inválidos silenciosamente.** Cada opção do tipo
   lista tem um conjunto exato de valores aceitos, declarado no schema do
   jogo. Passar um valor fora dessa lista simplesmente não surte efeito —
   sem erro, sem aviso.
3. **A configuração precisa de três ferramentas diferentes** para cobrir as
   três camadas, e o resultado final não é auditável nem reversível.

Este projeto resolve os três:

- **Um perfil único em JSON** (`profile/lowspec-30fps.json`) descreve as três
  camadas. É auditável, ajustável sem mexer em código, e serve de fonte
  única de verdade.
- **Um instalador** (`installer/Install-Profile.ps1`) aplica o perfil com
  backup automático, opção de dry-run, e **valida cada valor contra o schema
  do jogo** antes de escrever — para que nada seja revertido em silêncio.
- **Um painel in-game** (`mod/PerformanceProfile/`) que expõe as opções
  principais dentro do menu de settings nativo do jogo, com botão para
  aplicar o perfil.

O projeto também documenta **como cada decisão foi derivada** — inclusive os
bugs encontrados nos mods existentes — em [`docs/FINDINGS.md`](docs/FINDINGS.md).

### O que o perfil faz, em resumo

| Efeito | Como |
|---|---|
| Reduz carga de fragmento (GPU) | Nuvens volumétricas `Off`, GI e sombras no mínimo aceito, reflexos e decals `Low` |
| Reduz custo de CPU | `NoSpatialAudio`, densidade de multidão `Low`, culling de streaming, cache de setup de animação, lip sync simplificado |
| Reduz largura de banda de textura (VRAM) | Anisotropia `1`, `TextureQuality=Low`, `LODPreset=Low` |
| Adapta a resolução ao frame time | Dynamic Resolution Scaling ligado, piso de 50% |
| Estabiliza o frame time | Limite de 30 FPS, VSync desligado, Reflex e XeL desligados |
| Reduz I/O e ruído de fundo | Saves em nuvem e telemetria desligados, modo de I/O em `On` |

### O que o perfil deliberadamente **não** faz

| Preservado | Motivo |
|---|---|
| `Hair = True` | Requisito do jogador — usa mod de terceira pessoa |
| `DistantShadows = True` | Desligar **quebra os lightmaps** (aviso do próprio motor) |
| `GlobalIllumination = True` | Desligar causa sombras e iluminação ruins |
| `ConstrastAdaptiveSharpening = True` | AMD FidelityFX CAS, custo baixo, melhora legibilidade |
| Ray Tracing (todas) | Incompatível com Vega 8 |
| Mods instalados | Nenhum mod é removido ou desabilitado |

---

## 2. Tecnologias usadas na análise e no desenvolvimento

### Engenharia reversa

| Ferramenta | Papel | Detalhe |
|---|---|---|
| **[REA](https://github.com/morluto/rea)** (`rea-agents` 6.7.0) | Orquestrador de RE | MCP com 207 ferramentas: análise de binários nativos e managed, decompilação, xrefs, runtime (Frida/GDB/Rizin), captura de evidência |
| **Ghidra 12.1.4** | Decompilador nativo | Análise do `Cyberpunk2077.exe` (PE x64, 60 MB) — 219.702 funções e 72.145 strings mapeadas na execução completa |
| **ilspycmd 11.1.0** | Decompilador .NET | Decompilação completa do `ConfigOverhaulCyberpunk.exe` — foi assim que os bugs dele foram encontrados |
| **luaparse** | Validador de sintaxe Lua | Validação do mod antes de ir para o jogo |
| **Performance counters do Windows** | Medição de runtime | `GPU Engine\Utilization Percentage`, `GPU Process Memory` — mediu o gargalo real |

### Desenvolvimento

| Ferramenta | Papel |
|---|---|
| **PowerShell 5.1+** | Instalador, validação contra o schema, backups |
| **Lua (LuaJIT)** | Mod do painel in-game, executado pelo CET |
| **JSON** | Formato do perfil e persistência do painel |
| **Git** | Versionamento |

### Como a análise foi conduzida

1. **Leitura direta dos arquivos de configuração** — o schema
   (`r6/config/settings/options.json`), o estado real
   (`UserSettings.json`) e os cvars (`user.ini`).
2. **Decompilação** do `ConfigOverhaulCyberpunk.exe` com `ilspycmd`, o que
   revelou a lista completa de 130+ toggles do RED engine em 29 grupos e os
   bugs do próprio mod.
3. **Log do CET em runtime** — expõe a função `HookGameOptionInit(struct
   GameOption *)` e o path `Grupo/Feature/Nome`, confirmando que o
   `user.ini` é lido e aplicado pelo motor.
4. **Varredura de strings do binário** — confirma que `Developer/FeatureToggles`,
   `Rendering/AsyncCompute`, `Rendering/FrostedGlass`, `Rendering/Shadows`,
   `user.ini`, `options.json` e `UserSettings` existem no PE (logo, são lidos
   em runtime).
5. **Validação em runtime** — captura de tela, log do mod, e medição de GPU/CPU
   com o jogo em execução.

O REA orquestrou as etapas 2 e 4 e forneceu a evidência estruturada
(permalinks, dumps, decompilação) que sustentou cada conclusão.

---

## 3. Dependências

### Do lado do jogo

| Dependência | Versão testada | Para quê | Obrigatória? |
|---|---|---|---|
| **[RED4ext](https://github.com/WopsS/RED4ext)** | — | Runtime de mods nativos | **Sim** |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** (CET) | v1.37.1 | Hospeda mods Lua, expõe patches do motor, `GetMod()` | **Sim** |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | v1.96 | Framework de UI nativa — o painel se registra nele | **Sim** (só para o painel) |
| **Redscript** | — | Compilação de scripts de mod | Não |
| **Ghidra 12.1.4** + **JDK 21** | — | Só para análise; **não é necessário para usar o perfil** | Não |

### Do lado do projeto

| Dependência | Para quê |
|---|---|
| **PowerShell 5.1+** (já presente no Windows) | Executar o instalador |
| **Nenhuma** para o painel in-game | O mod Lua é auto-contido |

Nada precisa ser compilado, instalado via gerenciador de pacotes, ou
adicionado ao PATH.

### O que o projeto **não** exige

- Não é preciso o `ConfigOverhaul` — ele serviu de referência de engenharia
  reversa, mas não é dependência.
- Não é preciso IDA Pro, Hopper, Kuna, jadx ou qualquer ferramenta paga.
- Não é preciso baixar, extrair ou recompilar nada do jogo.

---

## 4. Créditos e referências

<a id="creditos"></a>

Este projeto **não** inventou nada que já não existia. Ele se apoia
no trabalho de terceiros — de código, de documentação e de engenharia
reversa — e a contribuição de cada um é fundamental.

### Ferramenta de engenharia reversa

- **[REA — Reverse Engineer Anything](https://github.com/morluto/rea)**,
  por [morluto](https://github.com/morluto) — MIT.
  O servidor MCP que conduziu toda a análise: metadados .NET, decompilação
  via Ghidra, xrefs, e evidência encadeada. Sem ele, a investigação do
  `ConfigOverhaul` e do binário do jogo não teria sido possível.
  → [rea.tools](https://rea.tools/)

### Mods e frameworks analisados

Estes mods **foram submetidos a engenharia reversa** e o que se aprendeu
deles é a base técnica deste projeto. Agradecemos e creditos explícitos:

| Projeto | Autor | Licença | O que contribuiu |
|---|---|---|---|
| **[ConfigOverhaulCyberpunk](https://github.com/derplayer/ConfigOverhaulCyberpunk)** | [derplayer](https://github.com/derplayer) | MIT | Referência fundamental. Decompilado com `ilspycmd`: forneceu a lista completa de 130+ toggles em 29 grupos e a estrutura `GameOption` com path `Grupo/Feature/Nome`. Seus 3 bugs (RayTracing não persistido, grupos inalcançáveis, "Reset" apaga o arquivo) estão documentados em `docs/FINDINGS.md` e são a razão de este projeto existir como alternativa. |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | [justarandomguyintheinternet](https://github.com/justarandomguyintheinternet) | — | Framework de UI nativa. O `addSettingsExample/init.lua` do repositório documenta a API oficial (`GetMod("nativeSettings")`, assinaturas de callback) que o painel usa. Sem ele não existiria painel in-game possível. |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** | [yamashi](https://github.com/yamashi) e colaboradores | MIT | Runtime Lua, patches do motor (`disable_async_compute`, `disable_vignette`, …) e `GetMod()`. O log do CET foi a evidência de runtime que confirmou o mecanismo de `GameOption`. |
| **[RED4ext](https://github.com/WopsS/RED4ext)** | [WopsS](https://github.com/WopsS) e colaboradores | MIT | Script extender sobre o RED engine. |
| **[ArchiveXL](https://github.com/psiberx/cp2077-archive-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Carregamento de recursos; base do ambiente de mods. |
| **[TweakXL](https://github.com/psiberx/cp2077-tweak-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Manipulação de TweakDB em runtime. |
| **[Codeware](https://github.com/psiberx/cp2077-codeware)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Biblioteca para mods de script. |
| **[mod_settings](https://github.com/jackhumbert/mod_settings)** | [jackhumbert](https://github.com/jackhumbert) | MIT | Referência para menus de configuração de mods (implementação via redscript). |
| **[Immersive Third Person](https://www.nexusmods.com/cyberpunk2077/mods/4150)** | — | — | Mod em uso no hardware de referência; motivation para preservar `Hair = True`. |

### Ferramentas de terceiros

| Ferramenta | Autor / projeto | Licença | Uso |
|---|---|---|---|
| **Ghidra** | National Security Agency | Apache-2.0 | Decompilador nativo |
| **ilspycmd** | ILSpy | MIT | Decompilador .NET |
| **Dear ImGui** | Omar Cornut | MIT | Renderização do overlay do CET |
| **spdlog** | Gabi Melman | MIT | Logging do CET |
| **OpenJDK / Temurin** | Eclipse Adoptium | GPL-2.0 com Classpath Exception | Runtime do Ghidra |

### CD PROJEKT RED

Cyberpunk 2077 é © CD PROJEKT S.A. Este projeto é um mod de usuário, não
oficial e não afiliado à CD PROJEKT RED. Nenhum recurso do jogo é
redistribuído aqui.

---

## 5. Instalação

### 5.1 Pelo instalador (recomendado)

**1.** Feche o Cyberpunk 2077.

> O jogo reescreve `UserSettings.json` ao sair. Aplicar com o jogo aberto
> não funciona — o instalador detecta isso e recusa.

**2.** Abra o PowerShell na pasta do projeto:

```powershell
cd E:\CP2077-Performance
```

**3.** (Opcional, recomendado) Veja o que será alterado, sem escrever nada:

```powershell
.\installer\Install-Profile.ps1 -DryRun
```

A saída mostra cada alteração, confirma que os valores conferem com o
schema do jogo, e cria nada.

**4.** Aplique:

```powershell
.\installer\Install-Profile.ps1
```

**5.** Reinicie o jogo.

**Desfazer:**

```powershell
.\installer\Install-Profile.ps1 -Restore
```

Restaura a partir do backup mais recente em `backups/<timestamp>/`.

> Se o PowerShell bloquear a execução de scripts, use
> `powershell -ExecutionPolicy Bypass -File .\installer\Install-Profile.ps1`.

### 5.2 Painel in-game (opcional)

**1.** Copie o mod para a pasta de mods do CET:

```
E:\CP2077-Performance\mod\PerformanceProfile\
  →  <jogo>\bin\x64\plugins\cyber_engine_tweaks\mods\PerformanceProfile\
```

**2.** Reinicie o jogo.

**3.** No menu principal, clique **MODS** e depois entre em **Configurações**.

> ⚠️ As abas de mods **só** aparecem quando se entra em Configurações pelo
> item **"MODS"**. Pelo caminho comum de Configurações elas não aparecem —
> é assim que o Native Settings UI funciona, não um defeito.

**4.** Na aba **"Low-Spec 30 FPS"**, ajuste as opções e clique
**Apply Profile** para gravar `user.ini` e `UserSettings.json` (com backup).

O painel **não** altera o motor ao vivo: o RED engine lê as opções no
startup. Grave, reinicie o jogo.

**5.** Para iterar no painel sem reiniciar: pressione **F1** (overlay do
CET) e clique em **Reload all mods**.

### 5.3 Verificação

| Onde conferir | O que procurar |
|---|---|
| Menu do jogo → Configurações → Vídeo | "QPS máximo" = **LIGADO**, valor **30** |
| Menu do jogo → Configurações → Gráfico | Ray Tracing **desligado**, Nuvens Volumétricas **Off** |
| `engine/config/platform/pc/user.ini` | Seções `[Occlusion]`, `[Crowd]`, `[Animation]`, `[Streaming/Culling]` |
| `mods/PerformanceProfile/performanceprofile-*.log` | `painel registrado com sucesso` |

---

## 6. Diagnóstico no hardware de referência

Medições com o perfil aplicado, em gameplay:

| Métrica | Valor | Leitura |
|---|---|---|
| **GPU utilization** | **89% – 105%** | **Saturada** — é o gargalo |
| CPU | 43% – 46% de 12 threads | Folgada |
| RAM do processo | 7,1 – 7,8 GB | VRAM: 333 MB dedicada + 2.760 MB **compartilhada** |
| QPS máximo | LIGADO, 30 | Conforme o perfil |
| DRS | Ativo, piso 50% | Conforme o perfil |

**Conclusão:** o limite é a **GPU integrada**, não CPU nem memória. Ganhos
adicionais precisam vir de reduzir carga de fragmento ou de renderizar em
resolução interna menor — o DRS é exatamente esse mecanismo, e agora é
ajustável pelo painel.

### Por que o FPS absoluto não é medido automaticamente

| Método | Resultado |
|---|---|
| ETW (`Microsoft-DxgKrnl`) | Exige privilégio de administrador |
| `DwmGetCompositionTimingInfo` | Falha em fullscreen (`DWM_E_COMPOSITIONDISABLED`) |
| PresentMon | Download bloqueado no ambiente de desenvolvimento |
| Overlay do CET | Mede CPU/GPU, não FPS |

Use o **Game Bar do Windows (Win+G)** durante o jogo para ver o FPS absoluto.

---

## 7. Estrutura do projeto

```
CP2077-Performance/
├── profile/lowspec-30fps.json     fonte única de verdade (o perfil)
├── installer/Install-Profile.ps1  aplicador (dry-run / apply / restore)
├── mod/PerformanceProfile/        painel in-game (Lua + Native Settings UI)
├── docs/
│   ├── FINDINGS.md                pesquisa, evidências e bugs encontrados
│   └── NATIVE-SETTINGS-API.md     API de integração e armadilhas do CET
├── CHANGELOG.md
├── README.md
├── LICENSE                        MIT
└── backups/                       backups automáticos (criado pelo instalador)
```

---

## 8. Licença e aviso

**MIT** — veja [`LICENSE`](LICENSE).

Mods de performance podem violar os termos de serviço de plataformas online.
Este projeto é voltado a uso **offline / single-player**. Não use em partidas
online competitivas.

---

# README in English

<a id="en"></a>

## 1. What this is

Cyberpunk 2077 exposes three independent configuration layers, and the game
reads all of them at startup:

| Layer | File | What it controls |
|---|---|---|
| **Menu options** | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` | Graphics quality, dynamic resolution, FPS cap, audio, I/O, services |
| **Engine cvars** | `engine/config/platform/pc/user.ini` | RED engine features: occlusion, streaming culling, animation, physics, hair |
| **Engine patches** | `bin/x64/plugins/cyber_engine_tweaks/config.json` | Anti-aliasing, async compute, vignette, vsync |

A player on a Vega 8 who wants 30 FPS faces three obstacles:

1. **The settings menu hides the options that matter.** The RED engine
   features (which drive actual GPU and CPU cost) **do not appear in the
   UI** — the engine reads them from an INI file. The best-known tool for
   this ([ConfigOverhaul](#credits)) has bugs that limit what it can reach.
2. **The game silently reverts invalid values.** Every list-typed option has
   an exact set of accepted values, declared in the game's schema. A value
   outside that set simply has no effect — no error, no warning.
3. **Configuring it takes three different tools**, and the final result is
   neither auditable nor reversible.

This project solves all three:

- **A single JSON profile** (`profile/lowspec-30fps.json`) describes all
  three layers. Auditable, adjustable without touching code, single source of
  truth.
- **An installer** (`installer/Install-Profile.ps1`) applies the profile
  with automatic backups, a dry-run mode, and **validates every value
  against the game's schema** before writing — so nothing gets reverted in
  silence.
- **An in-game panel** (`mod/PerformanceProfile/`) exposing the main options
  inside the game's native settings menu, with a button to apply the profile.

The project also documents **how each decision was derived** — including the
bugs found in existing mods — in [`docs/FINDINGS.md`](docs/FINDINGS.md).

### What the profile does, in short

| Effect | How |
|---|---|
| Cuts fragment cost (GPU) | Volumetric clouds `Off`, GI and shadows at the lowest accepted value, reflections and decals `Low` |
| Cuts CPU cost | `NoSpatialAudio`, crowd density `Low`, streaming culling, animation setup caching, simplified lip sync |
| Cuts texture bandwidth (VRAM) | Anisotropy `1`, `TextureQuality=Low`, `LODPreset=Low` |
| Adapts resolution to frame time | Dynamic Resolution Scaling on, 50% floor |
| Stabilises frame time | 30 FPS cap, VSync off, Reflex and XeL disabled |
| Cuts background I/O and noise | Cloud saves and telemetry off, I/O mode `On` |

### What the profile deliberately **does not** do

| Preserved | Why |
|---|---|
| `Hair = True` | Player requirement — they use a third-person mod |
| `DistantShadows = True` | Disabling it **breaks the lightmaps** (the engine's own warning) |
| `GlobalIllumination = True` | Disabling it yields ugly shadows and lighting |
| `ConstrastAdaptiveSharpening = True` | AMD FidelityFX CAS, cheap, improves readability |
| Ray Tracing (all) | Incompatible with Vega 8 |
| Installed mods | No mod is removed or disabled |

---

## 2. Technologies used for analysis and development

### Reverse engineering

| Tool | Role | Detail |
|---|---|---|
| **[REA](https://github.com/morluto/rea)** (`rea-agents` 6.7.0) | RE orchestrator | MCP with 207 tools: native and managed binary analysis, decompilation, xrefs, runtime (Frida/GDB/Rizin), evidence capture |
| **Ghidra 12.1.4** | Native decompiler | Analysis of `Cyberpunk2077.exe` (PE x64, 60 MB) — 219,702 functions and 72,145 strings mapped in the completed run |
| **ilspycmd 11.1.0** | .NET decompiler | Full decompilation of `ConfigOverhaulCyberpunk.exe` — this is how its bugs were found |
| **luaparse** | Lua syntax validator | Validates the mod before it reaches the game |
| **Windows performance counters** | Runtime measurement | `GPU Engine\Utilization Percentage`, `GPU Process Memory` — measured the actual bottleneck |

### Development

| Tool | Role |
|---|---|
| **PowerShell 5.1+** | Installer, schema validation, backups |
| **Lua (LuaJIT)** | In-game panel mod, executed by CET |
| **JSON** | Profile format and panel persistence |
| **Git** | Version control |

### How the analysis was conducted

1. **Direct reads of the configuration files** — the schema
   (`r6/config/settings/options.json`), the real state
   (`UserSettings.json`), and the cvars (`user.ini`).
2. **Decompilation** of `ConfigOverhaulCyberpunk.exe` with `ilspycmd`,
   yielding the complete list of 130+ RED engine toggles across 29 groups
   and the `GameOption` structure with its `Group/Feature/Name` path.
3. **CET runtime logs** — exposing `HookGameOptionInit(struct GameOption *)`
   and the `Group/Feature/Name` path, confirming the engine reads and applies
   `user.ini`.
4. **Binary string scanning** — confirming `Developer/FeatureToggles`,
   `Rendering/AsyncCompute`, `Rendering/FrostedGlass`, `Rendering/Shadows`,
   `user.ini`, `options.json` and `UserSettings` exist in the PE (and are
   therefore read at runtime).
5. **Runtime validation** — screen capture, mod logs, and GPU/CPU measurement
   with the game running.

REA orchestrated steps 2 and 4 and supplied the structured evidence
(permalinks, dumps, decompilation) behind every conclusion.

---

## 3. Dependencies

### Game-side

| Dependency | Tested version | Purpose | Required? |
|---|---|---|---|
| **[RED4ext](https://github.com/WopsS/RED4ext)** | — | Native mod runtime | **Yes** |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** (CET) | v1.37.1 | Hosts Lua mods, exposes engine patches, `GetMod()` | **Yes** |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | v1.96 | Native UI framework — the panel registers into it | **Yes** (panel only) |
| **Redscript** | — | Compiling mod scripts | No |
| **Ghidra 12.1.4** + **JDK 21** | — | Analysis only; **not needed to use the profile** | No |

### Project-side

| Dependency | Purpose |
|---|---|
| **PowerShell 5.1+** (already on Windows) | Running the installer |
| **None** for the in-game panel | The Lua mod is self-contained |

Nothing needs to be compiled, installed via a package manager, or added to
PATH.

### What the project does **not** require

- `ConfigOverhaul` is not required — it was a reverse-engineering reference,
  not a dependency.
- No IDA Pro, Hopper, Kuna, jadx, or any paid tool.
- Nothing to download, extract, or rebuild from the game.

---

## 4. Credits and references

<a id="credits"></a>

This project **did not invent** anything that did not already exist. It builds
on the work of others — code, documentation, and reverse engineering — and
each contribution is essential.

### Reverse engineering tool

- **[REA — Reverse Engineer Anything](https://github.com/morluto/rea)**, by
  [morluto](https://github.com/morluto) — MIT.
  The MCP server that drove the entire analysis: .NET metadata, Ghidra-backed
  decompilation, xrefs, and chained evidence. Without it, the investigation
  of ConfigOverhaul and of the game binary would not have been possible.
  → [rea.tools](https://rea.tools/)

### Analysed mods and frameworks

These mods **underwent reverse engineering**, and what was learned from them
is this project's technical foundation. Explicit thanks and credits:

| Project | Author | License | What it contributed |
|---|---|---|---|
| **[ConfigOverhaulCyberpunk](https://github.com/derplayer/ConfigOverhaulCyberpunk)** | [derplayer](https://github.com/derplayer) | MIT | Foundational reference. Decompiled with `ilspycmd`: supplied the complete list of 130+ toggles across 29 groups and the `GameOption` structure with its `Group/Feature/Name` path. Its 3 bugs (RayTracing never persisted, most groups unreachable, "Reset" deletes the file) are documented in `docs/FINDINGS.md` and are the reason this project exists as an alternative. |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | [justarandomguyintheinternet](https://github.com/justarandomguyintheinternet) | — | Native UI framework. The `addSettingsExample/init.lua` in that repository documents the official API (`GetMod("nativeSettings")`, callback signatures) that this panel uses. Without it, an in-game panel would not be possible. |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** | [yamashi](https://github.com/yamashi) and contributors | MIT | Lua runtime, engine patches (`disable_async_compute`, `disable_vignette`, …) and `GetMod()`. CET's logs were the runtime evidence that confirmed the `GameOption` mechanism. |
| **[RED4ext](https://github.com/WopsS/RED4ext)** | [WopsS](https://github.com/WopsS) and contributors | MIT | Script extender for RED engine. |
| **[ArchiveXL](https://github.com/psiberx/cp2077-archive-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Resource loading; the base of the modding environment. |
| **[TweakXL](https://github.com/psiberx/cp2077-tweak-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Runtime TweakDB manipulation. |
| **[Codeware](https://github.com/psiberx/cp2077-codeware)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Library for script mods. |
| **[mod_settings](https://github.com/jackhumbert/mod_settings)** | [jackhumbert](https://github.com/jackhumbert) | MIT | Reference for mod configuration menus (redscript implementation). |
| **[Immersive Third Person](https://www.nexusmods.com/cyberpunk2077/mods/4150)** | — | — | Mod in use on the reference hardware; the reason `Hair = True` is preserved. |

### Third-party tools

| Tool | Author / project | License | Use |
|---|---|---|---|
| **Ghidra** | National Security Agency | Apache-2.0 | Native decompiler |
| **ilspycmd** | ILSpy | MIT | .NET decompiler |
| **Dear ImGui** | Omar Cornut | MIT | CET overlay rendering |
| **spdlog** | Gabi Melman | MIT | CET logging |
| **OpenJDK / Temurin** | Eclipse Adoptium | GPL-2.0 with Classpath Exception | Ghidra runtime |

### CD PROJEKT RED

Cyberpunk 2077 is © CD PROJEKT S.A. This is an unofficial, unaffiliated
user mod. No game assets are redistributed here.

---

## 5. Installation

### 5.1 Via the installer (recommended)

**1.** Close Cyberpunk 2077.

> The game rewrites `UserSettings.json` on exit. Applying while it is open
> does not work — the installer detects this and refuses.

**2.** Open PowerShell in the project folder:

```powershell
cd E:\CP2077-Performance
```

**3.** (Optional, recommended) See what would change, writing nothing:

```powershell
.\installer\Install-Profile.ps1 -DryRun
```

The output lists every change, confirms the values match the game's schema,
and creates nothing.

**4.** Apply:

```powershell
.\installer\Install-Profile.ps1
```

**5.** Restart the game.

**To undo:**

```powershell
.\installer\Install-Profile.ps1 -Restore
```

Restores from the most recent backup in `backups/<timestamp>/`.

> If PowerShell blocks script execution, use
> `powershell -ExecutionPolicy Bypass -File .\installer\Install-Profile.ps1`.

### 5.2 In-game panel (optional)

**1.** Copy the mod into CET's mods folder:

```
E:\CP2077-Performance\mod\PerformanceProfile\
  →  <game>\bin\x64\plugins\cyber_engine_tweaks\mods\PerformanceProfile\
```

**2.** Restart the game.

**3.** In the main menu, click **MODS**, then enter **Settings**.

> ⚠️ Mod tabs **only** appear when you enter Settings through the **"MODS"**
> item. They do not show up via the regular Settings path — that is how
> Native Settings UI works, not a defect.

**4.** On the **"Low-Spec 30 FPS"** tab, adjust the options and click
**Apply Profile** to write `user.ini` and `UserSettings.json` (with
backups).

The panel does **not** change the engine live: RED engine reads options at
startup. Write, then restart the game.

**5.** To iterate on the panel without restarting: press **F1** (CET
overlay) and click **Reload all mods**.

### 5.3 Verification

| Where to check | What to look for |
|---|---|
| Game menu → Settings → Video | "Max FPS" = **ON**, value **30** |
| Game menu → Settings → Graphics | Ray Tracing **off**, Volumetric Clouds **Off** |
| `engine/config/platform/pc/user.ini` | `[Occlusion]`, `[Crowd]`, `[Animation]`, `[Streaming/Culling]` sections |
| `mods/PerformanceProfile/performanceprofile-*.log` | `painel registrado com sucesso` |

---

## 6. Measurements on the reference hardware

Taken with the profile applied, during gameplay:

| Metric | Value | Reading |
|---|---|---|
| **GPU utilization** | **89% – 105%** | **Saturated** — the bottleneck |
| CPU | 43% – 46% of 12 threads | Headroom |
| Process RAM | 7.1 – 7.8 GB | VRAM: 333 MB dedicated + 2,760 MB **shared** |
| Max FPS | ON, 30 | Matches the profile |
| DRS | On, 50% floor | Matches the profile |

**Conclusion:** the limit is the **integrated GPU**, not CPU or memory.
Further gains must come from reducing fragment cost or rendering at a lower
internal resolution — which is exactly what DRS does, and it is now
adjustable from the panel.

### Why absolute FPS is not measured automatically

| Method | Result |
|---|---|
| ETW (`Microsoft-DxgKrnl`) | Requires administrator privileges |
| `DwmGetCompositionTimingInfo` | Fails in fullscreen (`DWM_E_COMPOSITIONDISABLED`) |
| PresentMon | Download blocked in the development environment |
| CET overlay | Measures CPU/GPU, not FPS |

Use the **Windows Game Bar (Win+G)** during the game to see absolute FPS.

---

## 7. Project layout

```
CP2077-Performance/
├── profile/lowspec-30fps.json     single source of truth (the profile)
├── installer/Install-Profile.ps1  applier (dry-run / apply / restore)
├── mod/PerformanceProfile/        in-game panel (Lua + Native Settings UI)
├── docs/
│   ├── FINDINGS.md                research, evidence, and bugs found
│   └── NATIVE-SETTINGS-API.md     integration API and CET gotchas
├── CHANGELOG.md
├── README.md
├── LICENSE                        MIT
└── backups/                       automatic backups (created by the installer)
```

---

## 8. License and notice

**MIT** — see [`LICENSE`](LICENSE).

Performance mods may violate online platform terms of service. This project
targets **offline / single-player** use. Do not use it in competitive online
matches.
