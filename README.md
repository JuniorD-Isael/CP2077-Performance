# CP2077-Performance

**[Português (Brasil)](#pt-br)** · **[English](#en)**

Perfil de performance para **Cyberpunk 2077** em hardware de baixo desempenho.
Objetivo: **30 FPS jogáveis** em GPU integrada, com nada de mods removidos.

**Versão atual: `0.1.1-alpha`** — veja o [CHANGELOG](CHANGELOG.md).

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
| **cvars do motor** | `<jogo>\engine\config\platform\pc\user.ini` | Features do RED engine: oclusão, culling de streaming, animação, física, cabelo |
| **Patches do motor** | `<jogo>\bin\x64\plugins\cyber_engine_tweaks\config.json` | Anti-aliasing, async compute, vignette, vsync |

Um jogador que quer 30 FPS num Vega 8 enfrenta três obstáculos:

1. **O menu de settings esconde as opções que importam.** As features do RED
   engine (que controlam o custo real de GPU e CPU) **não aparecem na
   interface** — o motor as lê de um arquivo INI. A ferramenta mais conhecida
   para isso ([ConfigOverhaul](#creditos)) tem bugs que limitam o que ela
   alcança: de 29 grupos de cvar, ela só gerencia 3.
2. **O jogo reverte valores inválidos silenciosamente.** Cada opção do tipo
   lista tem um conjunto exato de valores aceitos, declarado no esquema do
   jogo. Passar um valor fora dessa lista simplesmente não surte efeito —
   sem erro, sem aviso.
3. **A configuração precisa de três ferramentas diferentes** para cobrir as
   três camadas, e o resultado final não é auditável nem reversível.

Este projeto resolve os três:

- **Um perfil único em JSON** (`profile/lowspec-30fps.json`) descreve as três
  camadas. É auditável, ajustável sem mexer em código, e serve de fonte
  única de verdade.
- **Um aplicativo desktop** (`CP2077-Performance.exe`) aplica o perfil com
  backup automático, opção de simulação, restauração, e **validação de cada
  valor** contra o esquema do jogo **e contra o executável** — para que nada
  seja revertido em silêncio.
- **O mesmo aplicativo mostra o estado efetivo**: o que o jogo está usando
  de fato, ao lado do que o perfil define.

### O que o perfil faz, em resumo

| Efeito | Como |
|---|---|
| Reduz carga de fragmento (GPU) | Nuvens volumétricas `Off`, GI e sombras no mínimo aceito, reflexos e decals `Low` |
| Elimina passes de async compute (GPU) | 11 chaves de `Rendering/AsyncCompute` desligadas — passes assíncronos são custo puro numa GPU integrada |
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
| `Ray Tracing` (todas) | Incompatível com Vega 8 |
| Mods instalados | Nenhum mod é removido ou desabilitado |
| **A interface do jogo** | A redução de HUD é **opcional** (ver §4) |

### O que a medição em runtime mostrou

Uma sessão de jogo instrumentada (amostragem de ~1 s durante 19 minutos):

| Métrica | p25 | mediana | p95 | max |
|---|---|---|---|---|
| GPU utilization % | 94 | **95** | 106 | 112 |
| CPU (de 12 núcleos) | 3,7 | 4,2 | 8,6 | 15,1 |
| VRAM compartilhada MB | 2.467 | 2.528 | 2.654 | 2.754 |
| RAM do processo MB | 7.139 | 7.205 | 7.674 | 7.781 |

**72% das amostras com a GPU acima de 95%**, e a CPU com folga. A leitura é
direta: **o limite é custo de fragmento na GPU, não CPU.**

O FPS real, medido por um mod de instrumentação dentro do jogo: **mediana 24
em gameplay** (91% dos segundos abaixo de 30 FPS) contra 29 no menu.

---

## 2. O aplicativo

Aplicativo desktop Windows (.NET 10, WinForms). Um executável, sem
instalador, sem runtime a instalar.

| | |
|---|---|
| **Idioma** | Segue o sistema: `pt-BR` → português, demais → inglês |
| **Tema** | Segue o sistema: claro ou escuro |
| **Escala** | Funciona em monitores com DPI diferente de 100% |
| **Dependências** | Nenhuma. O `build.cmd` publica *self-contained* |

### O que ele faz

1. **Aponta a pasta do jogo.** O usuário informa onde o Cyberpunk 2077 está
   instalado, e o aplicativo deriva todo o resto. Sem varredura de disco e
   sem lógica por loja: **o diretório de save é definido pela CD PROJEKT
   RED, não pela loja**, então a mesma build serve a Steam, Epic e GOG.
2. **Valida.** Cada valor de menu é conferido contra o esquema do jogo; cada
   chave de cvar é conferida contra as strings do executável. Erro impede a
   aplicação.
3. **Simula.** Mostra exatamente o que seria alterado, sem escrever nada.
4. **Aplica.** Escreve as três camadas, com backup automático antes.
5. **Restaura.** Dos backups mantidos, qualquer um pode ser devolvido.

### As três camadas, na prática

O aplicativo **reescreve só o que precisa**:

```
UserSettings.json   2768 linhas -> 2768 linhas, 26 alteradas
                    (todas campos "value" ou "index")
user.ini            5 seções -> 15 seções, ordem das 5 originais preservada
                    e todos os comentários do motor intactos
config.json         3 patches mudados, os outros 2 intocados
```

O `user.ini` do motor carrega comentários que documentam efeitos colaterais
(`;Don't disable. (breaks lightmaps)`, `;Eats 2-5 fps`). Eles são
preservados: um parser de INI de uso geral jogaria fora essa documentação.

---

## 3. Compilação

```powershell
git clone https://github.com/CP2077-Performance/CP2077-Performance
cd CP2077-Performance
build.cmd
```

`build.cmd` publica **self-contained** em `dist\CP2077-Performance.exe` (um
arquivo, sem runtime instalado). Para uma build pequena, que exige o .NET 10
Desktop Runtime instalado:

```powershell
build.cmd framework-dependent
```

Requisitos de desenvolvimento: **SDK do .NET 10**. O `app/` tem a solução
completa, e `app\CP2077Performance.Tests` é o runner de verificações:

```powershell
dotnet run --project app\CP2077Performance.Tests
```

---

## 4. Uso

**1.** Abra `CP2077-Performance.exe`.

**2.** Informe a **pasta do jogo** e clique em **Validar**. O aplicativo
confere a instalação e mostra quais das três camadas estão disponíveis.

> O jogo precisa estar **fechado** para aplicar. Ele carrega as opções de
> menu no startup e descarrega o estado em memória para o arquivo ao encerrar
> — o que anularia a aplicação.

**3.** Clique em **Simular** para ver o que seria alterado, sem escrever nada.

**4.** Clique em **Aplicar**. O aplicativo:

- cria um **backup automático** do que vai alterar;
- escreve as três camadas;
- **relê os arquivos** e compara com o esperado — divergência é reportada
  como falha, não como sucesso silencioso;
- registra cada alteração e **a origem de cada valor**.

**5.** Reinicie o jogo.

### A variante de HUD

O perfil tem uma **variante opcional**: *HUD mínima*. Ela desliga minimapa,
marcadores de objetos, nomes de NPCs, barra de vida, rastreador de missões e
afins, e liga os presets de HUD reduzido que o próprio jogo oferece.

Cada elemento desligado é um draw call a menos na GPU — que é exatamente o
gargalo medido. **É opcional e fica desligado por padrão**: a interface do
jogo não é coisa que um perfil de performance deva mudar sozinho.

Com a variante ligada, desligá-la **devolve** as opções afetadas ao valor
padrão do próprio jogo (`default_value` do esquema), de forma determinística.

### Backups

Cada aplicação que altera algo cria uma versão de backup em
`%LOCALAPPDATA%\CP2077-Performance\backups\`. São mantidas **as três mais
recentes**: ao criar a quarta, a mais antiga é descartada; a quinta descarta a
segunda, e assim por diante.

O botão **Restaurar** devolve qualquer uma das versões mantidas.

---

## 5. O que mudou nesta versão (leia antes de atualizar)

**Removido, com quebra:**

- **O instalador de linha de comando** (`installer\Install-Profile.ps1`).
  Use o aplicativo.
- **O painel in-game** (`MODS → Low-Spec 30 FPS`). Ele **nunca conseguiu
  aplicar nada**: o sandbox Lua do Cyber Engine Tweaks confina o `io` ao
  diretório do mod — `os.getenv` não existe, e caminho absoluto ou com `..`
  são bloqueados. Três falhas registradas em log, todas com a mesma causa.
  Detalhes em [`docs/FINDINGS.md`](docs/FINDINGS.md) §13.1 e §14.4.

**Adicionado:** o aplicativo, o schema do perfil com variantes, a validação
contra o executável, a reconciliação de `value` e `index`, e a variante de
HUD.

---

## 6. Estrutura do projeto

```
CP2077-Performance/
├── app/                          fonte do aplicativo
│   ├── CP2077Performance.Core/   núcleo: sem dependência de interface
│   ├── CP2077Performance.Ui/     interface WinForms, i18n, tema
│   ├── CP2077Performance.App/    executável
│   └── CP2077Performance.Tests/  verificações
├── build.cmd                     script de build
├── profile/
│   ├── lowspec-30fps.json        fonte única de verdade (o perfil)
│   └── profile.schema.json       schema do formato de perfil
├── docs/
│   ├── FINDINGS.md               pesquisa, evidências e bugs encontrados
│   └── NATIVE-SETTINGS-API.md   API de integração e armadilhas do CET
├── CHANGELOG.md
├── README.md
└── LICENSE                       MIT
```

---

## 7. Licença e aviso

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
| **Engine cvars** | `<game>\engine\config\platform\pc\user.ini` | RED engine features: occlusion, streaming culling, animation, physics, hair |
| **Engine patches** | `<game>\bin\x64\plugins\cyber_engine_tweaks\config.json` | Anti-aliasing, async compute, vignette, vsync |

A player on a Vega 8 who wants 30 FPS faces three obstacles:

1. **The settings menu hides the options that matter.** The RED engine
   features (which drive actual GPU and CPU cost) **do not appear in the
   UI** — the engine reads them from an INI file. The best-known tool for
   this ([ConfigOverhaul](#credits)) has bugs that limit what it can reach:
   of 29 cvar groups, it manages only 3.
2. **The game silently reverts invalid values.** Every list-typed option has
   an exact set of accepted values, declared in the game's schema. A value
   outside that set simply has no effect — no error, no warning.
3. **Configuring it takes three different tools** to cover the three layers,
   and the final result is neither auditable nor reversible.

This project solves all three:

- **A single JSON profile** (`profile/lowspec-30fps.json`) describes all
  three layers. Auditable, adjustable without touching code, a single source
  of truth.
- **A desktop application** (`CP2077-Performance.exe`) applies the profile
  with automatic backup, a dry-run, restore, and **validation of every
  value** against the game's schema **and against the executable** — so
  nothing gets reverted in silence.
- **The same application shows the effective state**: what the game is
  really using, next to what the profile defines.

### What the profile does, in short

| Effect | How |
|---|---|
| Cuts fragment cost (GPU) | Volumetric clouds `Off`, GI and shadows at the lowest accepted value, reflections and decals `Low` |
| Removes async compute passes (GPU) | 11 keys under `Rendering/AsyncCompute` off — async passes are pure overhead on an integrated GPU |
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
| Ray Tracing (all) | Incompatible with Vega 8 |
| Installed mods | No mod is removed or disabled |
| **The game UI** | The HUD reduction is **optional** (see §4) |

### What the runtime measurement showed

An instrumented game session (~1 s sampling over 19 minutes):

| Metric | p25 | median | p95 | max |
|---|---|---|---|---|
| GPU utilization % | 94 | **95** | 106 | 112 |
| CPU (of 12 cores) | 3.7 | 4.2 | 8.6 | 15.1 |
| Shared VRAM MB | 2,467 | 2,528 | 2,654 | 2,754 |
| Process RAM MB | 7,139 | 7,205 | 7,674 | 7,781 |

**72% of samples above 95% GPU**, with CPU to spare. The reading is direct:
**the limit is GPU fragment cost, not CPU.**

Real FPS, measured by an in-game instrumentation mod: **median 24 in
gameplay** (91% of seconds below 30 FPS) against 29 in the menu.

---

## 2. The application

Windows desktop app (.NET 10, WinForms). One executable, no installer, no
runtime to install.

| | |
|---|---|
| **Language** | Follows the system: `pt-BR` → Portuguese, otherwise English |
| **Theme** | Follows the system: light or dark |
| **Scaling** | Works on monitors with DPI other than 100% |
| **Dependencies** | None. `build.cmd` publishes *self-contained* |

### What it does

1. **Point at the game folder.** The user says where Cyberpunk 2077 is
   installed and the application derives everything else. No disk scan and
   no per-store logic: **the save directory is defined by CD PROJEKT RED, not
   by the store**, so one build serves Steam, Epic and GOG.
2. **Validate.** Every menu value is checked against the game's schema; every
   cvar key is checked against the executable's strings. An error stops the
   application.
3. **Simulate.** Shows exactly what would change, writing nothing.
4. **Apply.** Writes the three layers, with an automatic backup beforehand.
5. **Restore.** Any of the retained backups can be rolled back.

### The three layers, in practice

The application **rewrites only what it must**:

```
UserSettings.json   2768 lines -> 2768 lines, 26 changed
                    (all "value" or "index" fields)
user.ini            5 sections -> 15 sections, original order preserved
                    and every engine comment intact
config.json         3 patches changed, the other 2 untouched
```

The engine's `user.ini` carries comments documenting side effects
(`;Don't disable. (breaks lightmaps)`, `;Eats 2-5 fps`). They are
preserved: a general-purpose INI parser would throw that documentation away.

---

## 3. Building

```powershell
git clone https://github.com/CP2077-Performance/CP2077-Performance
cd CP2077-Performance
build.cmd
```

`build.cmd` publishes **self-contained** to `dist\CP2077-Performance.exe` (a
single file, no runtime to install). For a smaller build that requires the
.NET 10 Desktop Runtime:

```powershell
build.cmd framework-dependent
```

Development requires the **.NET 10 SDK**. `app/` holds the full solution, and
`app\CP2077Performance.Tests` is the verification runner:

```powershell
dotnet run --project app\CP2077Performance.Tests
```

---

## 4. Usage

**1.** Open `CP2077-Performance.exe`.

**2.** Enter the **game folder** and click **Validate**. The application
checks the installation and shows which of the three layers are available.

> The game must be **closed** to apply. It loads the menu options at
> startup and flushes its in-memory state to the file on exit — which would
> undo the application.

**3.** Click **Simulate** to see what would change, writing nothing.

**4.** Click **Apply**. The application:

- takes an **automatic backup** of what it is about to change;
- writes the three layers;
- **reads the files back** and compares against the expected result — a
  mismatch is reported as a failure, not as silent success;
- records every change and **the origin of every value**.

**5.** Restart the game.

### The HUD variant

The profile has one **optional variant**: *minimal HUD*. It turns off the
minimap, object markers, NPC names, the health bar, the quest tracker and
the like, and turns on the reduced-HUD presets the game itself offers.

Each element turned off is one draw call less on the GPU — which is exactly
the measured bottleneck. **It is optional and off by default**: the game's UI
is not something a performance profile should change on its own.

With the variant on, turning it off **returns** the affected options to the
game's own default (`default_value` from the schema), deterministically.

### Backups

Every application that changes something creates a backup version under
`%LOCALAPPDATA%\CP2077-Performance\backups\`. **The three most recent are
kept**: creating a fourth discards the oldest; the fifth discards the second,
and so on.

The **Restore** button rolls back to any retained version.

---

## 5. What changed in this version (read before updating)

**Removed, breaking:**

- **The command-line installer** (`installer\Install-Profile.ps1`).
  Use the application.
- **The in-game panel** (`MODS → Low-Spec 30 FPS`). It **never managed to
  apply anything**: the CET's Lua sandbox confines `io` to the mod's own
  directory — `os.getenv` does not exist, and absolute or `..` paths are
  blocked. Three failures logged, all with the same cause. Details in
  [`docs/FINDINGS.md`](docs/FINDINGS.md) §13.1 and §14.4.

**Added:** the application, the profile schema with variants, validation
against the executable, `value`/`index` reconciliation, and the HUD variant.

---

## 6. Project layout

```
CP2077-Performance/
├── app/                          application source
│   ├── CP2077Performance.Core/   core: no UI dependency
│   ├── CP2077Performance.Ui/     WinForms UI, i18n, theme
│   ├── CP2077Performance.App/    executable
│   └── CP2077Performance.Tests/  verification runner
├── build.cmd                     build script
├── profile/
│   ├── lowspec-30fps.json        single source of truth (the profile)
│   └── profile.schema.json       profile format schema
├── docs/
│   ├── FINDINGS.md               research, evidence and bugs found
│   └── NATIVE-SETTINGS-API.md   integration API and CET gotchas
├── CHANGELOG.md
├── README.md
└── LICENSE                       MIT
```

---

## 7. License and notice

**MIT** — see [`LICENSE`](LICENSE).

Performance mods may violate online platform terms of service. This project
targets **offline / single-player** use. Do not use in competitive online
matches.

---

# Créditos

<a id="creditos"></a>

Este projeto **não inventou nada** que já não existia. Ele se apoia no
trabalho de terceiros — de código, de documentação e de engenharia reversa — e
a contribuição de cada um é fundamental.

## Ferramenta de engenharia reversa

- **[REA — Reverse Engineer Anything](https://github.com/morluto/rea)**, por
  [morluto](https://github.com/morluto) — MIT. O servidor MCP que conduziu a
  investigação: metadados .NET, decompilação via Ghidra, xrefs, e evidência
  encadeada. → [rea.tools](https://rea.tools/)

## Mods e frameworks analisados

Estes mods **foram submetidos a engenharia reversa**, e o que se aprendeu
deles é a base técnica deste projeto:

| Projeto | Autor | Licença | O que contribuiu |
|---|---|---|---|
| **[ConfigOverhaulCyberpunk](https://github.com/derplayer/ConfigOverhaulCyberpunk)** | [derplayer](https://github.com/derplayer) | MIT | Referência fundamental. Decompilado com `ilspycmd`: forneceu a lista completa de 130+ toggles em 29 grupos e a estrutura `GameOption` com path `Grupo/Feature/Nome`. Seus 3 bugs (RayTracing nunca persistido, 3 de 29 grupos alcançáveis, "Reset" apaga o arquivo) estão documentados em `docs/FINDINGS.md` e são a razão de este projeto existir como alternativa. |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | [justarandomguyintheinternet](https://github.com/justarandomguyintheinternet) | — | Framework de UI nativa. A investigação do seu sandbox — e a prova de que o painel in-game não alcança o jogo — está em `docs/FINDINGS.md` §13.1. |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** | [yamashi](https://github.com/yamashi) e colaboradores | MIT | Runtime Lua, patches do motor e o hook `HookGameOptionInit` que expõe as opções que o motor de fato registra. |
| **[RED4ext](https://github.com/WopsS/RED4ext)** | [WopsS](https://github.com/WopsS) e colaboradores | MIT | Script extender sobre o RED engine. |
| **[ArchiveXL](https://github.com/psiberx/cp2077-archive-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Carregamento de recursos; base do ambiente de mods. |
| **[TweakXL](https://github.com/psiberx/cp2077-tweak-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Manipulação de TweakDB em runtime. |
| **[Codeware](https://github.com/psiberx/cp2077-codeware)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Biblioteca para mods de script. |
| **[Immersive Third Person](https://www.nexusmods.com/cyberpunk2077/mods/4150)** | — | — | Mod em uso no hardware de referência; o motivo de preservar `Hair = True`. |

## Ferramentas de terceiros

| Ferramenta | Autor / projeto | Licença | Uso |
|---|---|---|---|
| **.NET 10** | Microsoft | MIT | Runtime e WinForms do aplicativo |
| **Ghidra** | National Security Agency | Apache-2.0 | Decompilador nativo |
| **ilspycmd** | ILSpy | MIT | Decompilador .NET |
| **Dear ImGui** | Omar Cornut | MIT | Renderização do overlay do CET |
| **spdlog** | Gabi Melman | MIT | Logging do CET |
| **OpenJDK / Temurin** | Eclipse Adoptium | GPL-2.0 com Classpath Exception | Runtime do Ghidra |

## CD PROJEKT RED

Cyberpunk 2077 é © CD PROJEKT S.A. Este projeto é um mod de usuário, não
oficial e não afiliado à CD PROJEKT RED. Nenhum recurso do jogo é
redistribuído aqui.
