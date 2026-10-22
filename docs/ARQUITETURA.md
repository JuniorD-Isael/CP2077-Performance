# Arquitetura

Material de desenvolvimento: como o aplicativo funciona por dentro, de onde
vem cada valor do perfil, e o que foi medido. O [README](../README.md) é o
guia de uso; este arquivo é para quem quer entender ou mexer.

---

## 1. Derivação de caminho

O aplicativo recebe **um** dado do usuário: a pasta do jogo. Todo o resto é
derivado, e nenhuma derivação depende da loja.

| Alvo | Origem |
|---|---|
| Pasta do jogo | informada pelo usuário |
| `UserSettings.json` | `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\` |
| `user.ini` | `<jogo>\engine\config\platform\pc\user.ini` |
| `config.json` do CET | `<jogo>\bin\x64\plugins\cyber_engine_tweaks\config.json` |
| Esquema de opções do jogo | `<jogo>\r6\config\settings\options.json` e `...\platform\pc\options.json` |
| Executável (tokens de cvar) | `<jogo>\bin\x64\Cyberpunk2077.exe` |
| Dados da ferramenta | `%LOCALAPPDATA%\CP2077-Performance\` |

**O save não é da loja.** O diretório de save é definido pela CD PROJEKT RED
e não pelo launcher: a mesma instalação servida por Steam, Epic ou GOG lê e
grava no mesmo lugar. Por isso uma única build serve as três, e por isso o
aplicativo **não varre o disco** nem consulta registro de loja.

A pasta do jogo é conferida pela existência do executável e do esquema de
opções. Uma pasta inválida é recusada com mensagem, não com erro genérico.

---

## 2. Escrita cirúrgica

O objetivo é que o arquivo do jogo fique **byte a byte igual**, exceto pelos
campos que precisam mudar. Qualquer coisa que não é valor de configuração é
intocada — inclusive o que documenta efeitos colaterais.

### `UserSettings.json`

O arquivo guarda, para cada opção, o campo `value` e, nas opções de lista,
o campo `index` (a posição na lista `values`). O escritor:

1. localiza as ocorrências do campo pelo texto, sem desserializar o JSON;
2. calcula a nova posição do texto a partir do fim, para que os deslocamentos
   válidos não invalidem os achados seguintes;
3. substitui **apenas** `value` e, quando o valor muda, `index`.

`value` e `index` são reconciliados: se o valor desejado está na posição
certa, o índice é corrigido sozinho; se o índice já aponta para o item
certo, nada muda. A reconciliação é o que fecha o caso do `HDDMode`, cujo
`value="On"` coexistia com um `index` que apontava para "Auto".

### `user.ini` do motor

O arquivo do motor carrega comentários que documentam efeitos colaterais:

```ini
;Don't disable. (breaks lightmaps)
DistantShadows = True
```

Um parser de INI de uso geral jogaria fora essa documentação. O escritor
trabalha sobre o texto: encontra o cabeçalho de cada seção, respeita a ordem
das seções e das chaves, e só toca nas chaves visadas. Uma seção que ainda
não existe é criada no fim do arquivo; uma chave nova é inserida no fim da
sua seção.

A **remoção** de chaves também existe, e tem uma regra: o valor padrão do
jogo para uma cvar é a **ausência** da chave. Desligar uma variante remove as
chaves que ela tinha acrescentado — mas só quando o valor em vigor ainda é
exatamente o da variante. Se o usuário ajustou o valor à mão, a chave fica e a
divergência é reportada.

### `config.json` do CET

Patches são booleanos. Só os declarados são tocados; os outros ficam
intactos.

### Backup

Toda operação que altera algo cria uma versão em
`%LOCALAPPDATA%\CP2077-Performance\backups\`, antes de escrever. São
mantidas **as três mais recentes**: ao criar a quarta, a mais antiga é
descartada; a quinta descarta a segunda, e assim por diante.

---

## 3. O modelo `GameOption`

O esquema do jogo (`r6/config/settings/options.json`) é a fonte de verdade
das opções de menu. Cada opção declara:

| Campo | O que é |
|---|---|
| `name` | o identificador da opção |
| `type` | `bool`, `int`, `float`, `string_list` |
| `default_value` | o valor padrão **do jogo** — é o que a variante devolve ao ser desligada |
| `values` | a lista exata de valores aceitos, nas opções de lista |

**Valor fora da lista não surte efeito.** Sem erro, sem aviso. Por isso a
validação contra o esquema é bloqueante: o aplicativo recusa a aplicação em
vez de escrever um valor que o motor ignoraria em silêncio.

O modelo `GameOption` (o nome vem do `ConfigOverhaul`, de onde a estrutura
foi aprendida) tem `Grupo/Feature/Nome` como caminho completo. O Cyberpunk
2077 não usa esse caminho: as opções do menu são um conjunto chato no
`UserSettings.json`, e as features do motor vivem no `user.ini`, separadas
em seções que carregam o prefixo do grupo (`Rendering/`, `Developer/`).

As **cvars** são validadas de outra forma: contra as **strings do
executável**. A presença do nome no binário prova que o nome existe; não
prova que o motor o leia como cvar. A exceção conhecida é
`GlassUseMipChain`, que não existe no executável mas vem do `user.ini`
original do próprio jogo (`FINDINGS.md` §10.4).

---

## 4. Medições

Sessão instrumentada de 19 minutos, amostragem de ~1 s:

| Métrica | p25 | mediana | p95 | max |
|---|---|---|---|---|
| GPU utilization % | 94 | **95** | 106 | 112 |
| CPU (de 12 núcleos) | 3,7 | 4,2 | 8,6 | 15,1 |
| VRAM compartilhada MB | 2.467 | 2.528 | 2.654 | 2.754 |
| RAM do processo MB | 7.139 | 7.205 | 7.674 | 7.781 |

**72% das amostras com a GPU acima de 95%**, e a CPU com folga. A leitura é
direta: **o limite é custo de fragmento na GPU, não CPU.**

FPS real, medido por um mod de instrumentação dentro do jogo: **mediana 24 em
gameplay** (91% dos segundos abaixo de 30 FPS) contra 29 no menu.

**O FPS não é mensurável de fora nesta máquina.** `DwmGetCompositionTimingInfo`
falha (erro ≠ 0) mesmo com o `hwnd` do jogo; o PresentMon exige elevação de
privilégio. As saídas possíveis estão no `ROADMAP.md` §2.2.

---

## 5. Proveniência dos valores

### O que o perfil deliberadamente não faz

| Preservado | Motivo |
|---|---|
| `Hair = True` | requisito do jogador — usa mod de terceira pessoa |
| `DistantShadows = True` | desligar **quebra os lightmaps** (aviso do próprio motor) |
| `GlobalIllumination = True` | desligar causa sombras e iluminação ruins |
| Ray Tracing (todas) | incompatível com Vega 8 |
| Mods instalados | nenhum mod é removido ou desabilitado |
| **A interface do jogo (HUD)** | o perfil não altera o HUD; a manipulação de HUD vive em projeto próprio |

A seção `preserve` do perfil declara essas travas explicitamente, e a
validação recusa escrever por cima delas.

### O que o perfil faz, e por quê

| Efeito | Como |
|---|---|
| Reduz carga de fragmento (GPU) | nuvens volumétricas `Off`, GI e sombras no mínimo aceito, reflexos e decals `Low` |
| Elimina passes de async compute | 11 chaves de `Rendering/AsyncCompute` desligadas — passes assíncronos são custo puro em GPU integrada |
| Reduz custo de CPU | `NoSpatialAudio`, densidade de multidão `Low`, culling de streaming, cache de setup de animação, lip sync simplificado |
| Reduz largura de banda de textura (VRAM) | anisotropia `1`, `TextureQuality=Low`, `LODPreset=Low` |
| Adapta a resolução ao frame time | dynamic resolution scaling ligado, piso de 50% |
| Estabiliza o frame time | limite de 30 FPS, VSync desligado, Reflex e XeL desligados |
| Reduz I/O e ruído de fundo | saves em nuvem e telemetria desligados, modo de I/O em `On` |

### O que o motor recalcula

`HDDMode`, `HDDMode2` e `EnableTelemetry` são **recalculados pelo motor** ao
encerrar o jogo. O perfil os escreve, e o motor os sobrescreve: na prática
são inertes. Confirmar a lista completa depende do encerramento natural do
jogo (`ROADMAP.md` §2.5).

### Fronteira de escopo e mecanismo de variantes

O aplicativo cuida apenas do setup de performance e da tela cheia. Toda
manipulação de HUD foi movida para um projeto separado (`handoff/hud-toggle/`).
O mecanismo de variantes (`VariantResolver`) permanece disponível para futuras
variantes de performance nas duas camadas (`userSettings` e `userIni`), mas o
perfil curado não declara opções de HUD. Desligar uma variante devolve as
opções afetadas ao `default_value` do esquema e remove as chaves de cvar que
ela acrescentou.

---

## 6. Identidade visual Cyberpunk: Edgerunners

A interface segue uma estética inspirada na arte promocional de **Cyberpunk:
Edgerunners** — recortes poligonais chanfrados em 45° (`ChamferedPath`),
cabeçalho estilo HUD com feixe neon verde *Sandevistan*, barras laterais em
amarelo ácido e cantoneiras técnicas — construída **sobre os controles padrão do
WinForms**, sem nenhuma biblioteca externa.

### Por que sem biblioteca

O projeto não tem dependência externa nenhuma, e a publicação *self-contained*
é um único executável sem pacotes de terceiros. **MaterialSkin2 não tem versão
para .NET 10** e está sem manutenção desde 2022; o **Krypton Toolkit** é um
toolkit Fluent/Spark que precisaria ser inteiramente re-tematizado. Desenhar os
recortes chanfrados e o cabeçalho via GDI+ sobre `Panel` e `Button` nativos
mantém o binário enxuto e fiel ao visual do anime.

### As quatro cores-base Edgerunners

| Papel | Variante escura | Variante clara |
|---|---|---|
| Fundo (`Background`) | `#04101c` (azul-petróleo abissal de Night City) | `#f4f7fa` (branco-gelo) |
| Texto (`OnSurface`) | `#f4f7fa` | `#04101c` |
| Ação principal (`Primary`) | `#fcee0a` (amarelo ácido) | `#fcee0a` (amarelo ácido) |
| Rótulo sobre a principal (`OnPrimary`) | `#04101c` (contraste **16,57:1**) | `#04101c` (contraste **16,57:1**) |
| Destaque (`Accent`) | `#00ff66` (verde neon *Sandevistan*) | tom derivado de `#00ff66` ($\ge 4{,}5:1$) |

Nenhuma cor entra na interface sem vir dessas quatro cores-base: os tons de
superfície (`Surface`, `SurfaceRaised`) e o contorno (`Outline`) são misturas
entre o fundo e a principal (`#fcee0a`), e o papel de erro (`Error`) é uma
rotação de matiz de $-60^\circ$ da cor principal (vermelho-magenta de falha de
implante/cyberpsychosis).

O sistema escolhe a **variante** (clara ou escura); a **cor** vem da paleta
*Edgerunners* do aplicativo. A moldura nativa da janela continua seguindo o
sistema.

---

## 7. Estrutura do código

```
CP2077Performance.Core/     núcleo: sem dependência de interface
  Profile/                  modelo do perfil e resolução das variantes
  Schema/                   esquema do jogo e validação do perfil
  Writing/                  escritores cirúrgicos das três camadas
  Binary/                   tokens do executável, validação de cvar
  Backup/                   backup versionado com anel de três
  Display/                  modo de exibição em tela cheia (WindowMode = Fullscreen)
  Application/              plano, aplicação e registro
CP2077Performance.Ui/       interface WinForms enxuta, i18n, tema Edgerunners
  Theme/                    paleta Cyberpunk: Edgerunners e recortes chanfrados
CP2077Performance.Tests/    runner de verificações, sem pacote externo
```

O runner de verificações é próprio, para que o projeto construa e teste
offline: `dotnet run --project app\CP2077Performance.Tests`.

---

## 8. Referências

- [`FINDINGS.md`](FINDINGS.md) — pesquisa, evidências e bugs encontrados
- [`NATIVE-SETTINGS-API.md`](NATIVE-SETTINGS-API.md) — API de integração e
  armadilhas do CET
- [`ROADMAP.md`](ROADMAP.md) — o que já está resolvido e o que ainda não está
- [`relatorio-auditoria-add-desktop-app.md`](relatorio-auditoria-add-desktop-app.md)
  — auditoria de usabilidade do aplicativo

---

## 9. O modo de exibição: tela cheia preservando a resolução do usuário

### A receita

Quando o jogo roda em uma resolução menor que a nativa do monitor (por exemplo,
`1366x768` num monitor `1600x900` ou `1920x1080`), o modo `BorderlessWindowed`
deixa o jogo preso em uma janela menor que a tela.

Para resolver isso em **qualquer** computador e em **qualquer** monitor — sem
amarrar o aplicativo aos monitores de uma máquina específica e sem sobrescrever
a resolução que o jogador escolheu dentro do jogo —, aplicar o perfil grava
automaticamente:

| Opção | Valor aplicado | Índice | O que acontece com `Resolution` |
|---|---|---|---|
| `WindowMode` | `Fullscreen` | `2` | **Intocada** — qualquer resolução configurada no jogo (`1366x768`, `1280x720`, `1600x900`, `1920x1080`, etc.) é preservada |

O aplicativo **não** enumera monitores, **não** exibe seletor de monitor na
interface e **não** escreve `Resolution` no `UserSettings.json`. O modo
`Fullscreen` faz o jogo ocupar a tela inteira usando a resolução que o próprio
usuário escolheu no menu do jogo, enquanto a escala dinâmica de resolução
(`DynamicResolutionScaling`, `50%`–`85%`) continua ativa.

### O índice de `WindowMode`

`WindowMode` é uma opção `is_dynamic` no `UserSettings.json`. A medição em
runtime (`FINDINGS.md` §15.4) mostrou que a lista interna do motor é
`["Windowed", "BorderlessWindowed", "Fullscreen"]`, na qual `Fullscreen` ocupa
sempre o índice `2`. O escritor cirúrgico grava `value = "Fullscreen"` e
`index = 2` nas duas ocorrências de `/video/display/WindowMode` sem tocar em
nenhuma outra linha.

---

## 10. Referência técnica completa das três camadas

Esta seção consolida todas as configurações aplicadas pelo perfil
[`profile/lowspec-30fps.json`](../profile/lowspec-30fps.json) (anteriormente
exibidas nas abas do botão *Mostrar detalhe técnico* da interface).

### 10.1 Camada 1 — Opções de menu (`UserSettings.json`)

Arquivo: `%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077\UserSettings.json`

#### Opções aplicadas pelo perfil e pelo modo de exibição (38 opções)

| Opção | Valor aplicado | Origem | Finalidade |
|---|---|---|---|
| `WindowMode` | `"Fullscreen"` (`index = 2`) | `Display` | Força tela cheia mantendo a `Resolution` escolhida pelo usuário |
| `VolumetricCloudsQuality` | `"Off"` | `Profile` | Desliga nuvens volumétricas (alto custo de fragmento) |
| `GlobaIlluminationRange` | `"High"` | `Profile` | Mantém alcance de GI compatível com `GlobalIllumination = True` |
| `ColorPrecision` | `"Medium"` | `Profile` | Reduz banda de framebuffer sem artefatos visíveis |
| `AmbientOcclusion` | `"Low"` | `Profile` | Reduz custo do passe de oclusão de ambiente |
| `ScreenSpaceReflectionsQuality` | `"Low"` | `Profile` | Reduz custo de reflexos em espaço de tela |
| `ShadowMeshQuality` | `"Low"` | `Profile` | Simplifica malhas de sombra |
| `CascadedShadowsRange` | `"Low"` | `Profile` | Reduz alcance das cascatas de sombra |
| `CascadedShadowsResolution` | `"Low"` | `Profile` | Reduz resolução das cascatas de sombra |
| `DistantShadowsResolution` | `"Low"` | `Profile` | Mantém sombras distantes ativas na menor resolução |
| `VolumetricFogResolution` | `"Low"` | `Profile` | Reduz resolução do nevoeiro volumétrico |
| `MaxDynamicDecals` | `"Low"` | `Profile` | Limita decals dinâmicos simultâneos |
| `SubsurfaceScatteringQuality` | `"Low"` | `Profile` | Reduz custo de dispersão subsuperficial |
| `MirrorQuality` | `"Low"` | `Profile` | Reduz custo de renderização de espelhos |
| `TextureQuality` | `"Low"` | `Profile` | Reduz pressão sobre a VRAM compartilhada |
| `LODPreset` | `"Low"` | `Profile` | Reduz distância de nível de detalhe das malhas |
| `Anisotropy` | `1` | `Profile` | Desliga filtragem anisotrópica para economizar banda de textura |
| `CrowdDensity` | `"Low"` | `Profile` | Reduz custo de CPU e draw calls da multidão |
| `DynamicResolutionScaling` | `true` | `Profile` | Ativa escala dinâmica de resolução (DRS) |
| `DRS_TargetFPS` | `30` | `Profile` | Alvo de 30 FPS para o DRS |
| `DRS_MinResolution` | `50` | `Profile` | Piso de 50% da resolução de saída |
| `DRS_MaxResolution` | `85` | `Profile` | Teto de 85% da resolução de saída |
| `MaximumFPS_OnOff` | `true` | `Profile` | Ativa trava de FPS para estabilizar frame time |
| `MaximumFPS_Value` | `30` | `Profile` | Limita a taxa de quadros a 30 FPS |
| `NoSpatialAudio` | `true` | `Profile` | Desliga áudio espacial para aliviar a CPU |
| `EnableCloudSaves` | `false` | `Profile` | Evita I/O de sincronização de saves em nuvem |
| `EnableTelemetry` | `false` | `Profile` | Desliga telemetria em segundo plano |
| `Cinematic` | `false` | `Profile` | Desliga pós-processamento cinematográfico extra |
| `Overheads` | `false` | `Profile` | Desliga elementos sobrepostos desnecessários |
| `HDDMode` | `"On"` | `Profile` | Otimiza streaming de assets |
| `HDDMode2` | `"Off"` | `Profile` | Otimiza fila secundária de streaming |
| `VSync` | `"UI-Settings-Video-QualitySetting-Off"` | `Profile` | Desliga VSync para evitar queda abrupta para 15 FPS |
| `MotionBlur` | `"Off"` | `Profile` | Desliga desfoque de movimento |
| `FilmGrain` | `false` | `Profile` | Desliga granulação de filme |
| `ChromaticAberration` | `false` | `Profile` | Desliga aberração cromática |
| `DepthOfField` | `false` | `Profile` | Desliga profundidade de campo |
| `LensFlares` | `false` | `Profile` | Desliga reflexos de lente |
| `Vignette` | `false` | `Profile` | Desliga vinheta |

#### Travas preservadas no `UserSettings.json` (`preserve.userSettings`)

| Opção | Valor preservado | Motivo |
|---|---|---|
| `RayTracing` | `false` | Incompatível com GPU integrada (Vega) |
| `RayTracedReflections` | `false` | Incompatível com GPU integrada |
| `RayTracedSunShadows` | `false` | Incompatível com GPU integrada |
| `RayTracedLocalShadows` | `false` | Incompatível com GPU integrada |

---

### 10.2 Camada 2 — cvars do motor (`user.ini`)

Arquivo: `<jogo>\engine\config\platform\pc\user.ini`

#### Travas preservadas (`preserve.userIni`) — nunca desabilitadas

| Seção | Chave | Valor preservado | Comentário / Motivo |
|---|---|---|---|
| `Developer/FeatureToggles` | `DistantShadows` | `True` | `;Don't disable. (breaks lightmaps)` — desligar corrompe os lightmaps |
| `Developer/FeatureToggles` | `GlobalIllumination` | `True` | `;Don't disable. (causes bad shadows and lighting)` — desligar quebra iluminação global |
| `Developer/FeatureToggles` | `Hair` | `True` | Requisito do usuário — mantém o cabelo dos personagens ativo |

#### 60 cvars aplicadas pelo perfil (14 seções)

| Seção | Chave | Valor | Efeito no motor |
|---|---|---|---|
| `Developer/FeatureToggles` | `Antialiasing` | `False` | Desliga anti-aliasing do motor |
| `Developer/FeatureToggles` | `Bloom` | `False` | Desliga passe de bloom |
| `Developer/FeatureToggles` | `CharacterLightBlockers` | `False` | Desliga bloqueadores de luz por personagem |
| `Developer/FeatureToggles` | `CharacterRimEnhancement` | `False` | Desliga realce de borda de iluminação em personagens |
| `Developer/FeatureToggles` | `CharacterSubsurfaceScattering` | `False` | Desliga dispersão subsuperficial em personagens |
| `Developer/FeatureToggles` | `ChromaticAberration` | `False` | Desliga passe de aberração cromática |
| `Developer/FeatureToggles` | `ContactShadows` | `False` | Desliga sombras de contato em espaço de tela |
| `Developer/FeatureToggles` | `DepthOfField` | `False` | Desliga passe de profundidade de campo |
| `Developer/FeatureToggles` | `DistantFog` | `False` | Desliga nevoeiro distante |
| `Developer/FeatureToggles` | `DistantGI` | `False` | Desliga iluminação global distante complementar |
| `Developer/FeatureToggles` | `DistantVolFog` | `False` | Desliga nevoeiro volumétrico distante |
| `Developer/FeatureToggles` | `DynamicDecals` | `False` | Desliga decals dinâmicos |
| `Developer/FeatureToggles` | `FilmGrain` | `False` | Desliga passe de granulação de filme |
| `Developer/FeatureToggles` | `ImageBasedFlares` | `False` | Desliga flares baseados em imagem |
| `Developer/FeatureToggles` | `LocalShadows` | `False` | Desliga sombras locais dinâmicas extras |
| `Developer/FeatureToggles` | `MotionBlur` | `False` | Desliga passe de motion blur |
| `Developer/FeatureToggles` | `RainMap` | `False` | Desliga mapa de chuva |
| `Developer/FeatureToggles` | `RuntimeTangentUpdate` | `False` | Evita recálculo de tangentes em runtime |
| `Developer/FeatureToggles` | `ScreenSpaceHeatHaze` | `False` | Desliga distorção de calor em espaço de tela |
| `Developer/FeatureToggles` | `ScreenSpacePlanarReflection` | `False` | Desliga reflexos planares em espaço de tela |
| `Developer/FeatureToggles` | `ScreenSpaceRain` | `False` | Desliga reflexos de chuva em espaço de tela |
| `Developer/FeatureToggles` | `ScreenSpaceReflection` | `False` | Desliga SSR no motor |
| `Developer/FeatureToggles` | `ScreenSpaceUnderwater` | `False` | Desliga pós-processamento subaquático em espaço de tela |
| `Developer/FeatureToggles` | `SSAO` | `False` | Desliga oclusão de ambiente em espaço de tela |
| `Developer/FeatureToggles` | `VolumetricClouds` | `False` | Desliga nuvens volumétricas no motor |
| `Developer/FeatureToggles` | `VolumetricFog` | `False` | Desliga nevoeiro volumétrico no motor |
| `Developer/FeatureToggles` | `Weather` | `False` | Simplifica passes de clima |
| `Rendering/AsyncCompute` | `BuildDepthChain` | `False` | Desliga construção assíncrona da cadeia de profundidade |
| `Rendering/AsyncCompute` | `DPL` | `False` | Desliga passe assíncrono de Dynamic Point Lights |
| `Rendering/AsyncCompute` | `DynamicTexture` | `False` | Desliga geração assíncrona de texturas dinâmicas |
| `Rendering/AsyncCompute` | `Enable` | `False` | Desliga fila mestre de Async Compute (custo puro em iGPU) |
| `Rendering/AsyncCompute` | `ExposureScaleBuffer` | `False` | Desliga cálculo assíncrono de escala de exposição |
| `Rendering/AsyncCompute` | `ExtendedDuringSSR` | `False` | Desliga async compute estendido durante SSR |
| `Rendering/AsyncCompute` | `FlattenNormals` | `False` | Desliga achatamento assíncrono de normais |
| `Rendering/AsyncCompute` | `HairClears` | `False` | Desliga limpeza assíncrona de buffers de cabelo |
| `Rendering/AsyncCompute` | `LutGeneration` | `False` | Desliga geração assíncrona de LUT |
| `Rendering/AsyncCompute` | `RaytraceASBuild` | `False` | Desliga construção assíncrona de estruturas de aceleração RT |
| `Rendering/AsyncCompute` | `SSAO` | `False` | Desliga SSAO em async compute |
| `Rendering/FrostedGlass` | `GlassUseMipChain` | `False` | Desliga cadeia de mips para vidro fosco (exceção documentada em `FINDINGS.md` §10.4) |
| `Rendering` | `RainMapProxySorting` | `False` | Desliga ordenação de proxies do mapa de chuva |
| `Rendering` | `UseExperimentalVolFog` | `False` | Desliga nevoeiro volumétrico experimental |
| `Rendering` | `UseSkinningLOD` | `False` | Evita sobrecarga de transição de skinning LOD |
| `Rendering/Shadows` | `CascadeFitToWorstCase` | `False` | Evita dimensionamento de cascata pelo pior caso |
| `Rendering/Shadows` | `CascadeUseBackfacesAsCullingPlanes` | `False` | Simplifica planos de culling de sombras em cascata |
| `Occlusion` | `MultiFrustumUseGlobalOcclusion` | `False` | Desliga oclusão global multi-frustum |
| `Streaming/Culling` | `VisibilityQuery` | `False` | Simplifica consultas de visibilidade do streaming |
| `Streaming/Culling/AutoHideDistanceNear` | `Enabled` | `False` | Desliga cálculo extra de auto-hide próximo |
| `Streaming/Culling/SecondaryRefPoint` | `Enabled` | `False` | Desliga ponto de referência secundário de streaming |
| `Crowd` | `ZCutoffEnabled` | `False` | Desliga teste extra de corte Z na multidão |
| `Animation` | `UseAnimSetupCaching` | `False` | Evita retenção extra de memória de setup de animação |
| `Animation` | `EnableRefreshLocoWrappersOnUnlock` | `False` | Evita atualização extra de wrappers de locomoção |
| `Lipsync` | `SyncToAudio` | `False` | Simplifica sincronização labial com áudio |
| `Lipsync` | `UseAudioAdjustment` | `False` | Desliga ajuste dinâmico de áudio no lipsync |
| `Editor/Characters/Hair` | `UseGlobalContactShadowsOnHair` | `False` | Desliga sombras de contato globais no cabelo (preservando o cabelo) |
| `Editor/Characters/Hair` | `UseLocalContactShadowsOnHair` | `False` | Desliga sombras de contato locais no cabelo |
| `Vehicle` | `EnableAirResistance` | `False` | Simplifica cálculo de resistência do ar em veículos |
| `Vehicle` | `PhysXClampHugeImpacts` | `False` | Desliga passo extra de clamp de impactos PhysX |
| `Vehicle` | `PhysXClampHugeSpeeds` | `False` | Desliga passo extra de clamp de velocidades PhysX |
| `SnapToTerrainIk` | `Enabled` | `False` | Desliga cinemática inversa de ajuste ao terreno |
| `SnapToTerrainIk` | `FootLockEnabled` | `False` | Desliga trava de pé na cinemática inversa de terreno |

---

### 10.3 Camada 3 — Patches do motor (`config.json` do Cyber Engine Tweaks)

Arquivo: `<jogo>\bin\x64\plugins\cyber_engine_tweaks\config.json` (opcional quando o CET não está instalado)

| Patch | Valor | Finalidade |
|---|---|---|
| `disable_antialiasing` | `true` | Desliga o anti-aliasing forçado pelo motor |
| `disable_async_compute` | `true` | Desliga o uso global de filas de Async Compute no DirectX 12 |
| `disable_vignette` | `true` | Remove o efeito de vinheta pós-processado |
| `disable_win7_vsync` | `true` | Desliga o caminho legado de VSync |
