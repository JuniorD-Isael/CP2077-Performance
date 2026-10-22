# CP2077-Performance

**[Português (Brasil)](#pt-br)** · **[English](#en)**

Perfil de performance para **Cyberpunk 2077** em hardware de baixo desempenho.
Objetivo: **30 FPS jogáveis** em GPU integrada, sem remover nenhum mod.

**Versão: `0.2.0-alpha`** — veja o [CHANGELOG](CHANGELOG.md). Validado em
**AMD Ryzen 5 5500U + AMD Radeon Vega 8 integrada + 20 GB RAM**. O projeto
**não remove nenhum mod** e **preserva o cabelo** (`Hair = True`).

---

# README em Português (PT-BR)

<a id="pt-br"></a>

## 1. O que é

O Cyberpunk 2077 tem **três camadas** de configuração, lidas pelo jogo no
startup:

| Camada | Arquivo | O que controla |
|---|---|---|
| **Opções do menu** | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` | Qualidade gráfica, resolução dinâmica, FPS, áudio, I/O |
| **cvars do motor** | `<jogo>\engine\config\platform\pc\user.ini` | Features do RED engine: oclusão, culling, animação, física, cabelo |
| **Patches do motor** | `<jogo>\bin\x64\plugins\cyber_engine_tweaks\config.json` | Anti-aliasing, async compute, vignette, vsync |

O problema: **o menu de settings esconde as opções que importam.** As features
do RED engine, que controlam o custo real de GPU e CPU, não aparecem na
interface — o motor as lê de um arquivo INI. E o jogo **reverte valores
inválidos em silêncio**: cada opção de lista tem um conjunto exato de valores
aceitos, e um valor fora dele não surte efeito.

Este projeto resolve os dois com um perfil único em JSON, aplicado por um
aplicativo desktop que **valida cada valor** contra o esquema do jogo e contra
o executável antes de escrever.

| Efeito | Como |
|---|---|
| Reduz carga de fragmento (GPU) | Nuvens volumétricas `Off`, GI e sombras no mínimo, reflexos e decals `Low` |
| Elimina passes de async compute | 11 chaves de `Rendering/AsyncCompute` desligadas |
| Reduz custo de CPU | `NoSpatialAudio`, multidão `Low`, culling de streaming |
| Reduz banda de textura (VRAM) | Anisotropia `1`, `TextureQuality=Low`, `LODPreset=Low` |
| Adapta a resolução ao frame time | Dynamic Resolution Scaling ligado, piso de 50% |
| Estabiliza o frame time | Limite de 30 FPS, VSync, Reflex e XeL desligados |
| Reduz I/O e ruído de fundo | Saves em nuvem e telemetria desligados, I/O em `On` |

O perfil **não** mexe em `Hair`, `DistantShadows`, `GlobalIllumination` ou Ray
Tracing, não remove mod nenhum, e **não toca na interface do jogo (HUD)**.

O aplicativo **reescreve só o que precisa**: numa aplicação real,
`UserSettings.json` ficou com 2768 linhas antes e depois, e uma alterada — só o
campo `value`. O `user.ini` mantém a ordem das seções e todos os comentários do
motor, que documentam efeitos colaterais.

### Fronteira de escopo e modo de exibição

O aplicativo foca **exclusivamente** no setup de performance para hardware de
baixo desempenho e em **forçar o jogo em tela cheia (`WindowMode = Fullscreen`)
preservando a resolução que o usuário escolheu no jogo** (ex.: `1366x768`,
`1280x720`, `1600x900`, `1920x1080`), sem enumerar monitores nem sobrescrever
`Resolution` — funcionando em qualquer setup de monitor e mantendo a escala
dinâmica de resolução interna (`DynamicResolutionScaling`).

Qualquer manipulação ou alternância de HUD foi separada em **um projeto
próprio** (ponto de partida preservado em `handoff/hud-toggle/`). O perfil
curado mantém o mecanismo de variantes pronto para futuras opções de
performance, mas não declara mais opções de HUD.

## 2. Como usar

**1.** Abra `CP2077-Performance.exe`.

**2.** Informe a **pasta do jogo** e clique em **Validar**.

> O save fica sempre em `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077`, seja
> qual for a loja: sem varredura de disco e sem lógica por loja, a mesma build
> serve a Steam, Epic e GOG.

> O jogo precisa estar **fechado** para aplicar: ele carrega as opções de menu
> no startup e descarrega o estado em memória para o arquivo ao encerrar — o
> que anularia a aplicação.

**3.** O cartão **Status do Perfil** mostra o resumo do que será feito, quantas
configurações serão alteradas e o estado real lido dos arquivos em disco (se o
perfil está aplicado, desde quando, ou quantas configurações divergem).

**4.** Clique em **Aplicar Perfil**. O aplicativo cria um **backup automático**,
escreve as três camadas (incluindo `WindowMode = Fullscreen` sem alterar a sua
resolução), **relê os arquivos** e compara com o esperado — divergência é
reportada como falha, não como sucesso silencioso — e registra cada alteração
com **a origem de cada valor**.

**5.** Abra o jogo. A tabela técnica completa de todas as opções de menu, cvars
do motor e patches do CET está documentada em
[`docs/ARQUITETURA.md` §10](docs/ARQUITETURA.md#10-referência-técnica-completa-das-três-camadas).

## 3. Como desfazer

Cada aplicação que altera algo cria uma versão de backup em
`%LOCALAPPDATA%\CP2077-Performance\backups\`. São mantidas **as três mais
recentes**: ao criar a quarta, a mais antiga é descartada; a quinta descarta a
segunda, e assim por diante.

Clique em **Restaurar Backup** na tela principal para reverter imediatamente à
versão mais recente.

## 4. Compilação

```powershell
git clone https://github.com/CP2077-Performance/CP2077-Performance
cd CP2077-Performance
build.cmd
```

`build.cmd` publica **self-contained** em `dist\CP2077-Performance.exe` (um
arquivo, sem runtime instalado); `build.cmd framework-dependent` publica a
build pequena, que exige o .NET 10 Desktop Runtime. Desenvolver exige o **SDK
do .NET 10**, e o runner de verificações é:

```powershell
dotnet run --project app\CP2077Performance.Tests
```

## 5. Documentação

[`docs/ARQUITETURA.md`](docs/ARQUITETURA.md) — como funciona por dentro, de
onde vem cada valor, o que foi medido ·
[`docs/FINDINGS.md`](docs/FINDINGS.md) — pesquisa, evidências e bugs ·
[`docs/ROADMAP.md`](docs/ROADMAP.md) — o que já está resolvido e o que não está ·
[`docs/NATIVE-SETTINGS-API.md`](docs/NATIVE-SETTINGS-API.md) — API do CET

## 6. Licença

**MIT** — veja [`LICENSE`](LICENSE). Mods de performance podem violar os termos
de serviço de plataformas online: este projeto é voltado a uso **offline /
single-player**.

---

# README in English

<a id="en"></a>

The full English version lives in **[README.en.md](README.en.md)** — the same
content, in English: what the three configuration layers are, what the profile
does to the game, how to use the application, how to undo, and how to build it.

Quick facts:

- **Three configuration layers**: menu options (UserSettings.json), engine
  cvars (user.ini) and engine patches (config.json).
- **The problem**: the settings menu hides the options that drive real GPU
  and CPU cost, and the game silently reverts values outside a list option's
  accepted set.
- **The fix**: one JSON profile, applied by a desktop application that
  validates every value against the game's schema and the executable before
  writing.
- **Fullscreen preserving user resolution**: forces `WindowMode = Fullscreen`
  (`index = 2`) while preserving whatever `Resolution` you set in the game and
  keeping dynamic resolution scaling intact.
- **Scope boundary**: HUD manipulation and in-game HUD toggling live in a
  **separate project** (`handoff/hud-toggle/`); this application touches only
  performance setup and fullscreen display.
- **Undo**: every application takes a backup; the three most recent are kept,
  and **Restore** rolls back to the most recent one.

---
# Créditos

<a id="creditos"></a>

Este projeto **não inventou nada** que já não existia: apoia-se no trabalho de
terceiros — de código, de documentação e de engenharia reversa.

- **[REA — Reverse Engineer Anything](https://github.com/morluto/rea)**, por
  [morluto](https://github.com/morluto) — MIT. O servidor MCP que conduziu a
  investigação: metadados .NET, decompilação via Ghidra, xrefs e evidência
  encadeada. → [rea.tools](https://rea.tools/)

| Projeto | Autor | Licença | O que contribuiu |
|---|---|---|---|
| **[ConfigOverhaulCyberpunk](https://github.com/derplayer/ConfigOverhaulCyberpunk)** | [derplayer](https://github.com/derplayer) | MIT | Os 130+ toggles em 29 grupos e a estrutura de opções com caminho `Grupo/Feature/Nome`. Seus 3 bugs estão em `docs/FINDINGS.md`. |
| **[Native Settings UI](https://github.com/justarandomguyintheinternet/CP77_nativeSettings)** | [justarandomguyintheinternet](https://github.com/justarandomguyintheinternet) | — | Framework de UI nativa; a investigação do sandbox está em `docs/FINDINGS.md` §13.1. |
| **[Cyber Engine Tweaks](https://github.com/yamashi/CyberEngineTweaks)** | [yamashi](https://github.com/yamashi) e colaboradores | MIT | Runtime Lua, patches do motor e o hook que expõe as opções registradas. |
| **[RED4ext](https://github.com/WopsS/RED4ext)** | [WopsS](https://github.com/WopsS) e colaboradores | MIT | Script extender sobre o RED engine. |
| **[ArchiveXL](https://github.com/psiberx/cp2077-archive-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Carregamento de recursos; base do ambiente de mods. |
| **[TweakXL](https://github.com/psiberx/cp2077-tweak-xl)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Manipulação de TweakDB em runtime. |
| **[Codeware](https://github.com/psiberx/cp2077-codeware)** | [Pavel Siberx](https://github.com/psiberx) | MIT | Biblioteca para mods de script. |
| **[Immersive Third Person](https://www.nexusmods.com/cyberpunk2077/mods/4150)** | — | — | Mod em uso no hardware de referência. |

| Ferramenta | Licença | Uso |
|---|---|---|
| **.NET 10** (Microsoft) | MIT | Runtime e WinForms |
| **Ghidra** (NSA) | Apache-2.0 | Decompilador nativo |
| **ilspycmd** (ILSpy) | MIT | Decompilador .NET |
| **Dear ImGui** (Omar Cornut) | MIT | Overlay do CET |
| **spdlog** (Gabi Melman) | MIT | Logging do CET |
| **OpenJDK / Temurin** | GPL-2.0 com Classpath Exception | Runtime do Ghidra |

Cyberpunk 2077 é © CD PROJEKT S.A. Este projeto é um mod de usuário, não
oficial e não afiliado à CD PROJEKT RED. Nenhum recurso do jogo é
redistribuído aqui.
