namespace CP2077Performance.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Validador de JSON Schema, para o subconjunto que o schema do perfil usa:
/// <c>type</c>, <c>required</c>, <c>properties</c>, <c>additionalProperties</c>,
/// <c>items</c>, <c>minProperties</c>, <c>minLength</c>, <c>minimum</c> e
/// <c>pattern</c>.
/// </summary>
/// <remarks>
/// O projeto nao usa nenhuma dependencia externa, e o schema do perfil precisa
/// ser conferido a cada build. Um validador proprio de poucas dezenas de
/// linhas resolve isso sem quebrar a regra.
/// </remarks>
internal static class SchemaFormatValidator
{
    public static IReadOnlyList<string> Validate(JsonNode? instance, JsonNode? schema, string path = "$")
    {
        var errors = new List<string>();
        Validate(instance, schema, path, errors);
        return errors;
    }

    private static void Validate(JsonNode? instance, JsonNode? schema, string path, List<string> errors)
    {
        if (schema is not JsonObject s)
        {
            return;
        }

        if (s["type"] is JsonNode typeNode)
        {
            var types = typeNode is JsonArray array
                ? array.Select(n => n?.GetValue<string>() ?? "").ToList()
                : new List<string> { typeNode.GetValue<string>() };

            if (!types.Any(t => MatchesType(instance, t)))
            {
                errors.Add($"{path}: esperava tipo {string.Join(" ou ", types)}, obtive {KindOf(instance)}");
                return;
            }
        }

        switch (instance)
        {
            case JsonObject obj:
                ValidateObject(obj, s, path, errors);
                break;
            case JsonArray array:
                ValidateArray(array, s, path, errors);
                break;
            case JsonValue value:
                ValidateValue(value, s, path, errors);
                break;
        }
    }

    private static void ValidateObject(JsonObject obj, JsonObject schema, string path, List<string> errors)
    {
        if (schema["required"] is JsonArray required)
        {
            foreach (var item in required)
            {
                var name = item?.GetValue<string>() ?? "";
                if (!obj.ContainsKey(name))
                {
                    errors.Add($"{path}: falta a propriedade obrigatoria '{name}'");
                }
            }
        }

        var properties = schema["properties"] as JsonObject;

        if (schema["minProperties"] is JsonValue minProperties &&
            minProperties.TryGetValue<int>(out var min) &&
            obj.Count < min)
        {
            errors.Add($"{path}: esperava ao menos {min} propriedade(s), tem {obj.Count}");
        }

        foreach (var (name, node) in obj)
        {
            var child = $"{path}.{name}";

            if (properties is not null && properties[name] is JsonObject propertySchema)
            {
                Validate(node, propertySchema, child, errors);
                continue;
            }

            if (schema["additionalProperties"] is JsonObject additional)
            {
                Validate(node, additional, child, errors);
                continue;
            }

            if (schema["additionalProperties"] is JsonValue additionalValue &&
                additionalValue.TryGetValue<bool>(out var allowed) &&
                !allowed)
            {
                errors.Add($"{path}: a propriedade '{name}' nao e permitida aqui");
            }
        }
    }

    private static void ValidateArray(JsonArray array, JsonObject schema, string path, List<string> errors)
    {
        if (schema["items"] is not JsonObject items)
        {
            return;
        }

        for (var i = 0; i < array.Count; i++)
        {
            Validate(array[i], items, $"{path}[{i}]", errors);
        }
    }

    private static void ValidateValue(JsonValue value, JsonObject schema, string path, List<string> errors)
    {
        if (value.TryGetValue<string>(out var text))
        {
            if (schema["minLength"] is JsonValue minLength &&
                minLength.TryGetValue<int>(out var min) &&
                text.Length < min)
            {
                errors.Add($"{path}: texto com {text.Length} caractere(s), esperava ao menos {min}");
            }

            if (schema["pattern"] is JsonValue pattern &&
                pattern.TryGetValue<string>(out var expression) &&
                !Regex.IsMatch(text, expression))
            {
                errors.Add($"{path}: '{text}' nao casa com o padrao '{expression}'");
            }
        }

        if (schema["minimum"] is JsonValue minimum &&
            minimum.TryGetValue<double>(out var floor) &&
            value.TryGetValue<double>(out var number) &&
            number < floor)
        {
            errors.Add($"{path}: {number} e menor que o minimo {floor}");
        }
    }

    private static bool MatchesType(JsonNode? instance, string type) => type switch
    {
        "object" => instance is JsonObject,
        "array" => instance is JsonArray,
        "string" => instance is JsonValue v && v.TryGetValue<string>(out _),
        "boolean" => instance is JsonValue b && (b.TryGetValue<bool>(out _)),
        "null" => instance is null || (instance is JsonValue n && n.TryGetValue<object?>(out var o) && o is null),
        "number" => instance is JsonValue num && num.TryGetValue<double>(out _),
        "integer" => instance is JsonValue i && i.TryGetValue<double>(out var d) && Math.Abs(d % 1) < double.Epsilon,
        _ => true,
    };

    private static string KindOf(JsonNode? instance) => instance switch
    {
        null => "nulo",
        JsonObject => "objeto",
        JsonArray => "array",
        JsonValue v when v.TryGetValue<string>(out _) => "texto",
        JsonValue v when v.TryGetValue<bool>(out _) => "booleano",
        JsonValue => "numero",
        _ => "desconhecido",
    };
}
