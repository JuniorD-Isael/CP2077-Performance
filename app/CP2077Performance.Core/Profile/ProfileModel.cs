using System.Text.Json;
using System.Text.Json.Nodes;

namespace CP2077Performance.Core.Profile;

/// <summary>Hardware de referencia do perfil.</summary>
public sealed class ProfileTarget
{
    public string Cpu { get; init; } = "";

    public string Gpu { get; init; } = "";

    public string Ram { get; init; } = "";

    public int GoalFps { get; init; }

    public string? Note { get; init; }

    public override string ToString() =>
        string.Join(" / ", new[] { Cpu, Gpu, Ram }.Where(s => !string.IsNullOrEmpty(s)))
        + (GoalFps > 0 ? $" / {GoalFps} FPS" : "");
}

public sealed class ProfileVariant
{    public string Label { get; init; } = "";

    public string? Description { get; init; }

    public IReadOnlyDictionary<string, ProfileValue> UserSettingsValues { get; init; } =
        new Dictionary<string, ProfileValue>();
}

/// <summary>
/// Perfil de performance: as tres camadas de configuracao do jogo.
/// </summary>
/// <remarks>
/// O modelo guarda o documento JSON original e expoe visoes tipadas sobre ele.
/// Assim, campos que o aplicativo nao conhece sobrevivem a ida e volta, e o
/// perfil do usuario continua editavel sem perder nada.
/// </remarks>
public sealed class ProfileModel
{
    private ProfileModel(JsonObject root) => Root = root;

    public JsonObject Root { get; }

    public static ProfileModel Parse(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new FormatException("o perfil precisa ser um objeto JSON");
        return new ProfileModel(node);
    }

    public string ToJson() =>
        Root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    public string Name => Root["name"]?.GetValue<string>() ?? "";

    public string Version => Root["version"]?.GetValue<string>() ?? "";

    public string? Description => Root["description"]?.GetValue<string>();

    /// <summary>Hardware de referencia para o qual o perfil foi calibrado.</summary>
    public ProfileTarget Target
    {
        get
        {
            var target = Root["target"];
            return new ProfileTarget
            {
                Cpu = target?["cpu"]?.GetValue<string>() ?? "",
                Gpu = target?["gpu"]?.GetValue<string>() ?? "",
                Ram = target?["ram"]?.GetValue<string>() ?? "",
                GoalFps = target?["goal_fps"]?.GetValue<int>() ?? 0,
                Note = target?["note"]?.GetValue<string>(),
            };
        }
    }

    // ---------------------------------------------------------------- cvars

    /// <summary>Cvars do motor, por secao do user.ini.</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> UserIni
    {
        get
        {
            var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            if (Root["userIni"] is not JsonObject sections)
            {
                return result;
            }

            foreach (var (section, node) in sections)
            {
                if (section.StartsWith('_') || node is not JsonObject keys)
                {
                    continue;
                }

                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (key, value) in keys)
                {
                    if (key.StartsWith('_'))
                    {
                        continue;
                    }

                    map[key] = value is null ? "" : JsonValueOf(value);
                }

                result[section] = map;
            }

            return result;
        }
    }

    /// <summary>Chaves de cvar que nunca podem ser alteradas, por secao.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> PreservedUserIni
    {
        get
        {
            var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);
            if (Root["preserve"]?["userIni"] is not JsonObject sections)
            {
                return result;
            }

            foreach (var (section, node) in sections)
            {
                if (node is not JsonObject keys)
                {
                    continue;
                }

                result[section] = keys.Select(k => k.Key).ToHashSet(StringComparer.Ordinal);
            }

            return result;
        }
    }

    // ---------------------------------------------------------------- menu

    public IReadOnlyDictionary<string, ProfileValue> UserSettingsValues =>
        ReadValues(Root["userSettings"]?["values"] as JsonObject);

    // ---------------------------------------------------------------- CET

    public IReadOnlyDictionary<string, bool> CetPatches
    {
        get
        {
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (Root["cetConfig"]?["patches"] is not JsonObject patches)
            {
                return result;
            }

            foreach (var (name, node) in patches)
            {
                if (name.StartsWith('_'))
                {
                    continue;
                }

                if (node is JsonValue v && v.TryGetValue<bool>(out var b))
                {
                    result[name] = b;
                }
            }

            return result;
        }
    }

    // ---------------------------------------------------------------- variantes

    public IReadOnlyDictionary<string, ProfileVariant> Variants
    {
        get
        {
            var result = new Dictionary<string, ProfileVariant>(StringComparer.Ordinal);
            if (Root["variants"] is not JsonObject variants)
            {
                return result;
            }

            foreach (var (id, node) in variants)
            {
                if (node is not JsonObject obj)
                {
                    continue;
                }

                result[id] = new ProfileVariant
                {
                    Label = obj["label"]?.GetValue<string>() ?? id,
                    Description = obj["description"]?.GetValue<string>(),
                    UserSettingsValues = ReadValues(obj["userSettings"]?["values"] as JsonObject),
                };
            }

            return result;
        }
    }

    // ---------------------------------------------------------------- edicao

    /// <summary>Define o valor de uma opcao de menu do perfil base.</summary>
    public void SetMenuValue(string option, ProfileValue value)
    {
        var values = Root["userSettings"]?["values"] as JsonObject;
        if (values is null)
        {
            values = new JsonObject();
            if (Root["userSettings"] is JsonObject userSettings)
            {
                userSettings["values"] = values;
            }
            else
            {
                Root["userSettings"] = new JsonObject { ["values"] = values };
            }
        }

        values[option] = JsonNode.Parse(value.Json);
    }

    /// <summary>Define o valor de uma opcao dentro de uma variante.</summary>
    public void SetVariantValue(string variantId, string option, ProfileValue value)
    {
        if (Root["variants"] is not JsonObject variants)
        {
            variants = new JsonObject();
            Root["variants"] = variants;
        }

        if (variants[variantId] is not JsonObject variant)
        {
            variant = new JsonObject();
            variants[variantId] = variant;
        }

        if (variant["userSettings"] is not JsonObject userSettings)
        {
            userSettings = new JsonObject();
            variant["userSettings"] = userSettings;
        }

        if (userSettings["values"] is not JsonObject values)
        {
            values = new JsonObject();
            userSettings["values"] = values;
        }

        values[option] = JsonNode.Parse(value.Json);
    }

    public void SetVersion(string version)
    {
        Root["version"] = version;
    }

    private static IReadOnlyDictionary<string, ProfileValue> ReadValues(JsonObject? values)
    {
        var result = new Dictionary<string, ProfileValue>(StringComparer.Ordinal);
        if (values is null)
        {
            return result;
        }

        foreach (var (key, node) in values)
        {
            if (key.StartsWith('_') || node is null)
            {
                continue;
            }

            result[key] = ProfileValue.FromJson(node);
        }

        return result;
    }

    private static string JsonValueOf(JsonNode node) =>
        node is JsonValue value && value.TryGetValue<string>(out var s) ? s : node.ToJsonString();
}
