# Integração com Native Settings UI — API e armadilhas

Este documento registra o que foi aprendido **em runtime**, testando contra o
Native Settings UI v1.96 instalado no jogo.

---

## A API correta

A integração **não** usa `require`. O CET carrega cada mod em um ambiente Lua
isolado (`setfenv`), então um mod não enxerga o `return` de outro mod.

A forma oficial (confirmada no repositório `CP77_nativeSettings`, arquivo
`addSettingsExample/init.lua`) é:

```lua
registerForEvent("onInit", function()
    local nativeSettings = GetMod("nativeSettings") -- referência ao mod

    if not nativeSettings then
        print("Error: NativeSettings not found!")
        return
    end

    nativeSettings.addTab("/myMod", "My mod")
    nativeSettings.addSubcategory("/myMod/sub", "A subcategory")

    nativeSettings.addSwitch("/myMod/sub", "Label", "Desc",
        currentValue, defaultValue, function(state) ... end)

    nativeSettings.addRangeInt("/myMod", "Label", "Desc",
        min, max, step, currentValue, defaultValue, function(value) ... end)

    nativeSettings.addRangeFloat("/myMod", "Label", "Desc",
        min, max, step, "%.2f", currentValue, defaultValue, function(value) ... end)

    nativeSettings.addSelectorString("/myMod/sub", "Label", "Desc",
        { [1]="A", [2]="B" }, selectedIndex, defaultIndex, function(value) ... end)

    nativeSettings.addButton("/myMod/sub", "Label", "Desc", "BtnLabel", 45, function() ... end)
end)
```

### Assinatura dos callbacks (importante)

O callback recebe **o valor direto**, não `(self, value)`:

```lua
-- ERRADO
ns.addSwitch(path, label, desc, cur, def, function(_, state) ... end)

-- CERTO
ns.addSwitch(path, label, desc, cur, def, function(state) ... end)
```

Para `addSelectorString`, o callback recebe o **índice** (número), não a string.
É preciso mapear: `function(value) SetValue(key, list[value]) end`.

### API completa disponível

```
addTab, addSubcategory, removeSubcategory,
addSwitch, addRangeInt, addRangeFloat, addSelectorString,
addButton, addKeyBinding, addCustom,
setOption, pathExists, removeOption, refresh,
registerRestoreDefaultsCallback
```

---

## Onde as abas aparecem

As abas de mods **não** aparecem nas Configurações normais. O NativeSettings
detecta a origem pelo label do item de menu:

```lua
nativeSettings.fromMods = target:GetData().label == "Mods"
```

Ou seja: as abas aparecem quando se entra em **Configurações a partir do item
"MODS"** do menu principal. O menu de mods em si não tem configurações.

Se nenhum mod registrar nada, o NativeSettings cria a aba
`"No mods using NativeSettings installed!"` — útil como sinal de diagnóstico:
se essa aba aparecer, nenhum mod está usando a API.

---

## Armadilhas do ambiente Lua do CET

### 1. `require` não atravessa mods

```lua
require("nativeSettings")            --> retorna nil, sem erro
require("../nativeSettings/init")     --> retorna nil, sem erro
```

O `require` do CET resolve apenas módulos **do próprio mod** (é assim que
`nativeSettings` carrega `Cron.lua` e `UIButton.lua`). O `LUA_PATH` efetivo é
`;.;;` — apenas o diretório atual.

Para acessar outro mod, use `GetMod("nomeDoMod")`.

### 2. `debug` pode ser `nil`

O `immersive_third_person` usa `debug and debug.getinfo` com guarda — o mesmo
guarda é necessário em qualquer mod:

```lua
local okDir, info = pcall(function()
    if not debug or not debug.getinfo then return nil end
    return debug.getinfo(1, "S").source
end)
```

Sem o guarda, `attempt to index global 'debug' (a nil value)` aborta o mod.

### 3. Paths com `..` são bloqueados

O CET registra `Tried to access invalid path '..'!` e lança `C++ exception`.
Portanto `dofile("../outro_mod/init.lua")` **não** funciona.

### 4. `dofile` re-executa o init.lua

Mesmo quando o path é encontrado, `dofile` re-executa o `init.lua` do outro
mod, o que falha se ele usa `require` internamente. Não é um caminho válido
para integrar.

### 5. O CWD é o diretório do próprio mod

Verificado empiricamente: o mod escreve `settings.json` no seu próprio
diretório. Logo, caminhos relativos a partir do diretório do mod funcionam,
e `../../../../../engine/config/platform/pc/user.ini` resolve a partir de
`bin/x64/plugins/cyber_engine_tweaks/mods/<mod>/`.

### 6. Logs são bufferizados

`scripting.log` e `cyber_engine_tweaks.log` do CET só são gravados no fim da
sessão. Para diagnosticar em tempo real, o mod deve escrever o **próprio** log.

Cuidado: o jogo mantém o handle do arquivo aberto. Se o log tiver nome fixo,
um reload não reabre em modo `"w"`. Use **timestamp no nome do arquivo**:

```lua
local LOG_FILE = "performanceprofile-" .. os.date("%Y%m%d-%H%M%S") .. ".log"
```

### 7. `os.date` existe, `toString` não

`toString` não é uma função Lua. Use `tostring()`.

---

## Recarga de mods sem reiniciar o jogo

O overlay do CET (tecla **F1**, valor `0x70` em `bindings.json` →
`"cet": { "overlay_key": 31525197391593472 }`) tem o botão **"Reload all mods"**.

Cada recarga recria o `init.lua` do mod, o que permite iterar no painel sem
reiniciar o jogo.

Ao clicar no botão, o jogo precisa estar em foco e as coordenadas de clique
devem ser **de tela**, não da imagem capturada — a janela do jogo estava em
`(117, 66)` nesta máquina, então o botão em `(1103, 47)` na imagem corresponde
a `(1220, 113)` reais.

---

## Como capturar a tela do jogo

`PrintWindow` (user32, flag `PW_RENDERFULLCONTENT` = 2) funciona para o D3D12
do jogo, mas **bloqueia** se a janela estiver em foco — a chamada não retorna.
Solução: capturar de um processo separado, em background, sem trazer a janela
para o primeiro plano.

```powershell
Start-Process powershell -ArgumentList '-NoProfile','-WindowStyle','Hidden','-Command',`
  "Start-Sleep 1; & 'snap.ps1' -Out 'panel.png'"
```

O overlay do CET é desenhado sobre o jogo e aparece na captura.
