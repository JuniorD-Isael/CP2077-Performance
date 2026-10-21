--[[
  PerformanceProfile - painel de performance in-game para Cyberpunk 2077.

  Requer:
    - RED4ext
    - Cyber Engine Tweaks (CET) >= 1.25
    - Native Settings UI >= 1.4

  FONTE UNICA DE VERDADE: o arquivo profile.json (copia de
  profile/lowspec-30fps.json do repositorio) ao lado deste init.lua.
  O painel nao tem nenhuma copia hardcoded dos valores do perfil - ele le
  o JSON, expoe alguns deles como widget e, ao aplicar, escreve as tres
  camadas a partir do mesmo arquivo.

  O painel NAO altera o motor ao vivo: o REDengine le as opcoes no startup.
  Ele escreve o user.ini, o UserSettings.json e o config.json do CET (com
  backup) e pede para reiniciar o jogo.

  Persistencia das preferencias do painel: settings.json neste diretorio.
--]]

local mod = {}

local SETTINGS_FILE = "settings.json"
local PROFILE_FILE  = "profile.json"
-- Nome com timestamp: o jogo pode manter o handle do arquivo anterior aberto,
-- o que impediria reabrir em modo "w" e perderia o log a cada reload.
local LOG_FILE = "performanceprofile-" .. os.date("%Y%m%d-%H%M%S") .. ".log"

-- O mod roda com o CWD no proprio diretorio:
--   <jogo>/bin/x64/plugins/cyber_engine_tweaks/mods/PerformanceProfile/
-- Logo, a raiz do jogo esta SEIS niveis acima (nao cinco).
local GAME_ROOT_REL = "../../../../../../"
local USER_INI_REL  = GAME_ROOT_REL .. "engine/config/platform/pc/user.ini"
local CET_CONFIG_REL = "../../config.json"

local UserSettings = {}   -- desvios do painel em relacao ao perfil
local options      = {}   -- metadata de UI (tipo/descricao) por opcao
local profile      = nil  -- conteudo do profile.json

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

local function LogErr(where, err)
    Log("ERRO em " .. tostring(where) .. ": " .. tostring(err))
    local ok, f = pcall(io.open, LOG_FILE .. ".erros", "a")
    if ok and f then
        f:write("[" .. os.date("%Y-%m-%d %H:%M:%S") .. "] " .. tostring(where) .. ": " .. tostring(err) .. "\n")
        f:close()
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

-- Persiste um desvio do painel em relacao ao perfil.
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

-- ------------------------------------------------------------------ perfil

-- Le o profile.json. Sem ele o painel nao registra nada.
local function LoadProfile()
    local file = io.open(PROFILE_FILE, "r")
    if not file then
        Log("profile.json nao encontrado no diretorio do mod")
        return nil
    end
    local text = file:read("*a")
    file:close()
    local ok, decoded = pcall(function() return json.decode(text) end)
    if not ok or type(decoded) ~= "table" then
        Log("falha ao decodificar profile.json")
        return nil
    end
    return decoded
end

-- Valor efetivo de uma opcao do menu: desvio do painel sobre o perfil.
local function EffectiveValue(name)
    local key = "menu." .. name
    if UserSettings[key] ~= nil then return UserSettings[key] end
    if profile and profile.userSettings and profile.userSettings.values then
        return profile.userSettings.values[name]
    end
    return nil
end

-- ------------------------------------------------------------------ escrita

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

-- O conjunto de chaves a preservar: profile.preserve.userIni
local function PreservedIniKeys()
    local out = {}
    if profile and profile.preserve and profile.preserve.userIni then
        for section, keys in pairs(profile.preserve.userIni) do
            for key, _ in pairs(keys) do out[section .. "\0" .. key] = true end
        end
    end
    return out
end

-- Escreve o user.ini com todas as cvars do perfil.
local function WriteUserIni()
    if not profile or not profile.userIni then
        Log("perfil sem secao userIni")
        return false
    end

    local path = USER_INI_REL
    local file = io.open(path, "r")
    if not file then
        Log("user.ini nao encontrado em " .. path)
        return false
    end
    local text = file:read("*a")
    file:close()

    local backup = io.open(path .. ".perfprofile.bak", "w")
    if backup then backup:write(text) backup:close() end

    local preserved = PreservedIniKeys()
    local count = 0
    for section, keys in pairs(profile.userIni) do
        if type(section) == "string" and section:sub(1, 1) ~= "_" then
            for key, value in pairs(keys) do
                if type(key) == "string" and key:sub(1, 1) ~= "_" then
                    if not preserved[section .. "\0" .. key] then
                        text = SetIniValue(text, section, key, tostring(value))
                        count = count + 1
                    end
                end
            end
        end
    end

    local out = io.open(path, "w")
    if not out then
        Log("falha ao escrever user.ini")
        return false
    end
    out:write(text)
    out:close()
    Log("user.ini atualizado (" .. count .. " chaves)")
    return true
end

-- Procura o UserSettings.json do jogador.
local function UserSettingsPath()
    if os and type(os.getenv) == "function" then
        local la = os.getenv("LOCALAPPDATA")
        if la and la ~= "" then
            return la .. "\\CD Projekt Red\\Cyberpunk 2077\\UserSettings.json"
        end
    end
    -- fallback: derivar de USERPROFILE
    if os and type(os.getenv) == "function" then
        local up = os.getenv("USERPROFILE")
        if up and up ~= "" then
            return up .. "\\AppData\\Local\\CD Projekt Red\\Cyberpunk 2077\\UserSettings.json"
        end
    end
    return nil
end

-- Escreve as opcoes de menu do perfil no UserSettings.json do jogador.
local function WriteUserSettings()
    if not profile or not profile.userSettings or not profile.userSettings.values then
        Log("perfil sem secao userSettings")
        return false
    end

    local path = UserSettingsPath()
    if not path then
        Log("LOCALAPPDATA indisponivel")
        return false
    end
    local file = io.open(path, "r")
    if not file then
        Log("UserSettings.json nao encontrado em " .. path)
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
            local want = EffectiveValue(opt.name)
            if want ~= nil and opt.value ~= want then
                opt.value = want
                changed = changed + 1
            end
            -- o arquivo guarda value E index; manter os dois coerentes
            if opt.values and opt.index then
                local idx = nil
                for i, v in ipairs(opt.values) do
                    if tostring(v) == tostring(want) then
                        idx = i - 1
                        break
                    end
                end
                if idx and tonumber(opt.index) ~= idx then opt.index = idx end
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

-- Escreve os patches do CET (config.json).
local function WriteCetConfig()
    if not profile or not profile.cetConfig or not profile.cetConfig.patches then
        Log("perfil sem secao cetConfig")
        return false
    end

    local path = CET_CONFIG_REL
    local file = io.open(path, "r")
    if not file then
        Log("config.json do CET nao encontrado em " .. path)
        return false
    end
    local text = file:read("*a")
    file:close()

    local backup = io.open(path .. ".perfprofile.bak", "w")
    if backup then backup:write(text) backup:close() end

    local ok, decoded = pcall(function() return json.decode(text) end)
    if not ok or type(decoded) ~= "table" or not decoded.patches then
        Log("falha ao decodificar config.json do CET")
        return false
    end

    local changed = 0
    for name, value in pairs(profile.cetConfig.patches) do
        if type(name) == "string" and name:sub(1, 1) ~= "_" then
            if decoded.patches[name] ~= value then
                decoded.patches[name] = value
                changed = changed + 1
            end
        end
    end

    local out = io.open(path, "w")
    if not out then
        Log("falha ao escrever config.json do CET")
        return false
    end
    out:write(json.encode(decoded))
    out:close()
    Log("config.json do CET atualizado (" .. changed .. " patches)")
    return true
end

-- ------------------------------------------------------------------ UI

-- Metadata de UI. Nao e dado de configuracao - e apenas como o controle
-- deve aparecer (tipo, faixa, rotulo). Os VALORES vem do profile.json.
local WIDGETS = {
    VolumetricCloudsQuality   = { type = "selector", values = { "Off", "Medium", "High", "Ultra" }, desc = "Qualidade das nuvens volumetricas." },
    GlobaIlluminationRange    = { type = "selector", values = { "High", "Ultra" },           desc = "Alcance da iluminacao global indireta." },
    ColorPrecision            = { type = "selector", values = { "Medium", "High" },           desc = "Precisao de cor do framebuffer." },
    AmbientOcclusion          = { type = "selector", values = { "Off", "Low", "Medium", "High" }, desc = "Oclusao ambiente (SSAO)." },
    ScreenSpaceReflectionsQuality = { type = "selector", values = { "Off", "Low", "Medium", "High", "Ultra", "Insane" }, desc = "Reflexos em espaco de tela." },
    ShadowMeshQuality         = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Qualidade da malha de sombra." },
    CascadedShadowsRange      = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Alcance das sombras em cascata." },
    CascadedShadowsResolution = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Resolucao das sombras em cascata." },
    DistantShadowsResolution  = { type = "selector", values = { "Low", "High" },             desc = "Resolucao das sombras distantes." },
    VolumetricFogResolution   = { type = "selector", values = { "Low", "Medium", "High", "Ultra" }, desc = "Resolucao da neblina volumetrica." },
    MaxDynamicDecals          = { type = "selector", values = { "Low", "Medium", "High", "Ultra" }, desc = "Quantidade maxima de decals dinamicos." },
    SubsurfaceScatteringQuality = { type = "selector", values = { "Low", "Medium", "High" },  desc = "Subsurface scattering." },
    MirrorQuality             = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Qualidade dos reflexos em espelho." },
    TextureQuality            = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Qualidade das texturas. Impacto direto na VRAM." },
    LODPreset                 = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Distancia de troca de LOD." },
    CrowdDensity              = { type = "selector", values = { "Low", "Medium", "High" },    desc = "Densidade da multidao." },
    HDDMode                   = { type = "selector", values = { "Off", "On", "Auto" },        desc = "Modo de I/O para disco lento." },
    MotionBlur                = { type = "selector", values = { "Off", "Low", "High" },       desc = "Desfoque de movimento." },

    Anisotropy                = { type = "range", min = 1, max = 16, step = 1, desc = "1 = minimo. Reduz bandwidth de textura (VRAM)." },
    DRS_MinResolution         = { type = "range", min = 40, max = 100, step = 5, desc = "Piso de resolucao do DRS, em % (menor = mais FPS)." },
    DRS_MaxResolution         = { type = "range", min = 40, max = 100, step = 5, desc = "Teto de resolucao do DRS, em %." },
    DRS_TargetFPS             = { type = "range", min = 20, max = 60, step = 5, desc = "Alvo de FPS do DRS." },
    MaximumFPS_Value         = { type = "range", min = 20, max = 60, step = 5, desc = "Limite de FPS." },

    DynamicResolutionScaling  = { type = "switch", desc = "Upscale adaptativo. Maior ganho em GPU fraca." },
    MaximumFPS_OnOff          = { type = "switch", desc = "Trava o FPS e poupa CPU." },
    NoSpatialAudio            = { type = "switch", desc = "Desliga o DSP surround do audio. Ganho de CPU." },
    EnableCloudSaves          = { type = "switch", desc = "Desliga saves em nuvem (I/O e rede)." },
    EnableTelemetry           = { type = "switch", desc = "Desliga telemetria. (O jogo pode reativar por politica de conta.)" },
    Cinematic                 = { type = "switch", desc = "Desliga a camera cinematografica." },
    Overheads                 = { type = "switch", desc = "Desliga o HUD de cobertura." },
    FilmGrain                 = { type = "switch", desc = "Desliga o grão de filme." },
    ChromaticAberration       = { type = "switch", desc = "Desliga a aberracao cromatica." },
    DepthOfField              = { type = "switch", desc = "Desliga a profundidade de campo." },
    LensFlares                = { type = "switch", desc = "Desliga os lens flares." },
    Vignette                  = { type = "switch", desc = "Desliga a vinheta." },
}

-- Em que subcategoria cada opcao aparece.
local CATEGORY = {
    ["VolumetricCloudsQuality"]="gpu", ["GlobaIlluminationRange"]="gpu", ["ColorPrecision"]="gpu",
    ["AmbientOcclusion"]="gpu", ["ScreenSpaceReflectionsQuality"]="gpu", ["ShadowMeshQuality"]="gpu",
    ["CascadedShadowsRange"]="gpu", ["CascadedShadowsResolution"]="gpu", ["DistantShadowsResolution"]="gpu",
    ["VolumetricFogResolution"]="gpu", ["MaxDynamicDecals"]="gpu", ["SubsurfaceScatteringQuality"]="gpu",
    ["MirrorQuality"]="gpu", ["TextureQuality"]="gpu", ["LODPreset"]="gpu", ["Anisotropy"]="gpu",
    ["MotionBlur"]="gpu", ["FilmGrain"]="gpu", ["ChromaticAberration"]="gpu", ["DepthOfField"]="gpu",
    ["LensFlares"]="gpu", ["Vignette"]="gpu",
    ["DynamicResolutionScaling"]="res", ["DRS_MinResolution"]="res", ["DRS_MaxResolution"]="res",
    ["DRS_TargetFPS"]="res", ["MaximumFPS_OnOff"]="res", ["MaximumFPS_Value"]="res",
    ["CrowdDensity"]="cpu", ["NoSpatialAudio"]="cpu", ["EnableCloudSaves"]="cpu",
    ["EnableTelemetry"]="cpu", ["Cinematic"]="cpu", ["Overheads"]="cpu", ["HDDMode"]="cpu",
}

local function RegisterUI(ns)
    ns.addTab("/performance", profile.name or "Performance Profile")

    local cats = { gpu = "GPU / Render", res = "Resolucao / FPS", cpu = "CPU / Sistema" }
    for _, c in ipairs({ "gpu", "res", "cpu" }) do
        ns.addSubcategory("/performance/" .. c, cats[c])
    end

    local registered = 0
    for name, w in pairs(WIDGETS) do
        local base = EffectiveValue(name)
        if base ~= nil then
            local cat = CATEGORY[name] or "gpu"
            local key = "menu." .. name
            if w.type == "selector" then
                local sel = 1
                for i, e in ipairs(w.values) do if e == base then sel = i end end
                local list = w.values
                ns.addSelectorString("/performance/" .. cat, name, w.desc, list, sel, sel,
                    function(value) SetValue(key, list[value]) end)
            elseif w.type == "range" then
                ns.addRangeInt("/performance/" .. cat, name, w.desc,
                    w.min, w.max, w.step, GetNum(key, base), base,
                    function(value) SetValue(key, value) end)
            elseif w.type == "switch" then
                ns.addSwitch("/performance/" .. cat, name, w.desc,
                    GetBool(key, base == true), base == true,
                    function(state) SetValue(key, state) end)
            end
            registered = registered + 1
        end
    end
    Log("widgets registrados: " .. registered)

    ns.addSubcategory("/performance/apply", "Aplicar")
    ns.addButton("/performance/apply", "Apply Profile",
        "Escreve user.ini, UserSettings.json e config.json do CET (com backup). Feche o jogo antes de aplicar.",
        "Apply", 18,
        function()
            local ok1, ok2, ok3 = false, false, false
            local okc, err = pcall(function()
                ok1 = WriteUserIni()
                ok2 = WriteUserSettings()
                ok3 = WriteCetConfig()
            end)
            if not okc then
                LogErr("Apply Profile", err)
                return
            end
            if ok1 and ok2 and ok3 then
                Log("Perfil aplicado. Reinicie o jogo.")
            else
                Log("Perfil aplicado PARCIALMENTE (user.ini=" .. tostring(ok1) ..
                    " UserSettings=" .. tostring(ok2) .. " CET=" .. tostring(ok3) .. ")")
            end
        end)
end

-- ------------------------------------------------------------------ init

registerForEvent("onInit", function()
    Log("=== PerformanceProfile iniciando ===")
    LoadState()
    Log("desvios do painel: " .. countKeys(UserSettings))

    profile = LoadProfile()
    if not profile then
        Log("sem profile.json - painel desativado")
        Log("use o instalador: Install-Profile.ps1")
        return
    end
    Log("perfil carregado: " .. tostring(profile.name) .. " v" .. tostring(profile.version))

    -- API oficial: GetMod("nativeSettings")
    local ok, ns = pcall(function() return GetMod("nativeSettings") end)
    if not ok then
        Log("GetMod indisponivel: " .. tostring(ns))
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
        LogErr("RegisterUI", err)
    end
    Log("=== PerformanceProfile pronto ===")
end)
