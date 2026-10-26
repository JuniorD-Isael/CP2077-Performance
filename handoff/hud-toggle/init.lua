--[[
  CP2077 Performance - alterna o HUD do jogo durante a sessao.

  O jogo le as opcoes de interface no startup. Este mod muda as opcoes do
  grupo /interface/hud na memoria do motor, em tempo real, sem reiniciar.

  O que vier do aplicativo:
    - intencao.json  o estado do HUD que o aplicativo quer (ele escreve)
    - estado.json    o estado que esta em vigor agora (este mod escreve)

  O mod le e escreve SOMENTE no proprio diretorio.

  O CET nao expoe registro de tecla para mods na versao instalada, entao a
  tecla e capturada pelo aplicativo, que grava a intencao. O mod procura a
  intencao a cada meio segundo e aplica o que mudou.
]]

local ARQUIVO_INTENCAO = "intencao.json"
local ARQUIVO_ESTADO = "estado.json"
local GRUPO_HUD = "/interface/hud"
local INTERVALO = 30

local function Log(mensagem)
    local linha = os.date("%H:%M:%S") .. " [cp2077] " .. tostring(mensagem)
    print(linha)

    local arquivo = io.open("cp2077.log", "a")
    if arquivo then
        arquivo:write(linha .. "\n")
        arquivo:close()
    end
end

-- ------------------------------------------------------------------ arquivos

local function LerArquivo(nome)
    local arquivo = io.open(nome, "r")
    if not arquivo then
        return nil
    end

    local texto = arquivo:read("*a")
    arquivo:close()

    if not texto or texto == "" then
        return nil
    end

    return texto
end

local function GravarArquivo(nome, texto)
    local arquivo = io.open(nome, "w")
    if not arquivo then
        Log("ERRO: nao foi possivel gravar " .. nome)
        return false
    end

    arquivo:write(texto)
    arquivo:close()
    return true
end

-- O arquivo de intencao e um texto simples: o nome do estado. Nao ha json
-- disponivel no sandbox desta versao do CET.
local function LerIntencao()
    local texto = LerArquivo(ARQUIVO_INTENCAO)
    if not texto then
        return nil
    end

    local estado = texto:match("^%s*(%a+)%s*$")
    if estado == "minimo" or estado == "completo" then
        return estado
    end

    return nil
end

-- ------------------------------------------------------------------ o jogo

local function SistemaDeOpcoes()
    return Game.GetSettingsSystem()
end

-- Todas as opcoes booleanas de um grupo, com o valor atual.
local function OpcoesDoGrupo(grupo)
    local opcoes = {}

    for _, opcao in ipairs(grupo:GetVars(false)) do
        local tipo = opcao:GetType()
        if tipo and tipo.value == "Bool" then
            table.insert(opcoes, {
                opcao = opcao,
                nome = Game.NameToString(opcao:GetName()),
                valor = opcao:GetValue(),
            })
        end
    end

    return opcoes
end

-- Coloca todas as opcoes booleanas do grupo no mesmo estado.
local function AplicarEstado(grupo, estado)
    local sistema = SistemaDeOpcoes()
    local aplicadas = 0

    for _, item in ipairs(OpcoesDoGrupo(grupo)) do
        if item.valor ~= estado then
            item.opcao:SetValue(estado)
            aplicadas = aplicadas + 1
        end
    end

    if sistema:NeedsConfirmation() then
        sistema:ConfirmChanges()
    end

    return aplicadas
end

-- O estado do HUD: minimo e o HUD com a interface reduzida.
local function EhMinimo(opcoes)
    for _, item in ipairs(opcoes) do
        if item.nome == "minimap" then
            return not item.valor
        end
    end

    return false
end

-- As opcoes do grupo /interface/hud sao todas do mesmo tipo: cada uma liga
-- um elemento da interface, e o padrao do jogo e todas ligadas. Por isso o
-- HUD minimo e o grupo com tudo desligado, e o HUD completo e o grupo com
-- tudo ligado, que e o que o jogo traz por padrao.
local function Aplicar(estado)
    local sistema = SistemaDeOpcoes()
    local grupo = sistema:GetGroup(GRUPO_HUD)

    if not grupo then
        Log("ERRO: grupo " .. GRUPO_HUD .. " nao encontrado")
        return
    end

    local aplicadas = AplicarEstado(grupo, estado ~= "minimo")
    Log("HUD " .. estado .. " aplicado (" .. aplicadas .. " opcoes alteradas)")

    -- o estado de cada opcao depois da mudanca: e a forma de conferir, no
    -- log, o que o jogo tem em memoria
    local linhas = {}
    for _, item in ipairs(OpcoesDoGrupo(grupo)) do
        table.insert(linhas, item.nome .. "=" .. tostring(item.valor))
    end
    Log("opcoes: " .. table.concat(linhas, " "))

    GravarArquivo(ARQUIVO_ESTADO, estado)
end

-- ------------------------------------------------------------------ registro

local ultimaIntencao = nil
local quadros = 0

registerForEvent("onInit", function()
    Log("onInit: o mod carregou")

    local grupo = SistemaDeOpcoes():GetGroup(GRUPO_HUD)
    if not grupo then
        Log("ERRO: grupo " .. GRUPO_HUD .. " nao encontrado")
        return
    end

    -- o estado inicial: o que o aplicativo quer, ou o que o jogo ja tem
    local intencao = LerIntencao()
    if intencao then
        ultimaIntencao = intencao
        Aplicar(intencao)
    else
        local minimo = EhMinimo(OpcoesDoGrupo(grupo))
        ultimaIntencao = minimo and "minimo" or "completo"
        GravarArquivo(ARQUIVO_ESTADO, ultimaIntencao)
        Log("estado inicial do HUD: " .. ultimaIntencao)
    end
end)

registerForEvent("onUpdate", function()
    quadros = quadros + 1
    if quadros % INTERVALO ~= 0 then
        return
    end

    local intencao = LerIntencao()
    if not intencao or intencao == ultimaIntencao then
        return
    end

    ultimaIntencao = intencao
    Aplicar(intencao)
end)
