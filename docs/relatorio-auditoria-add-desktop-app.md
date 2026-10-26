# Relatório de Auditoria — Change `add-desktop-app`

Data da auditoria: 2026-10-21

---

## Sumário Executivo

| Indicador | Resultado |
|---|---|
| Tasks concluídas | **47 / 47** (100%) |
| Artefatos antigos removidos | `installer/` e `mod/PerformanceProfile/` confirmados ausentes no disco |
| Build funcional | `build.cmd` + artefato em `dist/` |
| Specs cobertos | 4 / 4 |
| Testes existem | `app/CP2077Performance.Tests/` |

---

## Parte 1 — O que foi aplicado corretamente (OK)

### 1. Estrutura e Build (Tasks 1.1–1.4)

| Task | Verificação |
|---|---|
| 1.1 Três projetos | `Core.csproj` (sem WinForms), `Ui.csproj` (`UseWindowsForms=true`), `App.csproj` (`WinExe`). Core **não** referencia assemblies de interface. |
| 1.2 `build.cmd` | Dois alvos: `self-contained` (padrão, `PublishSingleFile`) e `framework-dependent`. |
| 1.3 Perfil embarcado | `App.csproj`: `EmbeddedResource` com `lowspec-30fps.json`. `UiBootstrap.cs` lê e extrai na primeira execução. |
| 1.4 `.gitignore` | `dist/`, `app/**/bin/`, `app/**/obj/` ignorados. `backups/` listado. |

### 2. Localização e Validação da Instalação (Tasks 2.1–2.5)

| Task | Verificação |
|---|---|
| 2.1 Resolvedor de alvos | `GameLocator.cs`: três caminhos derivados, save não depende da loja (derivado de `Environment.SpecialFolder.LocalApplicationData`). |
| 2.2 Validação do executável | `InstallationValidator.cs`: exige `bin\x64\Cyberpunk2077.exe`. CET como camada opcional — ausência não bloqueia. |
| 2.3 Guarda de jogo em execução | `GameRunningGuard.cs`: detecta processo `Cyberpunk2077`, dupla checagem imediatamente antes de escrever (`CanProceed`). Mensagem explica o motivo (o jogo descarrega a memória ao encerrar). |
| 2.4 Painel remanescente | `LeftoverPanelDetector.cs`: detecta diretório do painel antigo, avisa que ele não aplica nada, oferece remoção. |
| 2.5 Documentação | FINDINGS.md referencia as regras de derivação de caminho. |

### 3. Schema e Validação de Valores (Tasks 3.1–3.5)

| Task | Verificação |
|---|---|
| 3.1 Leitor do schema | `GameSchema.cs` + `GameOptionSchema.cs`: lê `options.json` e `platform/pc/options.json`, expõe `AcceptedValues` e `DefaultValueText`. |
| 3.2 Validação contra schema | `ProfileValidator.cs`: valor fora da lista → erro com lista de aceitos; opção inexistente → erro. |
| 3.3 Tokens do executável | `BinaryTokenScanner.cs`: extração por byte nulo, range imprimível (0x20–0x7E). |
| 3.4 Exceções conhecidas | `CvarKeyValidator.cs`: `GlassUseMipChain` → warning com referência a `docs/FINDINGS.md §10.4`; chave inventada → erro. |
| 3.5 Documentação | FINDINGS.md §10.3 e §10.4 documentam o modelo de armazenamento de tokens e a limitação da validação. |

### 4. Escrita das Três Camadas (Tasks 4.1–4.7)

| Task | Verificação |
|---|---|
| 4.1 Localizador de campos | `JsonFieldLocator.cs`: regex sobre texto, `FieldSpan(Start, Length)`, sem reescrever o documento. |
| 4.2 Escrita cirúrgica | `MenuOptionsWriter.cs`: edits aplicados de trás para frente (`OrderByDescending(e => e.Start)`), sem round-trip de serialização. |
| 4.3 Reconciliação value/index | `MenuOptionsWriter.cs`: `acceptedIndex` calculado pela posição na lista do schema e escrito junto com o valor. Caso de valor correto com índice desatualizado coberto. |
| 4.4 INI por texto | `EngineIniWriter.cs`: preserva comentários (pula linhas que começam com `;` ou `#`), ordem de seções, seções novas ao final. |
| 4.5 Escrita CET | `CetConfigWriter.cs`: ausência do arquivo tratada como camada indisponível. |
| 4.6 Verificação pós-escrita | `WriteVerifier.cs`: `VerifyMenu` e `VerifyIni` relêem os arquivos e comparam o resultado com o esperado. Divergência reportada como falha. |
| 4.7 Documentação | Referenciada no design.md (D3) e no README (§2). |

### 5. Perfil, Variantes e Simulação (Tasks 5.1–5.7)

| Task | Verificação |
|---|---|
| 5.1 Schema com `variants` | `profile.schema.json` linha 99: propriedade `variants` aceita, com subestrutura `label`, `userSettings.values`. |
| 5.2 Modelo de perfil | `ProfileModel.cs`: `Variants`, `UserSettingsValues`, `UserIni`, `CetPatches`. Edição via `SetMenuValue`, `SetVariantValue`. Round-trip que preserva campos desconhecidos do JSON. |
| 5.3 Variante ligada | `VariantResolver.cs`: valores da variante aplicados por cima do perfil base (`values[option] = value`). |
| 5.4 Variante desligada | `VariantResolver.cs`: opções afetadas voltam ao `default_value` do schema do jogo via `ProfileValue.FromJson(optionSchema.DefaultValueText)`. |
| 5.5 Simulação | `ProfileApplier.Plan()`: retorna `ApplyPlan` sem escrever. Exposição via botão "Simular" na UI. |
| 5.6 Registro de alterações | `ChangeLog.cs`: `Append` com timestamp, arquivos alterados, detalhes com origem de cada valor. |
| 5.7 Variante de HUD | Perfil `lowspec-30fps.json`: variante `hudMinimal` com 22 toggles de HUD + 3 presets reduzidos do jogo (`FluffReduction`, `PostProcessingReduction`, `Sieja`). |

### 6. Backup e Restauração (Tasks 6.1–6.5)

| Task | Verificação |
|---|---|
| 6.1 Backup automático | `BackupService.Create()`: só quando há arquivos existentes. Manifest JSON registra `original` e `stored`. |
| 6.2 Conjunto por backup | Manifest registra todos os arquivos que a operação toca. |
| 6.3 Anel de 3 | `Prune()`: `Skip(RetainedVersions)` com `RetainedVersions = 3`. |
| 6.4 Restauração | `Restore()`: recusa quando `MissingFiles.Count > 0`; informa o arquivo ausente. |
| 6.5 Local dos backups | `GameLocator.BackupRoot`: `%LOCALAPPDATA%\CP2077-Performance\backups\`. Fora do jogo e fora do repositório. |

### 7. Interface (Tasks 7.1–7.7)

| Task | Verificação |
|---|---|
| 7.1 Tema | `ThemeHelper.cs`: lê `AppsUseLightTheme` do registry, aplica via `Application.SetColorMode`. `ApplicationHighDpiMode` = `PerMonitorV2` no csproj. |
| 7.2 Internacionalização | `Strings.resx` (EN, neutro) + `Strings.pt-BR.resx` (satellite). 60 chaves, 100% traduzidas nas duas línguas. `ResourceManager` com fallback para inglês. |
| 7.3 Janela principal | `MainForm.cs`: campo de pasta com `FolderBrowserDialog`, validação, origem dos alvos exibida por camada. |
| 7.4 Aba de cvars | `FillEngineGrid()`: seção, chave, valor "atual → novo", descrição do comentário do jogo. Marca "será acrescentada" para chaves novas. |
| 7.5 Aba de menu | `FillMenuGrid()`: desejado vs em vigor lado a lado, divergência em `Color.DarkOrange`. |
| 7.6 Ações | 4 botões: Aplicar, Simular, Restaurar, Descartar. Bloqueadas com motivo quando: sem pasta, pasta inválida, jogo aberto. |
| 7.7 Registro + aviso | `FillLogGrid()` com timestamp/arquivos/alterações. Label `TokenWarning` = "A presença do nome no executável não prova que o motor o leia como cvar". |

### 8. Remoção e Documentação (Tasks 8.1–8.4)

| Task | Verificação |
|---|---|
| 8.1 Remoção | `installer/` e `mod/PerformanceProfile/` **não existem no disco**. |
| 8.2 README reescrito | Seção de uso aponta para o `.exe`. Sem referência ao instalador PowerShell como caminho ativo. Seção §5 explica as remoções com quebra. |
| 8.3 CHANGELOG | Entradas `0.1.0` e `0.1.1-alpha` existentes. Remoções documentadas. |
| 8.4 FINDINGS.md | §13.1 documenta a causa-raiz do sandbox do CET. Referenciada pelo README. |

### 9. Verificação de Integração (Tasks 9.1–9.3)

Marcadas `[x]`. São tasks de verificação manual contra a instalação real do jogo. Não auditáveis por inspeção de código — dependem da execução do aplicativo e do jogo.

---

## Parte 2 — Itens incompletos ou com ressalvas

### ⚠️ CHANGELOG sem entrada consolidada da versão do app

O CHANGELOG tem `0.1.0` e `0.1.1-alpha` (versões do perfil/instalador), mas não tem uma entrada dedicada à versão que introduz o aplicativo desktop e remove o instalador/painel como versão nova (ex.: `0.2.0` ou `1.0.0`). O conteúdo está parcialmente descrito na seção §5 do README ("O que mudou nesta versão"), mas o CHANGELOG como artefato separado não registra essa transição com o tratamento de breaking change que o SemVer requer.

### ⚠️ Tasks de integração não verificáveis

As tasks 9.1–9.3 (aplicar contra a instalação real, subir o jogo, capturar tema/idioma) são por natureza manuais. Estão marcadas como concluídas e confio na marcação, mas registro que não há evidência automatizada.

---

## Parte 3 — Cobertura Spec vs Implementação

| Spec | Requirements | Scenarios | Cobertura no código |
|---|---|---|---|
| `game-installation` | 6 | 12 | `GameLocator`, `InstallationValidator`, `GameRunningGuard`, `LeftoverPanelDetector` — todos os 6 requisitos cobertos. |
| `profile-application` | 9 | 19 | `ProfileValidator`, `CvarKeyValidator`, `MenuOptionsWriter`, `EngineIniWriter`, `CetConfigWriter`, `WriteVerifier`, `VariantResolver`, `ProfileApplier`, `ProfileStore` — todos os 9 requisitos cobertos. |
| `config-backup` | 5 | 10 | `BackupService` — todos os 5 requisitos cobertos (criação automática, conjunto por backup, anel de 3, restauração, local fora do jogo). |
| `desktop-interface` | 5 | 9 | `ThemeHelper`, `Strings` (i18n), `MainForm` (cvars com descrição, estado efetivo, operações acessíveis) — todos os 5 requisitos cobertos. |

---

## Parte 4 — IHC (Interação Humano-Computador) para o Usuário Final

### O que a IHC acerta

| Aspecto | Avaliação | Detalhe |
|---|---|---|
| Acessibilidade linguística | ✅ Excelente | 60 strings traduzidas para pt-BR. Inglês como fallback seguro. Nenhuma chave vazia em locale desconhecido. |
| Feedback de erro | ✅ Bom | Pasta inválida, jogo aberto, valor inválido no schema — todos com mensagem específica. Botões desabilitados mostram o motivo entre parênteses. |
| Reversibilidade | ✅ Excelente | Simular antes de aplicar. Backup automático antes de toda alteração. Restaurar qualquer das 3 versões mantidas. Anel de 3 evita acúmulo. |
| Transparência | ✅ Excelente | Aba "Menu": desejado vs em vigor com divergência em laranja. Aba "Motor": descrição do comentário do próprio jogo. Nota sobre limitação do token na aba "Perfil". Origem de cada valor (perfil / variante / padrão do jogo). |
| Descoberta do caminho | ✅ Bom | Botão "Procurar..." abre `FolderBrowserDialog`. Caminho persistido entre execuções e revalidado ao abrir. |
| Proteção contra dano | ✅ Excelente | Jogo em execução → botões de aplicar/simular desabilitados com motivo. Verificação pós-escrita (relê e compara). Backup criado apenas quando há alteração (anel não gasto com cópias idênticas). |
| Tema e escala | ✅ Bom | Dark mode automático via registry. DPI `PerMonitorV2`. |

### O que a IHC precisa melhorar

#### 🟡 1. Ausência de tela de onboarding / primeira execução

Na primeira execução, o usuário vê um campo vazio "Pasta do jogo" e cinco abas com grids vazios. Não há nenhuma explicação do que o aplicativo faz, nem orientação de próximo passo.

**Impacto:** Abandono imediato de usuários iniciantes — público significativo no Nexus Mods.

**Recomendação:** Mostrar, quando nenhuma pasta foi informada, um texto orientador com instruções de como encontrar a pasta do jogo por loja (Steam: Biblioteca → botão direito → Gerenciar → Navegar em arquivos locais; GOG: Galaxy → Gerenciar instalação → Mostrar pasta).

#### 🟡 2. Mensagem de jogo em execução não ajuda com processo preso

A mensagem "O jogo está em execução. Feche-o antes de aplicar." é clara, mas não orienta o usuário cujo processo está preso no gerenciador de tarefas. Usuários de Nexus Mods frequentemente reportam processos de jogo que não encerram corretamente.

**Recomendação:** Adicionar à mensagem: "Se já fechou o jogo e ainda vê esta mensagem, abra o Gerenciador de Tarefas (Ctrl+Shift+Esc) e encerre o processo Cyberpunk2077.exe."

#### 🟡 3. Área de resultado (`_resultBox`) sem destaque visual

As mensagens de resultado são exibidas em uma `TextBox` readonly na parte inferior, ao lado dos botões, sem distinção visual entre sucesso, falha parcial e erro. Para o fluxo onde "Aplicar" é a ação principal, o resultado deveria ter mais destaque.

**Recomendação:** Cor de fundo diferente para sucesso (verde suave) e erro (vermelho suave), ou um painel de resultado dedicado com mais espaço vertical.

#### 🟡 4. Divergência na aba "Menu" sem ação contextual

A aba mostra divergências em laranja, mas o usuário precisa voltar aos botões para agir. Não há tooltip explicando que "em vigor" é o que o jogo usa agora e "desejado" é o que o perfil quer.

**Recomendação:** Tooltip nas colunas ou um texto introdutório na aba explicando: "Se 'Em vigor' difere de 'Desejado', clique Aplicar para corrigir."

#### 🟡 5. Variante de HUD sem preview do efeito

A `CheckedListBox` mostra "HUD minima" sem nenhuma imagem ou descrição expandida. Os nomes dos toggles (`minimap`, `npc_nameplates`, `FluffReduction`) são termos técnicos.

**Recomendação:** Tooltip ou descrição expandida por item, ou ao menos exibir a `description` da variante do perfil ("Reduz a interface do jogo: minimapa, marcadores, nomes...").

#### 🟢 6. Itens menores

- O botão "Descartar edições" não pede confirmação — pode surpreender o usuário.
- Não há barra de progresso ou spinner durante a varredura do executável (~60 MB), que pode levar alguns segundos.
- A aba "Registro" começa vazia e não explica que aparecerá algo após a primeira aplicação.
- O README não inclui screenshot do aplicativo — esperado no Nexus Mods.
- Nenhuma menção a compatibilidade com Vortex ou Mod Organizer 2 (gerenciadores de mods populares).

---

## Parte 5 — Resumo de Decisões de Design Confirmadas no Código

| Decisão | Proposta em `design.md` | Confirmada no código |
|---|---|---|
| D1 — .NET 10 WinForms | `net10.0-windows`, self-contained | ✅ Todos os csproj são `net10.0-windows`. `build.cmd` publica self-contained. |
| D2 — Núcleo sem WinForms | Core sem referência a interface | ✅ `Core.csproj` não tem `UseWindowsForms` nem referência ao Ui. |
| D3 — Leitura com System.Text.Json, escrita cirúrgica | Localizador sobre texto bruto | ✅ `JsonFieldLocator.cs` + `MenuOptionsWriter.cs`. |
| D4 — Validação por token do executável | Extração de tokens ASCII | ✅ `BinaryTokenScanner.cs` + `CvarKeyValidator.cs`. |
| D5 — Variante como seção do perfil | `variants` no JSON, restauração pelo `default_value` | ✅ `ProfileModel.Variants` + `VariantResolver.Resolve`. |
| D6 — Perfil curado embarcado + perfil do usuário | Recurso no exe, perfil do usuário em `%LOCALAPPDATA%` | ✅ `EmbeddedResource` no csproj + `ProfileStore.cs`. |
| D7 — Backups em `%LOCALAPPDATA%`, anel de 3 | Backup só quando altera, `RetainedVersions = 3` | ✅ `BackupService.cs`. |
| D8 — Idioma por recursos satellite | Neutro em inglês, satellite pt-BR | ✅ `Strings.resx` + `Strings.pt-BR.resx` + `SatelliteResourceLanguages` no csproj. |
| D9 — Detecção de painel remanescente | Detecta e oferece remover | ✅ `LeftoverPanelDetector.cs`. |
| D10 — `user.ini` por manipulação de texto | Preserva comentários e ordem | ✅ `EngineIniWriter.cs`. |

---

## Parte 6 — Próximos Passos Recomendados

### Antes de publicar no Nexus Mods

1. **Onboarding na primeira execução** — texto orientador quando nenhuma pasta foi informada, com instruções por loja (Steam/GOG/Epic).
2. **Entrada consolidada no CHANGELOG** — versão que marca a transição para o app desktop, com as remoções como breaking change.
3. **Screenshot do aplicativo** — essencial para a página do Nexus Mods.
4. **Nota sobre Vortex/MO2** — informar que o mod é um executável standalone, não um arquivo a ser gerenciado por mod manager.

### Melhorias de IHC a considerar

5. Destaque visual na área de resultado (sucesso vs erro).
6. Confirmação antes de "Descartar edições".
7. Spinner ou indicação de progresso durante a varredura do executável.
8. Descrição expandida da variante de HUD.
9. Mensagem de jogo preso no gerenciador de tarefas.

---

*Relatório gerado a partir de auditoria completa do código-fonte, artefatos OpenSpec, documentação e fluxo de IHC.*
