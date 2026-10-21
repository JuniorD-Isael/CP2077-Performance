# Pesquisa e achados

Documenta como o perfil foi derivado, sem depender de tools de modding prontos.

---

## 1. As três camadas de configuração

| Camada | Arquivo | Natureza |
|---|---|---|
| **Schema** | `r6/config/settings/options.json` (+ `platform/pc/options.json`) | Define tipos e valores válidos das opções do menu. **Não** é estado. |
| **Estado do menu** | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json` | 323 opções com `name`/`type`/`value`. É o que o jogo lê e persiste. |
| **cvars do motor** | `engine/config/platform/pc/user.ini` | INI com grupos `[Grupo/Feature]` e chaves. Lido pelo motor no startup. |
| **patches do motor** | `bin/x64/plugins/cyber_engine_tweaks/config.json` | Flags de patch do CET (`disable_async_compute` etc.) |

O `options.json` é a referência do schema: cada opção tem `name`, `type`
(`bool`/`int`/`float`/`string_list`/`name_list`/`int_list`/`float_list`),
`default_value`, `is_visible`, `in_pre_game`, `in_game` e
`update_policy` (`immediately` vs `require_confirmation` vs `require_restart`).

---

## 2. Mecanismo de cvars — confirmado em runtime

O log do Cyber Engine Tweaks durante uma sessão de jogo expõe:

```
[void *__cdecl HookGameOptionInit(struct GameOption *)] [13016]
    Disabled hidden setting "Developer/FeatureToggles/Antialiasing"
    Disabled hidden setting "Developer/FeatureToggles/ScreenSpaceReflection"
```

Duas conclusões:

1. O motor usa uma struct **`GameOption`** registrada por hook, com path no
   formato **`Grupo/Feature/Nome`** — idêntico ao esquema do `user.ini`
   (`[Developer/FeatureToggles]` + chave `Antialiasing`).
2. O **CET esconde** esses settings do menu. Por isso não aparecem nas opções
   do jogo — e por isso o `user.ini` é a única forma de alcançá-los.

---

## 3. O `user.ini` real cobre só 5 de 29 grupos

`engine/config/platform/pc/user.ini` contém:

```
[Developer/FeatureToggles]
[Rendering/AsyncCompute]
[Rendering/FrostedGlass]
[Rendering]
[Rendering/Shadows]
```

A lista completa de grupos (130+ toggles em 29 grupos) foi obtida decompilando
`ConfigOverhaulCyberpunk.exe` e extraindo o recurso `FeatureList` embutido:

```
Animation, ChoiceSettings, Crowd, Customization, Debug, DebugConsole,
Developer/FeatureToggles, DLSS, DRS, Editor/Characters/Hair,
Editor/Characters/Hair/HACKS, EntityVisualController, Input/Gestures,
Lipsync, Multiplayer, Muppet, Occlusion, RayTracing, RayTracing/Diffuse,
RayTracing/NRD, RayTracing/Reflection, Rendering, Rendering/AsyncCompute,
Rendering/FrostedGlass, Rendering/Shadows, SnapToTerrainIk,
Streaming/Culling, Streaming/Culling/AutoHideDistanceNear,
Streaming/Culling/SecondaryRefPoint, vehicle
```

Grupos ausentes e de maior impacto: `Occlusion`, `Streaming/Culling`,
`Crowd`, `Animation`, `Lipsync`, `Vehicle`, `SnapToTerrainIk`, `DRS`, `DLSS`.

---

## 4. Bugs do ConfigOverhaul (por que não usar como base)

Decompilado com `ilspycmd` (`ConfigOverhaulCyberpunk/MainForm.cs`):

### 4.1 Toggle de RayTracing nunca é persistido
`Form1_FormClosed`, linhas 227-231:

```csharp
if (rEDEngineFeature.GroupName.Contains("RayTracing")) {
    redConfig.Sections.AddSection(rEDEngineFeature.GroupName);  // <- deveria ser val
    redConfig[rEDEngineFeature.GroupName].AddKey(val2);
}
```

Os blocos `Developer` e `Rendering` escrevem em `val` (o `IniData` que vai para
o disco). O bloco `RayTracing` escreve em `redConfig` — o objeto original em
memória. Resultado: **toggles de RayTracing são descartados ao salvar**.

### 4.2 A maioria dos grupos é inalcançável
`InitializeFeatureList` só cria seções para grupos cujo nome contenha
`"Developer"`, `"Rendering"` ou `"RayTracing"`. Grupos como `Occlusion`,
`Streaming`, `Crowd`, `Animation`, `Vehicle` e `Lipsync` **nunca são
gerenciados** — logo inalcançáveis pela interface.

### 4.3 "Reset" apaga o arquivo
`Form1_FormClosed` conta quantos toggles estão desligados (`num`) e, se
`num < 1` (nenhum desligado), executa `File.Delete(path)` — apaga o
`user.ini` inteiro.

### 4.4 Presets mal nomeados
- `VeryLow` desliga tudo exceto `DistantShadows` e `GlobalIllumination` —
  inclui `Hair`, `RainMap` e `Weather`.
- `Optimized` **habilita tudo** e só desliga 10 chaves específicas. É o preset
  *menos* agressivo dos três.

---

## 5. O jogo reescreve as settings ao sair

Comparação entre `UserSettings.json` (mtime 16:16) e o arquivo regravado pelo
jogo (mtime 20:29, após uma sessão):

```
FrameGeneration          FSR3  => Off
FSR3_FrameGeneration     true  => false
Vignette                 true  => false
ContactShadows           true  => false
HDDMode2                 Auto  => Off
LensFlares               true  => false
CascadedShadowsRange     Medium=> Low
CascadedShadowsResolution Medium=> Low
FacialTangentUpdates     true  => false
```

Conclusão prática: **aplicar settings com o jogo em execução é inútil** — ele
sobrescreve tudo ao encerrar. Daí o instalador recusar rodar com o jogo aberto.

---

## 6. Confirmação por varredura de strings do binário

Busca de bytes ASCII no `Cyberpunk2077.exe` (60 MB) — todos **presentes**:

| String | Significado |
|---|---|
| `Developer/FeatureToggles` | grupo de cvars lido em runtime |
| `Rendering/AsyncCompute` | grupo de cvars lido em runtime |
| `Rendering/FrostedGlass` | grupo de cvars lido em runtime |
| `Rendering/Shadows` | grupo de cvars lido em runtime |
| `user.ini` | arquivo de cvars |
| `options.json` | schema de settings |
| `UserSettings` | store de settings do menu |

Cvars de performance confirmados no binário (existem no código, logo são
lidos): `MultiFrustumUseGlobalOcclusion`, `VisibilityQuery`, `ZCutoffEnabled`,
`UseAnimSetupCaching`, `EnableMirrorScaling`, `SecondaryRefPoint`,
`AutoHideDistanceNear`, `EnableFacialSetupCustomization`, `stickyLookAt`,
`UseRealTimeForSubsteps`, `SkyRadianceExposureScaling`.

---

## 7. Bug no Native Settings UI 1.96

`nativeSettings/init.lua`, linhas 1394-1400:

```lua
if nativeSettings.currentPage == #nativeSettings.tabSizeCache then
    nativeSettings.nextButton.root:SetVisible(false)   -- sem nil-check
    if nativeSettings.previousButton then                -- com nil-check
        nativeSettings.previousButton.root:SetVisible(true)
    end
else
    nativeSettings.nextButton.root:SetVisible(true)      -- sem nil-check
```

O `previousButton` é verificado, o `nextButton` não. Erro em runtime
confirmado no log durante sessão real:

```
[20:12:34] [13048] init.lua:1395: attempt to index field 'nextButton' (a nil value)
    init.lua:1395: in function 'switchToNextPage'
    init.lua:245: in function <init.lua:240>
```

Impacto: navegar entre abas do menu de settings com o teclado (`next_menu`)
pode interromper o script.

---

## 8. Hardware de referência

| Componente | Valor |
|---|---|
| CPU | AMD Ryzen 5 5500U — 6 núcleos / 12 threads, 2,1 GHz |
| GPU | AMD Radeon Vega 8 (integrada, **VRAM compartilhada**) |
| RAM | 20 GB |
| Disco | E: com 23 GB livres |
| Instalação | 92,8 GB (inclui 12,94 GB de `basegame_5_video.archive`) |
| Mods | RED4ext + CET 1.37.1 + ArchiveXL + Codeware + TweakXL + ImmersiveThirdPerson + Native Settings UI |

Restrição dominante: **VRAM compartilhada**. Reduzir largura de banda de
textura (`Anisotropy`) e o número de passes de GPU (DRS, raymarch desligado)
tem mais efeito do que reduzir resolução fixa.

---

## 9. Análise nativa (Ghidra) — incompleta

Uma análise de engine do `Cyberpunk2077.exe` via Ghidra 12.1.4 headless
**não completou**. Contagem parcial obtida: **219.702 funções** e **72.145
strings** mapeadas.

Motivo: os analisadores `StackVariableAnalyzer` e `EntryPointAnalyzer` são
extremamente lentos em binários de 60 MB (o `Stack` sozinho consumiu ~43 min
em ~1,3 núcleo). Desativá-los exige `-preScript` (antes da auto-análise); com
`-postScript` eles executam antes de qualquer desativação.

Consequência: **nenhuma conclusão deste projeto depende dessa análise.** Tudo
veio de leitura direta de config, decompilação do ConfigOverhaul, log do CET
em runtime e varredura de strings do binário. A análise com xrefs (mapear a
função C++ que aplica cada cvar) continua sendo o próximo passo possível,
mas não é necessária para o perfil.
