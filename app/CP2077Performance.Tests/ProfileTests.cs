namespace CP2077Performance.Tests;

using CP2077Performance.Core.Application;
using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;

internal static class ProfileTests
{
    private const string ProfileJson = """
    {
      "name": "Low-Spec 30 FPS",
      "version": "0.1.1-alpha",
      "userIni": {
        "_comment": "cvars do motor",
        "Developer/FeatureToggles": { "Bloom": "False" },
        "Rendering/AsyncCompute": { "Enable": "False" }
      },
      "preserve": {
        "userIni": { "Developer/FeatureToggles": { "DistantShadows": "True" } }
      },
      "userSettings": {
        "values": {
          "VolumetricCloudsQuality": "Off",
          "NoSpatialAudio": true,
          "CrowdDensity": "Low"
        }
      },
      "cetConfig": {
        "patches": { "disable_async_compute": true }
      },
      "variants": {
        "hudMinimal": {
          "label": "HUD minima",
          "userSettings": {
            "values": {
              "minimap": false,
              "quest_tracker": false
            }
          }
        }
      }
    }
    """;

    private const string SchemaJson = """
    {
      "options": [
        {
          "group_name": "/all",
          "options": [
            { "name": "VolumetricCloudsQuality", "type": "string_list", "default_value": "Medium",
              "values": [ "Off", "Medium", "High", "Ultra" ] },
            { "name": "NoSpatialAudio", "type": "bool", "default_value": false },
            { "name": "CrowdDensity", "type": "string_list", "default_value": "High",
              "values": [ "Low", "Medium", "High" ] },
            { "name": "minimap", "type": "bool", "default_value": true },
            { "name": "quest_tracker", "type": "bool", "default_value": true }
          ]
        }
      ]
    }
    """;

    private static GameSchema BuildSchema()
    {
        var path = Path.Combine(Path.GetTempPath(), "cp2077-pschema-" + Guid.NewGuid().ToString("N")[..8] + ".json");
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
        suite.Add("5.2 o perfil e lido com suas tres camadas e variantes", () =>
        {
            var profile = ProfileModel.Parse(ProfileJson);

            Check.Equal("Low-Spec 30 FPS", profile.Name, "nome");
            Check.Equal("0.1.1-alpha", profile.Version, "versao");
            Check.Equal(2, profile.UserIni.Count, "secoes de cvar");
            Check.Equal(3, profile.UserSettingsValues.Count, "opcoes de menu");
            Check.Equal(1, profile.CetPatches.Count, "patches do CET");
            Check.Equal(1, profile.Variants.Count, "variantes");
            Check.Equal("HUD minima", profile.Variants["hudMinimal"].Label, "rotulo da variante");
            Check.Equal(2, profile.Variants["hudMinimal"].UserSettingsValues.Count, "opcoes da variante");
        });

        suite.Add("5.2 as chaves preservadas sao identificadas", () =>
        {
            var profile = ProfileModel.Parse(ProfileJson);
            Check.True(
                profile.PreservedUserIni.ContainsKey("Developer/FeatureToggles"),
                "secao preservada");
            Check.True(
                profile.PreservedUserIni["Developer/FeatureToggles"].Contains("DistantShadows"),
                "chave preservada");
        });

        suite.Add("5.3 a variante ligada tem precedencia sobre o perfil base", () =>
        {
            var profile = ProfileModel.Parse(ProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string> { "hudMinimal" }, schema);

            Check.Equal("false", effective.MenuValues["minimap"].Json, "valor da variante");
            Check.Equal("\"Off\"", effective.MenuValues["VolumetricCloudsQuality"].Json, "perfil base preservado");
            Check.Equal(0, effective.RestoreToDefault.Count, "nada a restaurar");
        });

        suite.Add("5.4 a variante desligada devolve as opcoes ao padrao do esquema", () =>
        {
            var profile = ProfileModel.Parse(ProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string>(), schema);

            Check.Equal(2, effective.RestoreToDefault.Count, "opcoes a restaurar");
            Check.Equal("true", effective.RestoreToDefault["minimap"].Json, "minimap volta ao padrao do esquema");
            Check.Equal("true", effective.RestoreToDefault["quest_tracker"].Json, "quest_tracker volta ao padrao");
            Check.False(effective.MenuValues.ContainsKey("minimap"), "a opcao da variante nao e aplicada");
        });

        suite.Add("5.5 a simulacao lista as alteracoes sem escrever nada", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-sim-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var settingsPath = Path.Combine(root, "UserSettings.json");
                var iniPath = Path.Combine(root, @"engine\config\platform\pc\user.ini");
                Directory.CreateDirectory(Path.GetDirectoryName(iniPath)!);
                File.WriteAllText(settingsPath, "{\n  \"data\": []\n}");
                File.WriteAllText(iniPath, "[Sec]\n");

                var before = File.ReadAllBytes(settingsPath);

                var profile = ProfileModel.Parse(ProfileJson);
                var schema = BuildSchema();
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root);
                var applier = new ProfileApplier(
                    locator,
                    new CP2077Performance.Core.Backup.BackupService(Path.Combine(root, "backups")));

                var plan = applier.Plan(profile, new HashSet<string>(), schema, null);

                Check.True(plan.CanApply, "plano aplicavel");
                Check.Equal(2, plan.IniChanges.Count, "duas mudancas de cvar");

                // nada foi escrito
                Check.Equal(before.Length, File.ReadAllBytes(settingsPath).Length, "arquivo intacto");
                Check.Equal("[Sec]\n", File.ReadAllText(iniPath), "ini intacto");
                Check.False(Directory.Exists(Path.Combine(root, "backups")), "nenhum backup criado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("5.6 o registro guarda as alteracoes e a origem dos valores", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-log-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var log = new ChangeLog(root);
                log.Append(new ChangeLogEntry(
                    DateTime.Now,
                    new[] { "UserSettings.json" },
                    new[] { "minimap: true -> false (Variant)" }));

                var entries = log.Read();
                Check.Equal(1, entries.Count, "uma entrada");
                Check.Equal("UserSettings.json", entries[0].Files[0], "arquivo registrado");
                Check.Contains(entries[0].Details[0], "Variant", "origem do valor registrada");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });
    }
}
