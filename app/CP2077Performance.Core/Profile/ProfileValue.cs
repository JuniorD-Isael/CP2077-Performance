using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CP2077Performance.Core.Profile;

/// <summary>
/// Valor de configuracao, preservando o texto JSON exato.
/// </summary>
/// <remarks>
/// O texto exato importa: o jogo grava <c>5.0</c> e nao <c>5</c>, e um
/// round-trip de serializacao mudaria a forma dos numeros. Guardar o texto
/// permite reescrever um valor sem tocar no resto do arquivo.
/// </remarks>
public sealed class ProfileValue : IEquatable<ProfileValue>
{
    public ProfileValue(string json) => Json = json ?? throw new ArgumentNullException(nameof(json));

    /// <summary>Texto JSON do valor, como ele deve aparecer no arquivo.</summary>
    public string Json { get; }

    public static ProfileValue FromString(string value) =>
        new(JsonSerializer.Serialize(value));

    public static ProfileValue FromBoolean(bool value) => new(value ? "true" : "false");

    public static ProfileValue FromNumber(double value) =>
        new(value.ToString("R", CultureInfo.InvariantCulture));

    public static ProfileValue FromJson(JsonElement element) => new(element.GetRawText());

    public static ProfileValue FromJson(JsonNode node) => new(node.ToJsonString());

    public string? AsString() =>
        Json.StartsWith('"') ? JsonSerializer.Deserialize<string>(Json) : null;

    public bool? AsBoolean() => Json switch
    {
        "true" => true,
        "false" => false,
        _ => null,
    };

    public double? AsNumber() =>
        double.TryParse(Json, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    /// <summary>Texto para exibicao ao usuario.</summary>
    public string Display => AsString() ?? Json;

    public bool Equals(ProfileValue? other) =>
        other is not null && string.Equals(Json, other.Json, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ProfileValue);

    public override int GetHashCode() => Json.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Json;
}
