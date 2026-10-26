using System.Text.RegularExpressions;

namespace CP2077Performance.Core.Writing;

/// <summary>Posicao de um campo dentro do texto de um arquivo.</summary>
public sealed record FieldSpan(int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>
/// Localiza, dentro do arquivo de opcoes do jogo, o trecho exato do campo de
/// valor e o do campo de indice de cada opcao.
/// </summary>
/// <remarks>
/// O arquivo do jogo tem a forma <c>data[].options[]</c>, e cada opcao traz
/// <c>name</c>, <c>value</c> e, quando e de lista, <c>index</c>. A localizacao
/// trabalha sobre o texto, sem reescrever o documento, para que a escrita
/// surgically toque apenas os campos visados.
/// </remarks>
public static class JsonFieldLocator
{
    private static readonly Regex NamePattern = new(
        @"""name""\s*:\s*""(?<nameval>[^""]*)""",
        RegexOptions.Compiled);

    private static readonly Regex ValuePattern = new(
        @"""value""\s*:\s*(?<v>""(?:[^""\\]|\\.)*""|[^,}\]\s]+)",
        RegexOptions.Compiled);

    private static readonly Regex IndexPattern = new(
        @"""index""\s*:\s*(?<v>""(?:[^""\\]|\\.)*""|[^,}\]\s]+)",
        RegexOptions.Compiled);

    /// <summary>Campos de uma opcao.</summary>
    public sealed record OptionFields(
        string Name,
        FieldSpan Value,
        FieldSpan? Index);

    /// <summary>
    /// Localiza os campos de todas as opcoes. Uma opcao pode aparecer mais de
    /// uma vez no arquivo do jogo; todas as ocorrencias sao devolvidas.
    /// </summary>
    public static IReadOnlyDictionary<string, List<OptionFields>> Locate(string text)
    {
        var result = new Dictionary<string, List<OptionFields>>(StringComparer.Ordinal);

        foreach (Match nameMatch in NamePattern.Matches(text))
        {
            var name = nameMatch.Groups["nameval"].Value;
            var bodyStart = nameMatch.Index + nameMatch.Length;

            // o objeto da opcao termina no primeiro '}' apos o nome
            var bodyEnd = text.IndexOf('}', bodyStart);
            if (bodyEnd < 0)
            {
                continue;
            }

            var body = text[bodyStart..bodyEnd];

            var valueMatch = ValuePattern.Match(body);
            if (!valueMatch.Success)
            {
                continue;
            }

            var valueSpan = new FieldSpan(
                bodyStart + valueMatch.Groups["v"].Index,
                valueMatch.Groups["v"].Length);

            FieldSpan? indexSpan = null;
            var indexMatch = IndexPattern.Match(body);
            if (indexMatch.Success)
            {
                indexSpan = new FieldSpan(
                    bodyStart + indexMatch.Groups["v"].Index,
                    indexMatch.Groups["v"].Length);
            }

            if (!result.TryGetValue(name, out var list))
            {
                list = new List<OptionFields>();
                result[name] = list;
            }

            list.Add(new OptionFields(name, valueSpan, indexSpan));
        }

        return result;
    }

    /// <summary>Texto do valor de uma opcao, lido do arquivo.</summary>
    public static string? ReadValue(string text, string optionName)
    {
        if (!Locate(text).TryGetValue(optionName, out var fields) || fields.Count == 0)
        {
            return null;
        }

        return text[fields[0].Value.Start..fields[0].Value.End];
    }
}
