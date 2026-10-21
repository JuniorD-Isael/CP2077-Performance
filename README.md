# CP2077-Performance

Perfil de performance para **Cyberpunk 2077** em hardware de baixo desempenho
(objetivo: **30 FPS jogáveis** em GPU integrada).

**Versão atual: `0.1.0`** — veja o [CHANGELOG](CHANGELOG.md).

Desenvolvido e validado em: **AMD Ryzen 5 5500U (6C/12T, 2.1 GHz) + AMD Radeon
Vega 8 integrada (VRAM compartilhada) + 20 GB RAM**.

O projeto **não remove nenhum mod** e **preserva o cabelo** (`Hair = True`),
porque o jogador usa um mod de terceira pessoa e valoriza o visual do cabelo.

---

## O que faz

Aplica um perfil em três camadas, todas reversíveis:

| Camada | Arquivo | O que controla |
|---|---|---|
| cvars do motor | `engine/config/platform/pc/user.ini` | Features RED engine, culling, oclusão, animação |
| opções do menu | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` | Qualidade gráfica, DRS, FPS, áudio, I/O |
| patches do motor | `bin/x64/plugins/cyber_engine_tweaks/config.json` | Anti-aliasing, async compute, vignette, vsync |

O perfil é um **único arquivo JSON** (`profile/lowspec-30fps.json`) — auditável
e ajustável sem tocar em código.

---

## Instalação rápida

```powershell
# 1. ver o que será alterado (não escreve nada)
.\installer\Install-Profile.ps1 -DryRun

# 2. aplicar (fecha o jogo antes!)
.\installer\Install-Profile.ps1

# 3. desfazer
.\installer\Install-Profile.ps1 -Restore
```

O instalador cria backup automático em `backups/<timestamp>/` antes de qualquer
escrita.

> **Importante:** o jogo **reescreve** `UserSettings.json` ao sair. Aplique com
> o jogo fechado, ou as mudanças serão sobrescritas. O instalador detecta o
> jogo em execução e recusa (use `-Force` para ignorar).

> **Validação contra o schema:** o jogo reverte **silenciosamente** qualquer
> valor de option que não exista na lista `values` do schema
> (`r6/config/settings/options.json`). O instalador valida o perfil contra esse
> schema e reporta valores inválidos em vez de deixar o jogo desfazer a
> alteração por baixo dos panos.

---

## Painel in-game

`mod/PerformanceProfile/` é um mod Lua que cria a aba **"Low-Spec 30 FPS"** no
menu de settings nativo do jogo, usando o framework **Native Settings UI**.

Requisitos: RED4ext, Cyber Engine Tweaks ≥ 1.25, Native Settings UI ≥ 1.4.

Instalação: copie `mod/PerformanceProfile/` para
`bin/x64/plugins/cyber_engine_tweaks/mods/PerformanceProfile/`.

O painel tem três subcategorias e um botão:

| Subcategoria | Widgets |
|---|---|
| **GPU / Render** | seletores de qualidade (nuvens volumétricas, GI, sombras, reflexos, decals, texturas, LOD), slider de anisotropia |
| **Resolução / FPS** | switch de DRS, switch de limite de FPS, sliders de resolução mínima, alvo de FPS e valor do limite |
| **CPU / Sistema** | seletor de densidade de multidão, switches de áudio espacial, saves em nuvem, telemetria, HUD, câmera cinematográfica, seletor de modo de I/O |
| **Aplicar** | botão **Apply** — escreve `user.ini` e `UserSettings.json` com backup |

> **Onde o painel aparece:** as abas de mods só ficam visíveis quando se entra
> em Configurações **pelo item "MODS"** do menu principal. O NativeSettings
> detecta a origem do menu pelo rótulo, então as abas não aparecem no caminho
> comum de Configurações.

O painel **não** altera o motor ao vivo (o REDengine lê as opções no startup).
Ele grava os arquivos corretos com backup e pede para reiniciar.

Detalhes da API e armadilhas do ambiente Lua do CET em
[`docs/NATIVE-SETTINGS-API.md`](docs/NATIVE-SETTINGS-API.md).

---

## Perfil aplicado (resumo)

### Opções do menu

| Opção | Padrão | Perfil | Efeito |
|---|---|---|---|
| `VolumetricCloudsQuality` | Medium | **Off** | Raymarch por pixel — maior custo isolado de GPU |
| `GlobaIlluminationRange` | High | **High** (mínimo aceito) | GI raymarch — não há valor menor neste build |
| `ColorPrecision` | Medium | **Medium** (mínimo aceito) | Custo de shading — não há valor menor neste build |
| `Anisotropy` | 4 | **1** | Reduz bandwidth de textura (VRAM compartilhado) |
| `DynamicResolutionScaling` | false | **true** | Upscale adaptativo — maior ganho único |
| `DRS_MinResolution` | 65 | **50** | Piso de resolução mais baixo |
| `MaximumFPS_OnOff` | false | **true** (30) | Frame pacing; evita CPU a 100% |
| `NoSpatialAudio` | false | **true** | DSP surround é caro em CPU |
| `EnableCloudSaves` | true | **false** | I/O + rede em background |
| `EnableTelemetry` | true | **false** | Telemetria |
| `Cinematic` / `Overheads` | true | **false** | UI |
| `HDDMode` | Auto | **On** | I/O em disco lento |

### cvars do motor — grupos novos

Os grupos abaixo **não são alcançáveis pelo ConfigOverhaul** (bug: ele só cria
seções para `Developer`, `Rendering` e `RayTracing`).

| Grupo | Chave | Ganho |
|---|---|---|
| `Occlusion` | `MultiFrustumUseGlobalOcclusion` | Alto — oclusão global (GPU) |
| `Streaming/Culling` | `VisibilityQuery` | Alto — culling de streaming (CPU) |
| `Streaming/Culling/AutoHideDistanceNear` | `Enabled` | Médio — esconde NPCs distantes |
| `Streaming/Culling/SecondaryRefPoint` | `Enabled` | Médio |
| `Crowd` | `ZCutoffEnabled` | Alto — corta multidão (CPU) |
| `Animation` | `UseAnimSetupCaching` | Alto — cache de setup de animação (CPU) |
| `Animation` | `EnableRefreshLocoWrappersOnUnlock` | Médio |
| `Lipsync` | `SyncToAudio`, `UseAudioAdjustment` | Médio (CPU) |
| `Editor/Characters/Hair` | `UseGlobal/LocalContactShadowsOnHair` | Médio (com cabelo ligado) |
| `Vehicle` | `EnableAirResistane`, `PhysXClampHuge*` | Médio (física) |
| `SnapToTerrainIk` | `Enabled`, `FootLockEnabled` | Médio (CPU/IK) |

### Preservados deliberadamente

| Chave | Valor | Motivo |
|---|---|---|
| `DistantShadows` | `True` | Desligar **quebra os lightmaps** (aviso do próprio motor) |
| `GlobalIllumination` | `True` | Desligar causa sombras e iluminação ruins |
| `Hair` | `True` | Requisito do jogador — usa mod de terceira pessoa |
| `ConstrastAdaptiveSharpening` | `True` | AMD FidelityFX CAS, custo baixo |
| Ray Tracing (todas) | `false` | Incompatível com Vega 8 |

---

## Como isto foi descoberto

Sem usar tools de modding prontos — tudo foi derivado por engenharia reversa
e leitura direta dos arquivos do jogo:

1. **Schema de settings** — `r6/config/settings/options.json` e
   `r6/config/settings/platform/pc/options.json` definem todos os tipos e
   valores válidos das ~323 opções do menu.
2. **Estado real** — `UserSettings.json` mostra o que o jogo persiste de fato.
3. **Mecanismo de cvars** — o log do Cyber Engine Tweaks expõe a função
   `HookGameOptionInit(struct GameOption *)` e o path `Grupo/Feature/Nome`,
   confirmando que o `user.ini` é lido e aplicado pelo motor.
4. **Lista completa de toggles** — decompilando `ConfigOverhaulCyberpunk.exe`
   (via `ilspycmd`), obtendo o recurso `FeatureList` embutido: 130+ toggles em
   29 grupos. Comparado com o `user.ini` real, identificou-se que só 5 grupos
   estavam configurados.
5. **Confirmação no binário** — varredura de strings do `Cyberpunk2077.exe`
   confirma `Developer/FeatureToggles`, `Rendering/AsyncCompute`,
   `Rendering/FrostedGlass`, `Rendering/Shadows`, `user.ini`, `options.json` e
   `UserSettings` presentes no PE (logo, lidos em runtime).

Detalhes em [`docs/`](docs/).

---

## Diagnóstico no hardware de referência

Medições com os performance counters do Windows, com o perfil aplicado e em
gameplay:

| Métrica | Valor | Leitura |
|---|---|---|
| GPU utilization | **89% – 105%** | **saturada** — é o gargalo |
| CPU | 43% – 46% de 12 threads | folgado |
| RAM do processo | 7,1 – 7,8 GB | VRAM: 333 MB dedicada + 2.760 MB compartilhada |
| QPS máximo | LIGADO, 30 | conforme o perfil |
| DRS | ativo, piso 50% | conforme o perfil |

**Conclusão:** o limite é a GPU integrada, não CPU nem memória. Ganhos
adicionais precisam vir de reduzir a carga de fragmento (raymarch, sombras,
pós-processamento) ou de renderizar em resolução interna menor — o DRS é
exatamente o mecanismo para isso, e agora é ajustável pelo painel.

### Por que o FPS absoluto não é medido automaticamente

| Método | Resultado |
|---|---|
| ETW (`Microsoft-DxgKrnl`) | exige privilégio de administrador |
| `DwmGetCompositionTimingInfo` | falha em fullscreen (`DWM_E_COMPOSITIONDISABLED`) |
| PresentMon | download bloqueado no ambiente |
| Overlay do CET | mede CPU/GPU, não FPS |

Use o **Game Bar do Windows (Win+G)** durante o jogo para ver o FPS absoluto.

---

## Estrutura

```
CP2077-Performance/
├── profile/lowspec-30fps.json     fonte única de verdade (perfil)
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

## Licença

MIT — veja [`LICENSE`](LICENSE).

## Aviso

Mods de performance podem violar os termos de serviço de plataformas online.
Este projeto é voltado a uso **offline / single-player**. Não use em partidas
online competitivas.
