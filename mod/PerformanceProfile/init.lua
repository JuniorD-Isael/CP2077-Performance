--[[
  PerformanceProfile - painel de performance in-game para Cyberpunk 2077.

  Requer:
    - RED4ext
    - Cyber Engine Tweaks (CET) >= 1.25
    - Native Settings UI >= 1.4

  O painel NAO altera o motor ao vivo: o REDengine le as opcoes no startup.
  Ele escreve o user.ini e o UserSettings.json corretos (com backup) e pede
  para reiniciar o jogo.

  Persistencia das preferencias do painel: settings.json neste diretorio.
--]]

local mod = {}

local SETTINGS_FILE = "settings.json"
-- Nome com timestamp: o jogo pode manter o handle do arquivo anterior aberto,
-- o que impediria reabrir em modo "w" e perderia o log a cada reload.
local LOG_FILE = "performanceprofile-" .. os.date("%Y%m%d-%H%M%S") .. ".log"
local PROFILE_NAME = "Low-Spec 30 FPS"

local UserSettings = {}
local options = {}

-- ------------------------------------------------------------------ utils

-- Log proprio: o script.log do CET e bufferizado e so flush no fim da sessao.
-- Um arquivo dedicado permite diagnosticar problemas em tempo real.
local function Log(msg)
    local line = "[" .. os.date("%Y-%m-%d %H:%M:%S") .. "] " .. tostring(msg)
    print("[PerformanceProfile] " .. tostring(msg))
    local ok, file = pcall(io.open, LOG_FILE, "a")
    if ok and file then
        file:write(line .. "\n")
        file:close()
    end
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

local function GetNum(key, default)
    local v = UserSettings[key]
    if type(v) == "number" then return v end
    return default
end

local function GetStr(key, default)
    local v = UserSettings[key]
    if type(v) == "string" then return v end
    return default
end

-- Persiste uma preferencia do painel e a aplica de imediato quando possivel.
local function SetValue(key, value)
    UserSettings[key] = value
    SaveState()
    Log("alterado: " .. key .. " = " .. tostring(value))
end

local function countKeys(t)
    local n = 0
    for _ in pairs(t) do n = n + 1 end
    return n
end

-- ------------------------------------------------------------------ escrita

local function UserSettingsPath()
    local appdata = os.getenv("LOCALAPPDATA")
    if not appdata then return nil end
    return appdata .. "\\CD Projekt Red\\Cyberpunk 2077\\UserSettings.json"
end

-- Substitui/insere uma chave INI preservando o resto do arquivo.
local function SetIniValue(text, section, key, value)
    local sectionPattern = "%[" .. section:gsub("%[", "%%["):gsub("%]", "%%]") .. "%]"
    local si, se = text:find(sectionPattern)
    if not si then
        return text .. "\r\n[" .. section .. "]\r\n" .. key .. " = " .. value .. "\r\n"
    end
    local body = text:sub(se + 1)
    local nextSection = body:find("\n%[")
    if nextSection then body = body:sub(1, nextSection - 1) end
    local keyPattern = "^%s*" .. key:gsub("%-", "%%-") .. "%s*=%s*.-%s*$"
    local ki, ke = body:find(keyPattern)
    if ki then
        local replaced = body:sub(1, ki - 1) .. key .. " = " .. value .. body:sub(ke + 1)
        return text:sub(1, se) .. replaced .. text:sub(se + #body + 1)
    end
    local insertAt = se + #body
    return text:sub(1, insertAt) .. "\r\n" .. key .. " = " .. value .. text:sub(insertAt + 1)
end

local function WriteUserIni(entries)
    -- o caminho do user.ini vem do proprio mod: <raiz>/bin/x64/plugins/cyber_engine_tweaks/mods/PerformanceProfile
    -- Os mods rodam com CWD no proprio diretorio, entao o caminho relativo
    -- "../../../../../engine/config/platform/pc/user.ini" resolve.
    local rel = "../../../../../engine/config/platform/pc/user.ini"
    local path = rel
    local file = io.open(path, "r")
    if not file then
        Log("user.ini nao encontrado em " .. path)
        return false
    end
    local text = file:read("*a")
    file:close()

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

    local ok, decoded = pcall(function() return json.decode(text) end)
    if not ok then
        Log("falha ao decodificar UserSettings.json")
        return false
    end

    local changed = 0
    for _, group in ipairs(decoded.data or {}) do
        for _, opt in ipairs(group.options or {}) do
            local want = values[opt.name]
            if want ~= nil and opt.value ~= want then
                opt.value = want
                changed = changed + 1
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
    return changed > 0
end

-- ------------------------------------------------------------------ perfil

-- cvars do motor por grupo. Alinhado ao profile/lowspec-30fps.json.
-- Preservados de proposito: DistantShadows, GlobalIllumination, Hair.
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
-- IMPORTANTE: os valores devem existir no schema do jogo
-- (r6/config/settings/options.json, campo "values"). O jogo reverte valores
-- invalidos ao carregar, silenciosamente.
local MENU_DEFAULTS = {
    VolumetricCloudsQuality   = "Off",
    GlobaIlluminationRange    = "High",
    ColorPrecision            = "Medium",
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
    HDDMode                   = "On",
    HDDMode2                  = "Off",
}

-- opcoes com seletor de lista (valores aceitos pelo schema do jogo)
local MENU_LISTS = {
    VolumetricCloudsQuality   = { "Off", "Medium", "High", "Ultra" },
    GlobaIlluminationRange    = { "High", "Ultra" },
    ColorPrecision            = { "Medium", "High" },
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
    HDDMode                   = { "Off", "On", "Auto" },
}

-- opcoes com slider
local MENU_RANGES = {
    DRS_MinResolution = { min = 40, max = 100, step = 5, desc = "Piso de resolucao em % (menor = mais FPS)" },
    DRS_TargetFPS    = { min = 20, max = 60,  step = 5, desc = "Alvo de FPS do DRS" },
    MaximumFPS_Value = { min = 20, max = 60,  step = 5, desc = "Limite de FPS" },
    Anisotropy       = { min = 1,  max = 16,  step = 1, desc = "1 = minimo. Reduz bandwidth de textura (VRAM)" },
}

-- opcoes booleanas
local MENU_SWITCHES = {
    DynamicResolutionScaling = "Upscale adaptativo. Maior ganho em GPU fraca.",
    NoSpatialAudio           = "Desliga o DSP surround do audio. Ganho de CPU.",
    MaximumFPS_OnOff        = "Trava o FPS e poupa CPU.",
    EnableCloudSaves         = "Desliga saves em nuvem (I/O e rede).",
    EnableTelemetry          = "Desliga telemetria.",
    Cinematic                = "Desliga a camera cinematografica.",
    Overheads                = "Desliga o HUD de cobertura.",
}

-- ------------------------------------------------------------------ registro

local function RegisterUI(ns)
    ns.addTab("/performance", PROFILE_NAME)

    -- GPU / render
    ns.addSubcategory("/performance/gpu", "GPU / Render")
    for name, list in pairs(MENU_LISTS) do
        if name ~= "CrowdDensity" and name ~= "HDDMode" then
            local sel = 1
            for i, e in ipairs(list) do
                if e == MENU_DEFAULTS[name] then sel = i end
            end
            local key = "gpu." .. name
            ns.addSelectorString("/performance/gpu", name,
                "Ajuste de render. Reiniciar o jogo.", list, sel, sel,
                function(value) SetValue(key, list[value]) end)
        end
    end
    for name, r in pairs(MENU_RANGES) do
        if name == "Anisotropy" then
            local key = "gpu." .. name
            ns.addRangeInt("/performance/gpu", name, r.desc,
                r.min, r.max, r.step, GetNum(key, MENU_DEFAULTS[name]), MENU_DEFAULTS[name],
                function(value) SetValue(key, value) end)
        end
    end

    -- Resolucao / FPS
    ns.addSubcategory("/performance/res", "Resolucao / FPS")
    for name, desc in pairs(MENU_SWITCHES) do
        if name == "DynamicResolutionScaling" or name == "MaximumFPS_OnOff" then
            local key = "res." .. name
            ns.addSwitch("/performance/res", name, desc .. " Reiniciar o jogo.",
                GetBool(key, MENU_DEFAULTS[name]), MENU_DEFAULTS[name],
                function(state) SetValue(key, state) end)
        end
    end
    for name, r in pairs(MENU_RANGES) do
        if name ~= "Anisotropy" then
            local key = "res." .. name
            ns.addRangeInt("/performance/res", name, r.desc,
                r.min, r.max, r.step, GetNum(key, MENU_DEFAULTS[name]), MENU_DEFAULTS[name],
                function(value) SetValue(key, value) end)
        end
    end

    -- CPU / sistema
    ns.addSubcategory("/performance/cpu", "CPU / Sistema")
    local ck = "cpu.CrowdDensity"
    local clist = MENU_LISTS.CrowdDensity
    local csel = 1
    for i, e in ipairs(clist) do
        if e == MENU_DEFAULTS.CrowdDensity then csel = i end
    end
    ns.addSelectorString("/performance/cpu", "CrowdDensity",
        "Densidade de multidao. Reiniciar o jogo.", clist, csel, csel,
        function(value) SetValue(ck, clist[value]) end)

    for name, desc in pairs(MENU_SWITCHES) do
        if name ~= "DynamicResolutionScaling" and name ~= "MaximumFPS_OnOff" then
            local key = "cpu." .. name
            ns.addSwitch("/performance/cpu", name, desc .. " Reiniciar o jogo.",
                GetBool(key, MENU_DEFAULTS[name]), MENU_DEFAULTS[name],
                function(state) SetValue(key, state) end)
        end
    end

    local hk = "cpu.HDDMode"
    local hlist = MENU_LISTS.HDDMode
    local hsel = 1
    for i, e in ipairs(hlist) do
        if e == MENU_DEFAULTS.HDDMode then hsel = i end
    end
    ns.addSelectorString("/performance/cpu", "HDDMode",
        "Modo de I/O para disco lento.", hlist, hsel, hsel,
        function(value) SetValue(hk, hlist[value]) end)

    -- acao
    ns.addSubcategory("/performance/apply", "Aplicar")
    ns.addButton("/performance/apply", "Apply Profile",
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
    Log("=== PerformanceProfile iniciando ===")
    LoadState()
    Log("settings carregados: " .. countKeys(UserSettings))

    -- API oficial: GetMod("nativeSettings")
    local ok, ns = pcall(function() return GetMod("nativeSettings") end)
    if not ok then
        Log("GetMod indisponivel: " .. tostring(ns))
        Log("use o instalador: Install-Profile.ps1")
        return
    end
    if not ns then
        Log("Native Settings UI nao instalado - painel in-game desativado")
        Log("use o instalador: Install-Profile.ps1")
        return
    end
    if not ns.addTab then
        Log("mod nativeSettings sem addTab - versao incompativel")
        return
    end

    local ok2, err = pcall(RegisterUI, ns)
    if ok2 then
        Log("painel registrado com sucesso")
    else
        Log("ERRO ao registrar painel: " .. tostring(err))
    end
    Log("=== PerformanceProfile pronto ===")
end)
