using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;

namespace CP2077Performance.Core.Writing;

public sealed record MenuOptionChange(
    string Option,
    string Before,
    string After,
    bool IndexChanged,
    string? IndexBefore,
    string? IndexAfter);

public sealed record MenuWriteResult(
    string Path,
    string NewText,
    IReadOnlyList<MenuOptionChange> Changes,
    IReadOnlyList<string> MissingOptions)
{
    public bool Changed => Changes.Count > 0;
}

/// <summary>
/// Escreve as opcoes de menu do perfil no arquivo de opcoes do jogo, alterando
/// apenas os campos de valor e de indice das opcoes visadas.
/// </summary>
/// <remarks>
/// O resto do arquivo fica byte a byte igual: nem a formatacao, nem a ordem,
/// nem as opcoes que nao fazem parte do perfil.
/// </remarks>
public sealed class MenuOptionsWriter
{
    /// <summary>
    /// Calcula as alteracoes no texto das opcoes de menu.
    /// </summary>
    /// <param name="text">O conteudo do arquivo de opcoes do jogo.</param>
    /// <param name="desired">Os valores desejados, por opcao.</param>
    /// <param name="schema">O esquema do jogo.</param>
    /// <param name="explicitIndexes">
    /// O indice a escrever por opcao, quando o schema nao declara a lista de
    /// valores aceitos. As opcoes de exibicao sao <c>is_dynamic</c>: o motor
    /// monta a lista delas em runtime, e so a evidencia gravada pelo proprio
    /// jogo diz qual e o indice.
    /// </param>
    public MenuWriteResult Plan(
        string text,
        IReadOnlyDictionary<string, ProfileValue> desired,
        GameSchema schema,
        IReadOnlyDictionary<string, int>? explicitIndexes = null)
    {
        var located = JsonFieldLocator.Locate(text);
        var changes = new List<MenuOptionChange>();
        var missing = new List<string>();

        // de tras para frente, para que os deslocamentos validos
        var edits = new List<(int Start, int Length, string Replacement)>();

        foreach (var (option, value) in desired)
        {
            if (!located.TryGetValue(option, out var occurrences) || occurrences.Count == 0)
            {
                missing.Add(option);
                continue;
            }

            var acceptedIndex = -1;

            if (explicitIndexes is not null && explicitIndexes.TryGetValue(option, out var explicitIndex))
            {
                acceptedIndex = explicitIndex;
            }
            else if (schema is not null && schema.TryGet(option, out var schemaOption) && schemaOption.HasAcceptedValues)
            {
                var wanted = value.AsString();
                if (wanted is not null)
                {
                    acceptedIndex = schemaOption.AcceptedValues.ToList().IndexOf(wanted);
                }
            }

            foreach (var occurrence in occurrences)
            {
                var currentValue = text[occurrence.Value.Start..occurrence.Value.End];
                var newValue = value.Json;

                var currentIndex = occurrence.Index is null
                    ? null
                    : text[occurrence.Index.Start..occurrence.Index.End];

                string? newIndex = null;
                if (occurrence.Index is not null && acceptedIndex >= 0)
                {
                    newIndex = acceptedIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var valueChanged = !string.Equals(currentValue, newValue, StringComparison.Ordinal);
                var indexChanged = newIndex is not null
                    && !string.Equals(currentIndex, newIndex, StringComparison.Ordinal);

                if (!valueChanged && !indexChanged)
                {
                    continue;
                }

                if (valueChanged)
                {
                    edits.Add((occurrence.Value.Start, occurrence.Value.Length, newValue));
                }

                if (indexChanged && occurrence.Index is not null)
                {
                    edits.Add((occurrence.Index.Start, occurrence.Index.Length, newIndex!));
                }

                changes.Add(new MenuOptionChange(
                    option,
                    currentValue,
                    valueChanged ? newValue : currentValue,
                    indexChanged,
                    currentIndex,
                    newIndex ?? currentIndex));
            }
        }

        var newText = ApplyEdits(text, edits);
        return new MenuWriteResult(string.Empty, newText, changes, missing);
    }

    /// <summary>Aplica as edicoes, de tras para frente.</summary>
    private static string ApplyEdits(string text, List<(int Start, int Length, string Replacement)> edits)
    {
        var ordered = edits.OrderByDescending(e => e.Start).ToList();
        foreach (var (start, length, replacement) in ordered)
        {
            text = text[..start] + replacement + text[(start + length)..];
        }

        return text;
    }
}
