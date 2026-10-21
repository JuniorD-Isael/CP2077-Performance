using System.Text.Json;

namespace CP2077Performance.Core.Schema;

/// <summary>
/// Descricao de uma opcao do menu, conforme declarada pelo proprio jogo em
/// <c>r6/config/settings/options.json</c>.
/// </summary>
public sealed class GameOptionSchema
{
    public required string Name { get; init; }

    public string Type { get; init; } = "";

    public string? DisplayName { get; init; }

    /// <summary>
    /// Opcoes com <c>is_dynamic</c> tem a lista de valores montada em runtime:
    /// nao ha lista de aceitos no esquema, e nada pode ser conferido contra ela.
    /// </summary>
    public bool IsDynamic { get; init; }

    /// <summary>Lista de valores aceitos, quando o esquema declara.</summary>
    public IReadOnlyList<string> AcceptedValues { get; init; } = Array.Empty<string>();

    public bool HasAcceptedValues => AcceptedValues.Count > 0;

    /// <summary>Valor padrao declarado pelo jogo, em texto JSON.</summary>
    public string? DefaultValueText { get; init; }

    public bool? DefaultBool => DefaultValueText switch
    {
        "true" => true,
        "false" => false,
        _ => null,
    };

    /// <summary>Valor padrao como texto, sem as aspas de um valor textual.</summary>
    public string? DefaultString => DefaultValueText switch
    {
        null => null,
        "\"" => null,
        _ when DefaultValueText.StartsWith('"') && DefaultValueText.EndsWith('"')
            => DefaultValueText[1..^1],
        _ => DefaultValueText,
    };

    public bool Accepts(string value) =>
        !HasAcceptedValues || AcceptedValues.Contains(value, StringComparer.Ordinal);

    internal static GameOptionSchema FromJson(JsonElement element)
    {
        var name = element.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
        var display = element.TryGetProperty("display_name", out var d) ? d.GetString() : null;
        var isDynamic = element.TryGetProperty("is_dynamic", out var dyn) && dyn.ValueKind == JsonValueKind.True;

        var accepted = new List<string>();
        if (element.TryGetProperty("values", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in values.EnumerateArray())
            {
                if (v.ValueKind == JsonValueKind.String)
                {
                    accepted.Add(v.GetString()!);
                }
            }
        }

        string? defaultText = null;
        if (element.TryGetProperty("default_value", out var def))
        {
            defaultText = def.ValueKind == JsonValueKind.Null ? null : def.GetRawText();
        }

        return new GameOptionSchema
        {
            Name = name,
            Type = type,
            DisplayName = display,
            IsDynamic = isDynamic,
            AcceptedValues = accepted,
            DefaultValueText = defaultText,
        };
    }
}
