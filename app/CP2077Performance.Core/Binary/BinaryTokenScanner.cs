using System.Text;

namespace CP2077Performance.Core.Binary;

/// <summary>
/// Extrai as strings do executavel do jogo.
/// </summary>
/// <remarks>
/// O motor registra as opcoes como <c>GameOption</c> e o executavel guarda a
/// <em>secao</em> e a <em>chave</em> como strings separadas: o path
/// <c>Grupo/Feature/Nome</c> e montado em runtime. Por isso a validacao de
/// uma chave de cvar procura o token isolado correspondente ao nome da chave.
///
/// As strings do executavel sao separadas por byte nulo, que e o criterio
/// usado na medicao que originou esta regra.
/// </remarks>
public static class BinaryTokenScanner
{
    public static HashSet<string> ExtractTokens(string path, int minLength = 2, int maxLength = 96)
    {
        var bytes = File.ReadAllBytes(path);
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var buffer = new List<byte>(maxLength);

        foreach (var b in bytes)
        {
            if (b == 0)
            {
                AddIfUsable(buffer, tokens, minLength, maxLength);
                buffer.Clear();
            }
            else
            {
                buffer.Add(b);
            }
        }

        AddIfUsable(buffer, tokens, minLength, maxLength);
        return tokens;
    }

    private static void AddIfUsable(List<byte> buffer, HashSet<string> tokens, int minLength, int maxLength)
    {
        if (buffer.Count < minLength || buffer.Count > maxLength)
        {
            return;
        }

        // so interessam tokens integralmente imprimiveis
        var printable = true;
        foreach (var b in buffer)
        {
            if (b < 0x20 || b > 0x7E)
            {
                printable = false;
                break;
            }
        }

        if (!printable)
        {
            return;
        }

        tokens.Add(Encoding.ASCII.GetString(buffer.ToArray()));
    }
}
