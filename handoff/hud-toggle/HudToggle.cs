using System.Text.Json;
using System.Text.Json.Nodes;

namespace CP2077Performance.Core.GameIntegration;

/// <summary>Como o HUD do jogo esta.</summary>
public enum HudState
{
    /// <summary>HUD reduzido, com a interface minima.</summary>
    Minimal,

    /// <summary>HUD completo.</summary>
    Complete,
}

/// <summary>
/// A ponte entre o aplicativo e o mod que alterna o HUD dentro do jogo.
/// </summary>
/// <remarks>
/// O jogo le as opcoes de interface no startup, e o mod e quem as muda em
/// memoria, em tempo real. A fronteira entre os dois e o diretorio do mod:
/// o aplicativo escreve la a intencao, e o mod escreve la o estado.
///
/// A tecla e capturada pelo aplicativo, nao pelo mod: o CET nao expoe
/// registro de tecla para os mods na versao instalada (v1.37), e o mod nao
/// precisa de um: ele procura a intencao e aplica o que mudou.
/// </remarks>
public sealed class HudToggle
{
    /// <summary>Nome do mod, que e tambem a pasta onde ele e instalado.</summary>
    public const string ModId = "CP2077Performance";

    /// <summary>Codigo virtual da tecla padrao, F8.</summary>
    public const int DefaultVirtualKey = 0x77;

    /// <summary>
    /// A variante do perfil que corresponde ao HUD minimo. Quando ela esta
    /// ligada, o HUD do jogo e o reduzido.
    /// </summary>
    public const string HudVariantId = "hudMinimal";

    private const string IntentFileName = "intencao.json";
    private const string StateFileName = "estado.json";

    private readonly string _modDirectory;

    public HudToggle(string gameRoot)
    {
        if (string.IsNullOrWhiteSpace(gameRoot))
        {
            throw new ArgumentException("a pasta do jogo e obrigatoria", nameof(gameRoot));
        }

        var cet = Path.Combine(gameRoot, "bin", "x64", "plugins", "cyber_engine_tweaks");
        _modDirectory = Path.Combine(cet, "mods", ModId);
    }

    /// <summary>O diretorio do mod, derivado da pasta do jogo.</summary>
    public string ModDirectory => _modDirectory;

    /// <summary>O arquivo em que o aplicativo escreve a intencao.</summary>
    public string IntentPath => Path.Combine(_modDirectory, IntentFileName);

    /// <summary>O arquivo em que o mod escreve o estado em vigor.</summary>
    public string StatePath => Path.Combine(_modDirectory, StateFileName);

    /// <summary>Verdadeiro quando o mod esta instalado.</summary>
    public bool IsInstalled => File.Exists(Path.Combine(_modDirectory, "init.lua"));

    /// <summary>
    /// Instala os arquivos do mod.
    /// </summary>
    /// <param name="modSourcePath">O caminho do <c>init.lua</c> a instalar.</param>
    /// <returns>Verdadeiro quando o mod foi instalado.</returns>
    public bool Install(string modSourcePath)
    {
        try
        {
            Directory.CreateDirectory(_modDirectory);
            File.Copy(modSourcePath, Path.Combine(_modDirectory, "init.lua"), overwrite: true);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Grava a intencao: o estado do HUD que o aplicativo quer.</summary>
    /// <remarks>
    /// O arquivo e um texto simples com o nome do estado. Nao ha json
    /// disponivel no sandbox do mod, e nao ha razao para trazer esse
    /// formato para um arquivo de duas palavras.
    /// </remarks>
    public bool WriteIntent(HudState state)
    {
        try
        {
            Directory.CreateDirectory(_modDirectory);
            File.WriteAllText(IntentPath, Describe(state));
            Intent = state;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// A ultima intencao que este aplicativo publicou. E o que ele pediu; o que
    /// esta em vigor no jogo e o que o mod gravou, que atrasa um instante.
    /// </summary>
    public HudState? Intent { get; private set; }

    /// <summary>
    /// Alterna a intencao do HUD e publica a nova. Devolve o estado pedido.
    /// </summary>
    /// <remarks>
    /// A virada sai do que ja foi pedido, e nao do que o mod gravou: o mod
    /// leva um instante para aplicar, e duas teclas seguidas leriam o mesmo
    /// estado e nao alternariam nada.
    /// </remarks>
    public HudState ToggleIntent()
    {
        var atual = Intent ?? ReadState() ?? HudState.Complete;
        var novo = atual == HudState.Minimal ? HudState.Complete : HudState.Minimal;

        WriteIntent(novo);

        return novo;
    }

    /// <summary>
    /// O estado do HUD em vigor, como o mod o gravou. <c>null</c> quando o mod
    /// ainda nao gravou nada.
    /// </summary>
    public HudState? ReadState()
    {
        try
        {
            if (!File.Exists(StatePath))
            {
                return null;
            }

            return Parse(File.ReadAllText(StatePath).Trim());
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>O nome do estado, como o mod e o aplicativo o escrevem.</summary>
    public static string Describe(HudState state) =>
        state == HudState.Minimal ? "minimo" : "completo";

    /// <summary>Le o nome do estado, ou <c>null</c> quando nao e conhecido.</summary>
    public static HudState? Parse(string text) => text switch
    {
        "minimo" => HudState.Minimal,
        "completo" => HudState.Complete,
        _ => null,
    };

    /// <summary>
    /// Como o CET guarda a tecla: o codigo virtual deslocado 48 bits, com os
    /// modificadores nos bits de baixo. O CET instalado usa essa forma, mas o
    /// mod nao registra tecla: quem captura e o aplicativo.
    /// </summary>
    public static long EncodeVirtualKey(int virtualKey) => (long)virtualKey << 48;

    /// <summary>O inverso de <see cref="EncodeVirtualKey"/>.</summary>
    public static int DecodeVirtualKey(long encoded) => (int)(encoded >> 48);

    /// <summary>
    /// O nome de uma tecla, para mostrar ao usuario. As teclas de funcao e as
    /// letras tem nome; as outras, o codigo hexadecimal.
    /// </summary>
    public static string DescribeVirtualKey(int virtualKey)
    {
        if (virtualKey >= 0x70 && virtualKey <= 0x87)
        {
            return $"F{virtualKey - 0x6F}";
        }

        if (virtualKey >= 0x30 && virtualKey <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        return $"0x{virtualKey:X2}";
    }
}
