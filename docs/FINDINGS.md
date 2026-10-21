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

### 1.1 O campo `values` — o schema define os valores aceitos

Opções do tipo `string_list`/`name_list` trazem um array **`values`** com a
lista exata de valores válidos, e `display_values` com as chaves de
localização correspondentes:

```json
{
    "name": "VolumetricCloudsQuality",
    "type": "string_list",
    "index": 1,
    "default_index": 1,
    "values":        [ "Off", "Medium", "High", "Ultra" ],
    "display_values":[ "UI-Settings-Video-QualitySetting-Off", ... ]
}
```

**Isto é crítico:** o jogo **reverte silenciosamente** qualquer valor fora
dessa lista ao carregar as settings. Nenhum erro, nenhum aviso — a opção
simplesmente volta ao valor anterior.

Descoberto empiricamente: após aplicar o perfil e jogar uma sessão, 5 opções
foram revertidas pelo jogo:

| Opção | Valor aplicado | Valor aceito | Valor final |
|---|---|---|---|
| `VolumetricCloudsQuality` | `Low` | `Off`, `Medium`, `High`, `Ultra` | `Off` |
| `GlobaIlluminationRange` | `Low` | `High`, `Ultra` | `High` |
| `ColorPrecision` | `Low` | `Medium`, `High` | `Medium` |
| `HDDMode` | `HDD` | `Off`, `On`, `Auto` | `Auto` |
| `EnableTelemetry` | `false` | (bool, mas revertida) | `true` |

`EnableTelemetry` é caso especial: além do valor, o jogo pode reativá-la por
política de conta. O instalador agora **valida o perfil contra o schema** e
falha alto quando encontra valor inválido, em vez de deixar o jogo reverter
em silêncio.

Nota: nem toda opção tem `values`. Opções `bool` e `int` não têm lista, e
`EnableTelemetry` (bool) foi revertida mesmo assim — o que sugere que a
reversão pode vir de outro caminho (normalização do jogo ao salvar).

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

---

## 10. Validação em runtime — sessão medida de 19 minutos

Uma sessão de jogo foi monitorada com amostragem de ~1 s (processo, GPU,
VRAM, I/O) e com rastreamento de eventos de filesystem, para obter o
comportamento real em vez de inferido.

### 10.1 A camada de menu sobrevive a uma sessão inteira

Das **37 opções** do perfil, **35** estavam exatamente como o perfil define
depois da sessão. Das **13 opções de lista** em que o perfil diverge do
default do jogo, **13 foram honradas** pelo motor:

| opção | default do jogo | perfil | valor no jogo |
|---|---|---|---|
| CrowdDensity | High | Low | Low |
| TextureQuality | High | Low | Low |
| ShadowMeshQuality | Medium | Low | Low |
| CascadedShadowsRange | Medium | Low | Low |
| VolumetricFogResolution | Medium | Low | Low |
| VolumetricCloudsQuality | Medium | Off | Off |
| MaxDynamicDecals | Medium | Low | Low |
| SubsurfaceScatteringQuality | Medium | Low | Low |
| LODPreset | Medium | Low | Low |
| HDDMode2 | Auto | Off | Off |
| MotionBlur | Low | Off | Off |
| HDDMode | Auto | On | On |

As outras 8 opções de lista do perfil já coincidem com o **piso** que o
esquema do jogo permite (`ColorPrecision`=Medium é o mínimo,
`GlobaIlluminationRange`=High é o mínimo, `Anisotropy`=1 é o mínimo).

Duas exceções, ambas reais e esperadas:

- **`EnableTelemetry: false` é inefetivo.** O jogo o reativa por política de
  conta. Nenhuma ferramenta externa vence isso.
- **`HDDMode`** — durante a medição o valor estava "Auto" porque foi alterado
  dentro do próprio jogo, não por falha de aplicação.

### 10.2 O modelo correto de "reversão silenciosa"

A §1.1 descrevia o jogo como "revertendo" valores inválidos. A evidência de
runtime mostra um mecanismo mais preciso:

1. **No load**, o jogo carrega o `UserSettings.json`, **valida** cada opção
   contra o esquema e mantém em memória o valor aceito.
2. **No exit**, o jogo **descarrega a memória** para o arquivo.

Consequência prática: um valor aplicado com o jogo **fechado** sobrevive
intacto; um valor alterado por ferramenta com o jogo **aberto** é apagado na
saída. Isso não é normalização periódica — é flush de estado.

> O jogo também escreve o `UserSettings.json` **durante** a sessão (observado
> às 23:42:40 numa sessão que durou até 23:48:51). Qualquer ferramenta
> externa que escreva nesse arquivo pode ser atropelada a qualquer momento.

### 10.3 O que o motor realmente lê

O log do CET expõe o hook `HookGameOptionInit(struct GameOption *)` e
imprime, para cada opção registrada, o path no formato `Grupo/Feature/Nome`.
São **13** settings ocultos — ou seja, 13 opções que o motor registra e que
portanto o `user.ini` controla de fato:

```
Developer/FeatureToggles/Antialiasing
Developer/FeatureToggles/ScreenSpaceReflection
Rendering/AsyncCompute/BuildDepthChain
Rendering/AsyncCompute/DPL
Rendering/AsyncCompute/DynamicTexture
Rendering/AsyncCompute/Enable
Rendering/AsyncCompute/ExposureScaleBuffer
Rendering/AsyncCompute/ExtendedDuringSSR
Rendering/AsyncCompute/FlattenNormals
Rendering/AsyncCompute/HairClears
Rendering/AsyncCompute/LutGeneration
Rendering/AsyncCompute/RaytraceASBuild
Rendering/AsyncCompute/SSAO
```

O perfil (antes desta versão) cobria 9. Os 4 ausentes —
`DPL`, `ExposureScaleBuffer`, `ExtendedDuringSSR` e `HairClears` — foram
adicionados: são passes de *async compute*, custo puro numa GPU integrada.

**Modelo de armazenamento:** o executável **não** guarda o path completo.
`"Developer/FeatureToggles/Antialiasing"` não existe como string; existem
`"Developer/FeatureToggles"` e `"Antialiasing"` como tokens separados, e o
motor compõe o path em runtime. Por isso a validação de chaves procura o
**token isolado** do nome da chave.

### 10.4 Chave de cvar que não existe: erro de digitação herdado

`EnableAirResistane` **não existe** nem como token ASCII, nem como UTF-16,
nem em qualquer arquivo do jogo. O nome real no binário é
`EnableAirResistance` (contexto: `base\gameplay\vehicles\curves\drive_helpers.curveset`).
O perfil usava a grafia errada, **herdada da `FeatureList` do
ConfigOverhaul** — que é uma lista transcrita de um app .NET decompilado e
nunca confrontada com o binário.

Corregido nesta versão, e o instalador agora **valida toda chave de cvar
contra o executável** antes de escrever.

> **Limitação honesta:** presença no binário prova que o *nome* existe, não
> que o motor o leia como cvar. `EnableAirResistance`, por exemplo, aparece
> na região de curvas de física do veículo — pode ser propriedade de curva
> em vez de cvar. Só a análise de xrefs (mapear a função C++ que registra
> cada `GameOption`) fecha essa questão.

### 10.5 O `user.ini` original do jogo traz uma chave inexistente

O `user.ini` que o próprio jogo entrega traz:

```ini
[Rendering/FrostedGlass]
;Eats 2-4 FPS
GlassUseMipChain = False
```

`GlassUseMipChain` **não existe no executável** (só `m_hasMipchain` e
`m_mipChain`). É a única das 57 chaves do perfil ausente do binário, e é
mantida no perfil com um aviso explícito do instalador: o motor pode estar
ignorando essa chave em silêncio desde sempre.

### 10.6 Comportamento de arquivos observado

| quando | arquivo | por quê |
|---|---|---|
| a cada ~2-3 min | `red4ext/plugins/Codeware/Persistent/ScriptableServiceContainer.dat` | estado persistente do Codeware |
| a cada ~1-2 min | `%LOCALAPPDATA%\...\cache\GamePipelineLibrary.cache` | cache de pipeline do jogo |
| ao abrir o overlay | `cyber_engine_tweaks/layout.ini` | layout do overlay do CET |
| ao trocar câmera | `mods/immersive_third_person/settings.lua` | preferências do mod de terceira pessoa |
| durante a sessão | `%LOCALAPPDATA%\...\UserSettings.json` | flush de settings |
| na saída | `UserSettings.json`, `persistent.json`, `bindings.json`, `menu_mod/settings.json` | flush final |
| na saída | `scripting.log`, `cyber_engine_tweaks.log`, `nativeSettings.log`, logs do RED4ext | logs bufferizados do CET |
| na saída | `Cyberpunk2077.exe-<stamp>-<pid>-<tid>.txt`, `REDEngineErrorReporter.exe-...txt` **removidos** | arquivos temporários do error reporter, criados no startup |

### 10.7 Medições (19 min de gameplay)

| métrica | p25 | mediana | p95 | max |
|---|---|---|---|---|
| GPU utilization % | 59 | **95** | 106 | 112 |
| CPU (de 12 núcleos) | 2,3 | 4,2 | 8,6 | 14,2 |
| VRAM dedicada MB | 398 | 407 | 419 | 424 |
| VRAM compartilhada MB | 2.303 | 2.358 | 2.504 | 2.609 |
| RAM privado MB | 7.139 | 7.205 | 7.397 | 7.481 |

**50,6%** das amostras com GPU ≥ 95%. CPU na mediana ocupa 4,2 de 12 núcleos.
O gargalo é a GPU integrada, confirmado com densidade de amostragem — não
mais com uma leitura única.

---

## 11. Bugs encontrados e corrigidos nesta versão

| # | bug | impacto | correção |
|---|---|---|---|
| 1 | Painel escrevia `user.ini` com **cinco** níveis `..` a partir do diretório do mod | a camada de cvars **nunca** foi aplicada pelo painel (3 falhas registradas em log) | corrigido para seis níveis |
| 2 | O painel escrevia o `UserSettings.json` mas **nunca** a camada de patches do CET | o painel aplicava 2 das 3 camadas | o painel agora aplica as três, lendo o perfil |
| 3 | O painel tinha **8 chaves a menos** que o perfil (`DRS_MaxResolution`, `VSync`, `MotionBlur`, `FilmGrain`, `ChromaticAberration`, `DepthOfField`, `LensFlares`, `Vignette`) | "fonte única de verdade" era falso | o painel lê `profile.json`; os valores vêm do perfil |
| 4 | O instalador escrevia `value` mas **nunca** `index` nas opções de lista | `value` e `index` podiam divergir (ex.: `HDDMode` value="On", index=2/"Auto") | o instalador reconcilia os dois campos |
| 5 | `EnableAirResistane` no perfil | cvar inerte (nome inexistente) | corrigido para `EnableAirResistance` |
| 6 | Nenhuma validação das chaves de cvar | erros de digitação passavam despercebidos | o instalador valida as 57 chaves contra o executável |
| 7 | `powershell -File Install-Profile.ps1` **não funcionava** | o comando que o README documenta quebrava: com `[CmdletBinding()]`, o PowerShell 5.1 não popula `$PSScriptRoot` nos defaults do `param` | default resolvido no corpo do script |
| 8 | `profile/profile.schema.json` referenciado e inexistente | referência `$schema` quebrada | schema criado e o perfil validado contra ele |
| 9 | Dry-run do instalador apagava arquivos | violava o contrato de "não escreve nada" | a limpeza só ocorre sem `-DryRun` |

---

## 12. Pendências conhecidas

- **A morte silenciosa do botão Apply ainda não tem causa identificada.** O
  log do painel para logo após a mensagem de falha do `user.ini`, sem linha
  seguinte e sem erro Lua no log do CET. A nova versão envolve o Apply em
  `pcall` e grava o erro em `performanceprofile-*.log.erros`; um mod de
  diagnóstico (`PerfProbe`) testa `os.getenv`, `io.open` absoluto,
  `json.decode` do arquivo real e os dois caminhos relativos. **Precisa de
  um boot do jogo para ser concluído.**
- **`value` vs `index`:** o instalador agora escreve os dois de forma
  coerente, mas qual dos dois o motor lê em runtime ainda não foi provado.
- **Os 25 toggles de HUD** do schema (`minimap`, `object_markers`,
  `npc_nameplates`, `quest_tracker`, …) não estão no perfil. São o maior
  ganho de GPU de risco zero que resta.
- **A análise de xrefs** (Ghidra) continua pendente: o script
  `ReaExtractSettings.java` está escrito e nunca executou, porque o projeto
  Ghidra persistente ficou com 0 bytes.

---

## 13. Validação da 0.1.1-alpha — causa-raiz do painel e FPS real

Uma sessão completa de jogo foi instrumentada com o perfil `0.1.1-alpha`
aplicado (4 cvars novos + `EnableAirResistance` + `value`/`index`
reconciliados) e com um mod de diagnóstico (`PerfProbe`) dentro do jogo.

### 13.1 O painel in-game não pode aplicar — e nunca pôde

O diagnóstico dentro do jogo devolveu, sem ambiguidade:

```
os.getenv e funcao                  -> nil
os = clock date difftime remove rename time      (sem getenv, sem execute)
_G                                    -> nil
io.open caminho com .. (1 nivel)     -> nil (bloqueado)
io.open ABSOLUTO jogo (engine/config) -> nil (bloqueado)
io.open ABSOLUTO temp do usuario     -> nil (bloqueado)
io.open CWD relativo                 -> gravou no CWD (CWD = diretorio do mod)
```

**O sandbox Lua do CET confina o `io` ao diretório do mod.** Três
consequências:

1. **`os.getenv` não existe.** `UserSettingsPath()` chamava
   `os.getenv("LOCALAPPDATA")` → *"attempt to call a nil value"* → o
   callback do botão Apply morria ali, **sem log e sem erro no log do
   CET**. É a causa-raiz da morte silenciosa registrada 3 vezes na §11.
2. **O caminho relativo com `..` também é bloqueado.** A correção de
   "cinco → seis níveis" da §11 é insuficiente: nenhum caminho relativo
   alcança fora do diretório do mod. O painel não tem como escrever o
   `user.ini` de nenhuma forma.
3. **`_G` é `nil`.** O ambiente do mod não expõe a tabela global, e
   `getfenv` também não está disponível — o inventário do ambiente só é
   possível por *sonda* de nomes conhecidos.

**Conclusão de arquitetura (com evidência, não opinião):** o painel in-game
não pode ser o aplicador. Ele pode (e deve) ser **coletor de intenção +
medidor**; a aplicação tem de acontecer fora do jogo. O instalador é o
aplicador; o painel pode gravar a intenção no próprio diretório para que
um aplicador externo a consuma.

### 13.2 FPS real: o alvo de 30 não é alcançado em gameplay

O `PerfProbe` conta frames por segundo de parede (fronteiras de `os.time`)
e CPU-segundos por segundo de parede (núcleos médios em uso).

| contexto | amostras | min | p25 | **mediana** | p75 | p95 | max |
|---|---|---|---|---|---|---|---|
| menu principal | 203 | 0,1 | 28 | **29** | 30 | 30 | 31 |
| **gameplay** | 1058 | 7 | 22 | **24** | 26 | 30 | 30 |

**91,4%** dos segundos de gameplay ficaram abaixo de 30 FPS. E a GPU, na
mesma janela:

| métrica | min | p25 | mediana | p75 | p95 | max |
|---|---|---|---|---|---|---|
| GPU utilization % | 0 | 94 | **100** | 103 | 107 | 125 |
| CPU (de 12 núcleos) | 0 | 3,2 | **5,1** | 6,0 | 9,0 | 15,1 |
| VRAM compartilhada MB | 0 | 2467 | **2529** | 2601 | 2654 | 2754 |
| RAM privado MB | 0 | 7472 | **7506** | 7566 | 7674 | 7781 |

**72,3%** das amostras com GPU ≥ 95%.

A leitura é direta: **a GPU está saturada em 100% e a CPU tem folga
(mediana 5,1 de 12 núcleos).** O limite é custo de fragmento. Chegar a 30
FPS exige reduzir carga de GPU, não de CPU — o que aponta para a mesma
direção dos toggles de HUD e dos passes de *async compute* que faltam.

> Nota: o `cpu_cores` do probe (mediana 1,0) mede o **tempo de CPU do
> processo**, que é ~1 núcleo cheio; a métrica de 5,1 núcleos vem do
> contador de processo do Windows, que inclui as threads do motor. As duas
> não são comparáveis diretamente — a segunda é a que interessa.

### 13.3 O que o motor de fato respeita

Das 37 opções do perfil, **34 ficaram intactas** após a sessão. Três são
inerentes, por motivos diferentes:

| opção | perfil | no jogo | por quê |
|---|---|---|---|
| `EnableTelemetry` | `false` | `true` | o jogo reativa por política de conta |
| `HDDMode` | `"On"` | `"Auto"` | **o motor computa o modo de disco** |
| `HDDMode2` | `"Off"` | `"On"` | idem |

`HDDMode` e `HDDMode2` são um achado novo: mesmo com `value` **e** `index`
escritos de forma coerente pelo instalador, o motor sobrescreve os dois.
O modo de disco é **derivado do armazenamento detectado**, não uma
preferência do usuário. (O `HDDMode` que você viu como "Auto" na sessão
anterior era o motor, não uma configuração.)

> Isso também refuta a hipótese de `value` vs `index`: para estas duas
> opções nenhum dos dois campos importa — o motor recalcula.

### 13.4 O jogo não reescreve o settings quando não há delta

Nesta sessão o `UserSettings.json` **não** aparece no diff de arquivos:
o jogo o escreveu uma única vez, às 00:30:14 (pouco depois de subir, ao
normalizar o estado em memória), e **não** ao encerrar — porque o estado
em memória era idêntico ao arquivo. Na sessão anterior, em que você mudou
opções dentro do jogo, o arquivo foi reescrito na saída.

Confirma o modelo da §10.2: **a escrita de saída é um flush de memória,
e só acontece quando há divergência.**

### 13.5 Efeito dos 4 cvars novos: ainda não isolado

Os 4 cvars (`DPL`, `ExposureScaleBuffer`, `ExtendedDuringSSR`, `HairClears`)
foram aplicados e confirmados no `user.ini` desta sessão, mas **o efeito
individual não foi isolado** — falta a contraprótese (rodar sem eles na
mesma rota). É o experimento A/B pendente, e o que falta é uma rota
jogável comparável, não instrumentação: a instrumentação está pronta.
