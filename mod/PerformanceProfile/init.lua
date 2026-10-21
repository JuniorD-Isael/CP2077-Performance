--[[
  PerformanceProfile - painel de performance in-game para Cyberpunk 2077.

  Requer:
    - RED4ext
    - Cyber Engine Tweaks (CET) >= 1.25
    - Native Settings UI (mod) >= 1.4

  Como funciona:
    - Cria uma aba "Performance" no menu de settings nativo do jogo.
    - Os widgets NAO alteram o motor ao vivo. Eles geram o user.ini e o
      UserSettings.json corretos e avisam para reiniciar o jogo.
    - O botao "Apply Profile" escreve os arquivos com backup automatico.

  Persistencia: settings.json neste mesmo diretorio (padrao do menu_mod).
--]]

local mod = {}

local SETTINGS_FILE = "settings.json"
local PROFILE_NAME = "Low-Spec 30 FPS"

local UserSettings = {}
local NativeSettings = nil
local options = {}

-- ------------------------------------------------------------------ utils

local function Log(msg)
    print("[PerformanceProfile] " .. tostring(msg))
end

local function LoadState()
    local file = io.open(SETTINGS_FILE, "r")
    if not file then return end
    local text = file:read("*a")
    file:close()
    local ok, decoded = pcall(function() return json.decode(text) end)
    if ok and type(decoded) == "table" then
        for k, v in pairs(decoded) do UserSettings[k] = v end
    end
end

local function SaveState()
    local file = io.open(SETTINGS_FILE, "w")
    if not file then return end
    file:write(json.encode(UserSettings))
    file:close()
end

local function GetBool(key, default)
    local v = UserSettings[key]
    if type(v) == "boolean" then return v end
    return default
end

local function SetValue(key, value)
    UserSettings[key] = value
    SaveState()
    if NativeSettings and NativeSettings.setOption then
        local opt = options[key]
        if opt then NativeSettings.setOption(opt, value) end
    end
end

-- --------------------------------------------------------- escrita de disco

-- Caminhos reais no Windows. O jogo usa %LOCALAPPDATA% para o UserSettings.
local function UserSettingsPath()
    local appdata = os.getenv("LOCALAPPDATA")
    if not appdata then return nil end
    return appdata .. "\\CD Projekt Red\\Cyberpunk 2077\\UserSettings.json"
end

local function GameRoot()
    -- RED4ext/CET nao expõem o caminho do jogo ao Lua de forma estavel.
    -- Estrategia: 1) override manual em settings.json, 2) env GAME_ROOT,
    -- 3) deduzir a partir do caminho do proprio script (via debug.getinfo).
    local override = UserSettings["__gameRoot"]
    if type(override) == "string" and override ~= "" then return override end

    local p = os.getenv("GAME_ROOT")
    if p and p ~= "" then return p end

    -- deduzir: <raiz>/bin/x64/plugins/cyber_engine_tweaks/mods/PerformanceProfile/init.lua
    local ok, info = pcall(function()
        return debug.getinfo(1, "S").source
    end)
    if ok and type(info) == "string" and info:sub(1, 1) == "@" then
        local path = info:sub(2)
        local marker = "\\bin\\x64\\plugins\\cyber_engine_tweaks\\mods\\PerformanceProfile"
        local i = path:lower():find(marker:lower(), 1, true)
        if i then
            return path:sub(1, i - 1)
        end
    end
    return nil
end

local function UserIniPath()
    local root = GameRoot()
    if not root then return nil end
    return root .. "\\engine\\config\\platform\\pc\\user.ini"
end

-- Substitui/insere uma chave INI preservando o resto do arquivo.
local function SetIniValue(text, section, key, value)
    local sectionPattern = "%[" .. section:gsub("%[", "%%["):gsub("%]", "%%]") .. "%]"
    local si, se = text:find(sectionPattern)
    if not si then
        return text .. "\r\n[" .. section .. "]\r\n" .. key .. " = " .. value .. "\r\n"
    end
    -- procura a chave dentro da secao
    local body = text:sub(se + 1)
    local nextSection = body:find("\n%[")
    if nextSection then body = body:sub(1, nextSection - 1) end
    local keyPattern = "^%s*" .. key:gsub("%-", "%%-") .. "%s*=%s*.-%s*$"
    local ki, ke = body:find(keyPattern)
    if ki then
        local replaced = body:sub(1, ki - 1) .. key .. " = " .. value .. body:sub(ke + 1)
        return text:sub(1, se) .. replaced .. text:sub(se + #body + 1)
    end
    -- chave nao existe: insere no fim da secao
    local insertAt = se + #body
    return text:sub(1, insertAt) .. "\r\n" .. key .. " = " .. value .. text:sub(insertAt + 1)
end

-- ------------------------------------------------------------------ acoes

local function WriteUserIni(entries)
    local path = UserIniPath()
    if not path then
        Log("nao foi possivel localizar o jogo (GameRoot)")
        return false
    end
    local file = io.open(path, "r")
    if not file then
        Log("user.ini nao encontrado em " .. path)
        return false
    end
    local text = file:read("*a")
    file:close()

    -- backup
    local backup = io.open(path .. ".perfprofile.bak", "w")
    if backup then backup:write(text) backup:close() end

    for _, e in ipairs(entries) do
        text = SetIniValue(text, e.section, e.key, e.value)
    end

    local out = io.open(path, "w")
    if not out then
        Log("falha ao escrever user.ini")
        return false
    end
    out:write(text)
    out:close()
    Log("user.ini atualizado (" .. #entries .. " chaves)")
    return true
end

local function WriteUserSettings(values)
    local path = UserSettingsPath()
    if not path then
        Log("LOCALAPPDATA indisponivel")
        return false
    end
    local file = io.open(path, "r")
    if not file then
        Log("UserSettings.json nao encontrado (rode o jogo uma vez)")
        return false
    end
    local text = file:read("*a")
    file:close()

    local backup = io.open(path .. ".perfprofile.bak", "w")
    if backup then backup:write(text) backup:close() end

    -- o jogo grava JSON com chaves "value"; substitui preservando o resto
    local ok, decoded = pcall(function() return json.decode(text) end)
    if not ok then
        Log("falha ao decodificar UserSettings.json")
        return false
    end

    local changed = 0
    for _, group in ipairs(decoded.data or {}) do
        for _, opt in ipairs(group.options or {}) do
            local want = values[opt.name]
            if want ~= nil then
                if opt.value ~= want then
                    opt.value = want
                    changed = changed + 1
                end
            end
        end
    end

    local out = io.open(path, "w")
    if not out then
        Log("falha ao escrever UserSettings.json")
        return false
    end
    out:write(json.encode(decoded))
    out:close()
    Log("UserSettings.json atualizado (" .. changed .. " opcoes)")
    return changed
end

-- ------------------------------------------------------------------ perfil

-- cvars do motor por grupo. Valores alinhados ao profile/lowspec-30fps.json.
local INI_ENTRIES = {
    { section = "Occlusion",                                key = "MultiFrustumUseGlobalOcclusion", value = "False" },
    { section = "Streaming/Culling",                         key = "VisibilityQuery",                value = "False" },
    { section = "Streaming/Culling/AutoHideDistanceNear",   key = "Enabled",                        value = "False" },
    { section = "Streaming/Culling/SecondaryRefPoint",      key = "Enabled",                        value = "False" },
    { section = "Crowd",                                    key = "ZCutoffEnabled",                 value = "False" },
    { section = "Animation",                                key = "UseAnimSetupCaching",            value = "False" },
    { section = "Animation",                                key = "EnableRefreshLocoWrappersOnUnlock", value = "False" },
    { section = "Lipsync",                                  key = "SyncToAudio",                    value = "False" },
    { section = "Lipsync",                                  key = "UseAudioAdjustment",             value = "False" },
    { section = "Editor/Characters/Hair",                   key = "UseGlobalContactShadowsOnHair",  value = "False" },
    { section = "Editor/Characters/Hair",                   key = "UseLocalContactShadowsOnHair",   value = "False" },
    { section = "Vehicle",                                  key = "EnableAirResistane",             value = "False" },
    { section = "Vehicle",                                  key = "PhysXClampHugeImpacts",          value = "False" },
    { section = "Vehicle",                                  key = "PhysXClampHugeSpeeds",           value = "False" },
    { section = "SnapToTerrainIk",                          key = "Enabled",                        value = "False" },
    { section = "SnapToTerrainIk",                          key = "FootLockEnabled",                value = "False" },
}

-- opcoes do menu: valor padrao do perfil
local MENU_DEFAULTS = {
    VolumetricCloudsQuality   = "Low",
    GlobaIlluminationRange    = "Low",
    ColorPrecision            = "Low",
    AmbientOcclusion          = "Low",
    ScreenSpaceReflectionsQuality = "Low",
    ShadowMeshQuality         = "Low",
    CascadedShadowsRange      = "Low",
    CascadedShadowsResolution = "Low",
    DistantShadowsResolution  = "Low",
    VolumetricFogResolution   = "Low",
    MaxDynamicDecals          = "Low",
    SubsurfaceScatteringQuality = "Low",
    MirrorQuality             = "Low",
    TextureQuality            = "Low",
    LODPreset                 = "Low",
    CrowdDensity              = "Low",
    Anisotropy                = 1,
    DynamicResolutionScaling  = true,
    DRS_TargetFPS             = 30,
    DRS_MinResolution         = 50,
    MaximumFPS_OnOff          = true,
    MaximumFPS_Value          = 30,
    NoSpatialAudio            = true,
    EnableCloudSaves          = false,
    EnableTelemetry           = false,
    Cinematic                 = false,
    Overheads                 = false,
    HDDMode                   = "HDD",
    HDDMode2                  = "Off",
}

-- grupos do menu: chave -> rotulo
local MENU_SWITCHES = {
    "DynamicResolutionScaling", "NoSpatialAudio", "MaximumFPS_OnOff",
    "EnableCloudSaves", "EnableTelemetry", "Cinematic", "Overheads",
}

local MENU_SELECTORS = {
    VolumetricCloudsQuality   = { "Low", "Medium", "High" },
    GlobaIlluminationRange    = { "Low", "Medium", "High" },
    ColorPrecision            = { "Low", "Medium", "High" },
    AmbientOcclusion          = { "Low", "Medium", "High" },
    ScreenSpaceReflectionsQuality = { "Low", "Medium", "High" },
    ShadowMeshQuality         = { "Low", "Medium", "High" },
    CascadedShadowsRange      = { "Low", "Medium", "High" },
    CascadedShadowsResolution = { "Low", "Medium", "High" },
    DistantShadowsResolution  = { "Low", "Medium", "High" },
    VolumetricFogResolution   = { "Low", "Medium", "High" },
    MaxDynamicDecals          = { "Low", "Medium", "High" },
    SubsurfaceScatteringQuality = { "Low", "Medium", "High" },
    MirrorQuality             = { "Low", "Medium", "High" },
    TextureQuality            = { "Low", "Medium", "High" },
    LODPreset                 = { "Low", "Medium", "High" },
    CrowdDensity              = { "Low", "Medium", "High" },
    HDDMode                   = { "HDD", "Auto", "SSD" },
}

local MENU_RANGES = {
    DRS_MinResolution = { min = 40, max = 100, step = 5 },
    DRS_TargetFPS    = { min = 20, max = 60,  step = 5 },
    MaximumFPS_Value = { min = 20, max = 60,  step = 5 },
    Anisotropy       = { min = 1,  max = 16,  step = 1 },
}

-- ------------------------------------------------------------------ setup

local function RegisterUI()
    NativeSettings.addTab("/performance", PROFILE_NAME)

    -- sub GPU
    NativeSettings.addSubcategory("/performance/gpu", "GPU / Render")
    for _, name in ipairs({
        "VolumetricCloudsQuality", "GlobaIlluminationRange", "ColorPrecision",
        "AmbientOcclusion", "ScreenSpaceReflectionsQuality", "ShadowMeshQuality",
        "CascadedShadowsRange", "CascadedShadowsResolution", "DistantShadowsResolution",
        "VolumetricFogResolution", "MaxDynamicDecals", "SubsurfaceScatteringQuality",
        "MirrorQuality", "TextureQuality",
    }) do
        local elements = MENU_SELECTORS[name]
        if elements then
            local sel = 1
            for i, e in ipairs(elements) do
                if e == MENU_DEFAULTS[name] then sel = i end
            end
            local key = "gpu." .. name
            options[key] = NativeSettings.addSelectorString(
                "/performance/gpu", name, "Ajuste de render (reiniciar o jogo)",
                elements, sel, sel,
                function(_, value)
                    SetValue(key, elements[value])
                end)
        end
    end
    local akey = "gpu.Anisotropy"
    options[akey] = NativeSettings.addRangeInt(
        "/performance/gpu", "Anisotropy", "0 = off. Menos bandwidth de textura (VRAM).",
        1, 16, 1, MENU_DEFAULTS.Anisotropy, MENU_DEFAULTS.Anisotropy,
        function(_, value) SetValue(akey, value) end)

    -- sub resolucao
    NativeSettings.addSubcategory("/performance/res", "Resolucao / FPS")
    local dkey = "res.DynamicResolutionScaling"
    options[dkey] = NativeSettings.addSwitch(
        "/performance/res", "Dynamic Resolution Scaling",
        "Upscale adaptativo. Grande ganho em GPU fraca. Reiniciar o jogo.",
        GetBool(dkey, MENU_DEFAULTS.DynamicResolutionScaling),
        MENU_DEFAULTS.DynamicResolutionScaling,
        function(_, state) SetValue(dkey, state) end)

    for _, name in ipairs({ "DRS_MinResolution", "DRS_TargetFPS", "MaximumFPS_Value" }) do
        local r = MENU_RANGES[name]
        local key = "res." .. name
        options[key] = NativeSettings.addRangeInt(
            "/performance/res", name,
            name == "DRS_MinResolution" and "Piso de resolucao em % (mais baixo = mais FPS)" or "Alvo de FPS",
            r.min, r.max, r.step, MENU_DEFAULTS[name], MENU_DEFAULTS[name],
            function(_, value) SetValue(key, value) end)
    end
    local fkey = "res.MaximumFPS_OnOff"
    options[fkey] = NativeSettings.addSwitch(
        "/performance/res", "Limite de FPS", "Trava o FPS e poupa CPU. Reiniciar o jogo.",
        GetBool(fkey, MENU_DEFAULTS.MaximumFPS_OnOff), MENU_DEFAULTS.MaximumFPS_OnOff,
        function(_, state) SetValue(fkey, state) end)

    -- sub CPU
    NativeSettings.addSubcategory("/performance/cpu", "CPU / Sistema")
    local ckey = "cpu.CrowdDensity"
    options[ckey] = NativeSettings.addSelectorString(
        "/performance/cpu", "CrowdDensity", "Densidade de multidao. Reiniciar o jogo.",
        MENU_SELECTORS.CrowdDensity, 1, 1,
        function(_, value) SetValue(ckey, MENU_SELECTORS.CrowdDensity[value]) end)
    for _, name in ipairs({ "NoSpatialAudio", "EnableCloudSaves", "EnableTelemetry", "Cinematic", "Overheads" }) do
        local key = "cpu." .. name
        options[key] = NativeSettings.addSwitch(
            "/performance/cpu", name,
            name == "NoSpatialAudio" and "Desliga o DSP surround (audio). Grande ganho de CPU." or "Opcao de sistema. Reiniciar o jogo.",
            GetBool(key, MENU_DEFAULTS[name]), MENU_DEFAULTS[name],
            function(_, state) SetValue(key, state) end)
    end
    local hkey = "cpu.HDDMode"
    options[hkey] = NativeSettings.addSelectorString(
        "/performance/cpu", "HDDMode", "Modo de I/O para disco lento.",
        MENU_SELECTORS.HDDMode, 1, 1,
        function(_, value) SetValue(hkey, MENU_SELECTORS.HDDMode[value]) end)

    -- sub acao
    NativeSettings.addSubcategory("/performance/apply", "Aplicar")
    NativeSettings.addButton(
        "/performance/apply", "Apply Profile",
        "Escreve user.ini e UserSettings.json (com backup). Feche o jogo antes.",
        "Apply", 18,
        function()
            local ok1 = WriteUserIni(INI_ENTRIES)
            local values = {}
            for k, v in pairs(MENU_DEFAULTS) do values[k] = v end
            for k, v in pairs(UserSettings) do
                local base = k:match("^[^.]+%.(.+)$")
                if base then values[base] = v end
            end
            local ok2 = WriteUserSettings(values)
            if ok1 and ok2 then
                print("[PerformanceProfile] Perfil aplicado. Reinicie o jogo.")
            end
        end)
end

-- ------------------------------------------------------------------ init

registerForEvent("onInit", function()
    LoadState()
    Log("inicializado (settings carregados)")

    -- o Native Settings pode nao estar instalado
    local ok, ns = pcall(require, "nativeSettings")
    if ok and type(ns) == "table" and ns.addTab then
        NativeSettings = ns
        RegisterUI()
        Log("painel registrado")
    else
        Log("Native Settings UI nao encontrado - painel in-game desativado")
        Log("use o instalador: Install-Profile.ps1")
    end
end)

return mod
