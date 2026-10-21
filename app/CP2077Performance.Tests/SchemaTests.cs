namespace CP2077Performance.Tests;

using CP2077Performance.Core.Schema;

internal static class SchemaTests
{
    private const string SchemaJson = """
    {
      "options": [
        {
          "group_name": "/video",
          "options": [
            {
              "name": "VolumetricCloudsQuality",
              "type": "string_list",
              "display_name": "UI-Settings-Video-VolumetricCloudsQuality",
              "is_dynamic": false,
              "default_value": "Medium",
              "values": [ "Off", "Medium", "High", "Ultra" ],
              "display_values": [ "a", "b", "c", "d" ]
            },
            {
              "name": "VSync",
              "type": "string_list",
              "is_dynamic": true,
              "default_value": "On"
            },
            {
              "name": "NoSpatialAudio",
              "type": "bool",
              "is_dynamic": false,
              "default_value": false
            },
            {
              "name": "Anisotropy",
              "type": "int",
              "is_dynamic": false,
              "default_value": 4
            }
          ]
        }
      ]
    }
    """;

    private static GameSchema BuildSchema()
    {
        var path = Path.Combine(Path.GetTempPath(), "cp2077-schema-" + Guid.NewGuid().ToString("N")[..8] + ".json");
        File.WriteAllText(path, SchemaJson);
        try
        {
            return GameSchemaReader.Read(new[] { path });
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("3.1 o leitor do esquema expoe tipo, valores aceitos e default", () =>
        {
            var schema = BuildSchema();
            Check.Equal(4, schema.Count, "quantidade de opcoes");

            Check.True(schema.TryGet("VolumetricCloudsQuality", out var list), "opcao de lista encontrada");
            Check.Equal("string_list", list.Type, "tipo da opcao de lista");
            Check.True(list.HasAcceptedValues, "a opcao de lista declara valores aceitos");
            Check.Equal(4, list.AcceptedValues.Count, "quantidade de valores aceitos");
            Check.Equal("\"Medium\"", list.DefaultValueText, "valor padrao em texto JSON");
            Check.Equal("Medium", list.DefaultString, "valor padrao como texto");

            Check.True(schema.TryGet("NoSpatialAudio", out var flag), "opcao booleana encontrada");
            Check.Equal(false, flag.DefaultBool, "valor padrao booleano");
            Check.False(flag.HasAcceptedValues, "booleano nao declara lista de valores");

            Check.True(schema.TryGet("VSync", out var dynamic), "opcao dinamica encontrada");
            Check.True(dynamic.IsDynamic, "marcada como dinamica");
            Check.False(dynamic.HasAcceptedValues, "opcao dinamica nao tem lista no esquema");
        });

        suite.Add("3.2 valor fora da lista de aceitos e erro", () =>
        {
            var schema = BuildSchema();
            var values = new Dictionary<string, string> { ["VolumetricCloudsQuality"] = "Low" };

            var result = ProfileValidator.ValidateMenuOptions(values, schema);

            Check.True(result.HasErrors, "deve haver erro");
            var issue = result.Errors.Single();
            Check.Equal("VolumetricCloudsQuality", issue.Name, "opcao reportada");
            Check.Contains(issue.Message, "Off, Medium, High, Ultra", "lista de aceitos no relatorio");
        });

        suite.Add("3.2 opcao inexistente no esquema e erro", () =>
        {
            var schema = BuildSchema();
            var values = new Dictionary<string, string> { ["OpcaoQueNaoExiste"] = "1" };

            var result = ProfileValidator.ValidateMenuOptions(values, schema);

            Check.True(result.HasErrors, "deve haver erro");
            Check.Contains(result.Errors.Single().Message, "nao existe no esquema", "mensagem do erro");
        });

        suite.Add("3.2 valor conforme nao gera erro", () =>
        {
            var schema = BuildSchema();
            var values = new Dictionary<string, string>
            {
                ["VolumetricCloudsQuality"] = "Off",
                ["NoSpatialAudio"] = "true",
            };

            var result = ProfileValidator.ValidateMenuOptions(values, schema);

            Check.False(result.HasErrors, "sem erros");
            Check.Equal(0, result.Issues.Count, "nenhuma issue");
        });

        suite.Add("3.2 opcao dinamica nao gera erro por nao ter lista", () =>
        {
            var schema = BuildSchema();
            var values = new Dictionary<string, string> { ["VSync"] = "qualquer-coisa" };

            var result = ProfileValidator.ValidateMenuOptions(values, schema);

            Check.False(result.HasErrors, "opcao dinamica nao pode ser reprovada");
        });
    }
}
