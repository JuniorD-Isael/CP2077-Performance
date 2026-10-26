namespace CP2077Performance.Tests;

using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;
using CP2077Performance.Core.Writing;

internal static class WritingTests
{
    /// <summary>
    /// Amostra com a mesma forma do arquivo do jogo: data[].options[], com
    /// name, type, value e, nas opcoes de lista, index e values.
    /// </summary>
    private const string SampleJson = """
    {
        "version": 140,
        "data": [
            {
                "group_name": "/video",
                "options": [
                    {
                        "name": "VolumetricCloudsQuality",
                        "type": "string_list",
                        "is_dynamic": false,
                        "value": "Medium",
                        "index": 1,
                        "default_index": 1,
                        "values": [ "Off", "Medium", "High", "Ultra" ]
                    },
                    {
                        "name": "NoSpatialAudio",
                        "type": "bool",
                        "is_dynamic": false,
                        "value": false
                    }
                ]
            },
            {
                "group_name": "/other",
                "options": [
                    {
                        "name": "CrowdDensity",
                        "type": "string_list",
                        "is_dynamic": false,
                        "value": "High",
                        "index": 2,
                        "default_index": 2,
                        "values": [ "Low", "Medium", "High" ]
                    },
                    {
                        "name": "CrowdDensity",
                        "type": "string_list",
                        "is_dynamic": false,
                        "value": "High",
                        "index": 2,
                        "default_index": 2,
                        "values": [ "Low", "Medium", "High" ]
                    }
                ]
            }
        ]
    }
    """;

    private const string SampleSchema = """
    {
      "options": [
        {
          "group_name": "/video",
          "options": [
            {
              "name": "VolumetricCloudsQuality",
              "type": "string_list",
              "default_value": "Medium",
              "values": [ "Off", "Medium", "High", "Ultra" ]
            },
            {
              "name": "NoSpatialAudio",
              "type": "bool",
              "default_value": false
            },
            {
              "name": "CrowdDensity",
              "type": "string_list",
              "default_value": "High",
              "values": [ "Low", "Medium", "High" ]
            }
          ]
        }
      ]
    }
    """;

    private static GameSchema BuildSchema()
    {
        var path = Path.Combine(Path.GetTempPath(), "cp2077-wschema-" + Guid.NewGuid().ToString("N")[..8] + ".json");
        File.WriteAllText(path, SampleSchema);
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
        suite.Add("4.1 o localizador acha o valor e o indice da opcao", () =>
        {
            var located = JsonFieldLocator.Locate(SampleJson);

            Check.True(located.ContainsKey("VolumetricCloudsQuality"), "opcao localizada");
            var fields = located["VolumetricCloudsQuality"];
            Check.Equal(1, fields.Count, "uma ocorrencia");

            var value = SampleJson[fields[0].Value.Start..fields[0].Value.End];
            Check.Equal("\"Medium\"", value, "texto do valor");

            Check.NotNull(fields[0].Index, "indice presente");
            var index = SampleJson[fields[0].Index!.Start..fields[0].Index!.End];
            Check.Equal("1", index, "texto do indice");
        });

        suite.Add("4.1 opcoes sem indice sao localizadas assim mesmo", () =>
        {
            var located = JsonFieldLocator.Locate(SampleJson);
            var fields = located["NoSpatialAudio"];
            Check.Equal(1, fields.Count, "uma ocorrencia");
            Check.Null(fields[0].Index, "sem indice");
        });

        suite.Add("4.1 opcao que aparece duas vezes tem as duas ocorrencias", () =>
        {
            var located = JsonFieldLocator.Locate(SampleJson);
            Check.Equal(2, located["CrowdDensity"].Count, "duas ocorrencias");
        });

        suite.Add("4.2 a escrita altera apenas o campo visado", () =>
        {
            var schema = BuildSchema();
            var desired = new Dictionary<string, ProfileValue>
            {
                ["NoSpatialAudio"] = ProfileValue.FromBoolean(true),
            };

            var result = new MenuOptionsWriter().Plan(SampleJson, desired, schema);

            Check.Equal(1, result.Changes.Count, "uma mudanca");
            Check.Contains(result.NewText, "\"value\": true", "valor novo presente");

            // prova byte a byte: substituindo o campo, o resto do arquivo e identico
            var expected = SampleJson.Replace("\"value\": false", "\"value\": true");
            Check.Equal(expected, result.NewText, "apenas o campo de valor mudou");
        });

        suite.Add("4.2 as duas ocorrencias de uma opcao repetida sao alteradas", () =>
        {
            var schema = BuildSchema();
            var desired = new Dictionary<string, ProfileValue>
            {
                ["CrowdDensity"] = ProfileValue.FromString("Low"),
            };

            var result = new MenuOptionsWriter().Plan(SampleJson, desired, schema);

            Check.Equal(2, result.Changes.Count, "duas mudancas");
            Check.Equal(2, result.NewText.Split('\n').Count(l => l.Contains("\"value\": \"Low\"")), "duas ocorrencias com o valor novo");
        });

        suite.Add("4.3 valor e indice sao reconciliados quando o valor muda", () =>
        {
            var schema = BuildSchema();
            var desired = new Dictionary<string, ProfileValue>
            {
                ["VolumetricCloudsQuality"] = ProfileValue.FromString("Off"),
            };

            var result = new MenuOptionsWriter().Plan(SampleJson, desired, schema);

            var change = result.Changes.Single();
            Check.Equal("\"Medium\"", change.Before, "valor anterior");
            Check.Equal("\"Off\"", change.After, "valor novo");
            Check.True(change.IndexChanged, "o indice mudou");
            Check.Equal("1", change.IndexBefore, "indice anterior");
            Check.Equal("0", change.IndexAfter, "indice novo, 0 = Off");
        });

        suite.Add("4.3 o indice desatualizado e corrigido mesmo com o valor certo", () =>
        {
            var schema = BuildSchema();

            // o valor ja e o desejado, mas o indice aponta para outro item
            var stale = SampleJson.Replace("\"index\": 1,", "\"index\": 2,");

            var desired = new Dictionary<string, ProfileValue>
            {
                ["VolumetricCloudsQuality"] = ProfileValue.FromString("Medium"),
            };

            var result = new MenuOptionsWriter().Plan(stale, desired, schema);

            var change = result.Changes.Single();
            Check.Equal("\"Medium\"", change.Before, "valor inalterado");
            Check.Equal("\"Medium\"", change.After, "valor inalterado");
            Check.True(change.IndexChanged, "o indice foi corrigido");
            Check.Equal("2", change.IndexBefore, "indice desatualizado");
            Check.Equal("1", change.IndexAfter, "indice corrigido");
        });

        suite.Add("4.4 o escritor de ini preserva comentarios e ordem de secoes", () =>
        {
            var ini = """
            [Developer/FeatureToggles]

            ;Makes (Neon)Lights bloom. Eats 3-5 fps
            Bloom = True
            Antialiasing = True

            ;Don't disable. (breaks lightmaps)
            DistantShadows = True

            [Rendering]
            UseSkinningLOD = True
            """;

            var desired = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["Developer/FeatureToggles"] = new Dictionary<string, string> { ["Bloom"] = "False" },
                ["Rendering"] = new Dictionary<string, string> { ["RainMapProxySorting"] = "False" },
                ["NovaSecao"] = new Dictionary<string, string> { ["ChaveNova"] = "False" },
            };

            var result = new EngineIniWriter().Plan(ini, desired);

            Check.Contains(result.NewText, ";Makes (Neon)Lights bloom. Eats 3-5 fps", "comentario do motor preservado");
            Check.Contains(result.NewText, ";Don't disable. (breaks lightmaps)", "segundo comentario preservado");
            Check.Contains(result.NewText, "Bloom = False", "chave alterada");
            Check.Contains(result.NewText, "RainMapProxySorting = False", "chave acrescentada na secao");
            Check.Contains(result.NewText, "[NovaSecao]", "secao nova criada");

            // a ordem das secoes existentes nao muda
            var antes = new List<int>();
            var depois = new List<int>();
            for (var i = 0; i < ini.Split('\n').Length; i++) if (ini.Split('\n')[i].StartsWith("[")) antes.Add(i);
            for (var i = 0; i < result.NewText.Split('\n').Length; i++) if (result.NewText.Split('\n')[i].StartsWith("[")) depois.Add(i);
            Check.True(depois.Count >= antes.Count, "as secoes antigas continuam la");
            Check.True(result.NewText.IndexOf("[Developer/FeatureToggles]", StringComparison.Ordinal)
                     < result.NewText.IndexOf("[Rendering]", StringComparison.Ordinal), "ordem das secoes preservada");
        });

        suite.Add("4.4 chaves preservadas nao sao tocadas", () =>
        {
            var ini = "[Sec]\nDistantShadows = True\n";
            var desired = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["Sec"] = new Dictionary<string, string> { ["DistantShadows"] = "False" },
            };

            var preserved = new HashSet<string>(StringComparer.Ordinal) { "Sec\0DistantShadows" };
            var result = new EngineIniWriter().Plan(ini, desired, preserved);

            Check.Equal(0, result.Changes.Count, "nenhuma mudanca");
            Check.Contains(result.NewText, "DistantShadows = True", "valor preservado");
        });

        suite.Add("4.5 o escritor de patches altera so os patches declarados", () =>
        {
            var config = """
            {
                "patches": {
                    "disable_antialiasing": true,
                    "disable_async_compute": false,
                    "disable_vignette": false
                }
            }
            """;

            var desired = new Dictionary<string, bool>
            {
                ["disable_async_compute"] = true,
                ["disable_vignette"] = true,
            };

            var result = new CetConfigWriter().Plan(config, desired);

            Check.Equal(2, result.Changes.Count, "duas mudancas");
            Check.Contains(result.NewText, "\"disable_antialiasing\": true", "patch nao visado preservado");
            Check.Contains(result.NewText, "\"disable_async_compute\": true", "patch alterado");
        });

        suite.Add("4.6 a verificacao pos-escrita aponta divergencia", () =>
        {
            var schema = BuildSchema();
            var desired = new Dictionary<string, ProfileValue>
            {
                ["NoSpatialAudio"] = ProfileValue.FromBoolean(true),
            };

            // simula um arquivo em que a escrita nao aconteceu
            var verification = WriteVerifier.VerifyMenu(SampleJson, desired, schema);

            Check.False(verification.Ok, "deve falhar");
            var failure = verification.Failures.Single();
            Check.Equal("NoSpatialAudio", failure.Target, "opcao reportada");
        });

        suite.Add("4.6 a verificacao confirma um arquivo escrito corretamente", () =>
        {
            var schema = BuildSchema();
            var desired = new Dictionary<string, ProfileValue>
            {
                ["VolumetricCloudsQuality"] = ProfileValue.FromString("Off"),
            };

            var written = new MenuOptionsWriter().Plan(SampleJson, desired, schema).NewText;
            var verification = WriteVerifier.VerifyMenu(written, desired, schema);

            Check.True(verification.Ok, "deve passar");
            Check.Equal(0, verification.Failures.Count(), "nenhuma falha");
        });
    }
}
