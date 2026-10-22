using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;

namespace CP2077Performance.Core.Writing;

public sealed record WriteVerificationItem(string Layer, string Target, bool Ok, string? Detail);

public sealed record WriteVerificationResult(IReadOnlyList<WriteVerificationItem> Items)
{
    public bool Ok => Items.All(i => i.Ok);

    public IEnumerable<WriteVerificationItem> Failures => Items.Where(i => !i.Ok);
}

/// <summary>
/// Relê os arquivos escritos e compara com o que era esperado.
/// </summary>
/// <remarks>
/// A escrita cirúrgica e mais delicada que um round-trip de serializacao: um
/// deslocamento errado quebraria o arquivo sem erro visivel. Por isso a
/// verificacao relê o resultado e compara.
/// </remarks>
public static class WriteVerifier
{
    public static WriteVerificationResult VerifyMenu(
        string text,
        IReadOnlyDictionary<string, ProfileValue> desired,
        GameSchema schema)
    {
        var items = new List<WriteVerificationItem>();

        foreach (var (option, value) in desired)
        {
            var current = JsonFieldLocator.ReadValue(text, option);
            var ok = current is not null && string.Equals(current, value.Json, StringComparison.Ordinal);

            var detail = ok
                ? null
                : $"esperava {value.Json}, encontrei {current ?? "(opcao ausente)"}";

            if (ok && schema.TryGet(option, out var schemaOption) && schemaOption.HasAcceptedValues)
            {
                var wanted = value.AsString();
                if (wanted is not null)
                {
                    var expectedIndex = schemaOption.AcceptedValues.ToList().IndexOf(wanted);
                    if (expectedIndex >= 0)
                    {
                        var index = ReadIndex(text, option);
                        if (index is not null && index != expectedIndex)
                        {
                            ok = false;
                            detail = $"indice {index} aponta para outro item; esperava {expectedIndex}";
                        }
                    }
                }
            }

            items.Add(new WriteVerificationItem("Menu", option, ok, detail));
        }

        return new WriteVerificationResult(items);
    }

    public static WriteVerificationResult VerifyIni(
        string text,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> desired,
        IReadOnlyList<UserIniKey>? removedKeys = null)
    {
        var items = new List<WriteVerificationItem>();

        foreach (var (section, keys) in desired)
        {
            foreach (var (key, value) in keys)
            {
                var current = ReadIniValue(text, section, key);
                var ok = current is not null && string.Equals(current, value, StringComparison.Ordinal);
                items.Add(new WriteVerificationItem(
                    "Motor",
                    $"{section}/{key}",
                    ok,
                    ok ? null : $"esperava {value}, encontrei {current ?? "(chave ausente)"}"));
            }
        }

        if (removedKeys is not null)
        {
            foreach (var removed in removedKeys)
            {
                var current = ReadIniValue(text, removed.Section, removed.Key);

                // ausente: removida, como deveria. presente com outro valor: o
                // usuario ajustou a mao e a remocao foi recusada de proposito.
                // presente com o valor da variante: a remocao falhou.
                var ok = current is null || !string.Equals(current, removed.Value, StringComparison.Ordinal);
                items.Add(new WriteVerificationItem(
                    "Motor",
                    $"{removed.Section}/{removed.Key}",
                    ok,
                    ok ? null : $"a chave deveria ter sido removida e ainda vale {removed.Value}"));
            }
        }

        return new WriteVerificationResult(items);
    }

    private static int? ReadIndex(string text, string option)
    {
        var located = JsonFieldLocator.Locate(text);
        if (!located.TryGetValue(option, out var occurrences) || occurrences.Count == 0)
        {
            return null;
        }

        var index = occurrences[0].Index;
        if (index is null)
        {
            return null;
        }

        var raw = text[index.Start..index.End];
        return int.TryParse(raw, out var value) ? value : null;
    }

    private static string? ReadIniValue(string text, string section, string key)
    {
        var lines = text.Split('\n');
        string? current = null;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                current = trimmed[1..^1];
                continue;
            }

            if (!string.Equals(current, section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            if (string.Equals(trimmed[..eq].Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(eq + 1)..].Trim();
            }
        }

        return null;
    }
}
