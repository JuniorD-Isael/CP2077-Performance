namespace CP2077Performance.Tests;

using CP2077Performance.Core.Application;
using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;
using CP2077Performance.Core.Writing;

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
        "testVariant": {
          "label": "Variante de teste",
          "userSettings": {
            "values": {
              "CrowdDensity": "Medium",
              "NoSpatialAudio": false
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
            { "name": "WindowMode", "type": "string_list", "is_dynamic": true }
          ]
        }
      ]
    }
    """;

    /// <summary>
    /// Perfil com uma variante que alcanca as duas camadas e outra que so
    /// alcanca o menu, como os perfis gravados no formato anterior.
    /// </summary>
    private const string TwoLayerProfileJson = """
    {
      "name": "Duas camadas",
      "version": "0.2.0",
      "userIni": {
        "Rendering/AsyncCompute": { "Enable": "False" }
      },
      "userSettings": {
        "values": { "NoSpatialAudio": true }
      },
      "variants": {
        "testVariant": {
          "label": "Variante de teste",
          "summary": "Variante de teste que alcanca as duas camadas.",
          "highlights": [ "opcao A ajustada", "opcao B ajustada" ],
          "userIni": {
            "Rendering/AsyncCompute": { "Enable": "True", "HairClears": "False" }
          },
          "userSettings": {
            "values": { "NoSpatialAudio": false, "CrowdDensity": "Low" }
          }
        },
        "somenteMenu": {
          "label": "Somente menu",
          "userSettings": {
            "values": { "CrowdDensity": "Low" }
          }
        }
      }
    }
    """;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SemChaves =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

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
            Check.Equal("Variante de teste", profile.Variants["testVariant"].Label, "rotulo da variante");
            Check.Equal(2, profile.Variants["testVariant"].UserSettingsValues.Count, "opcoes da variante");
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

            var effective = VariantResolver.Resolve(profile, new HashSet<string> { "testVariant" }, schema);

            Check.Equal("\"Medium\"", effective.MenuValues["CrowdDensity"].Json, "valor da variante");
            Check.Equal("\"Off\"", effective.MenuValues["VolumetricCloudsQuality"].Json, "perfil base preservado");
            Check.Equal(0, effective.RestoreToDefault.Count, "nada a restaurar");
        });

        suite.Add("5.4 a variante desligada devolve as opcoes ao padrao do esquema", () =>
        {
            var profile = ProfileModel.Parse(TwoLayerProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string>(), schema);

            Check.Equal(2, effective.RestoreToDefault.Count, "opcoes a restaurar");
            Check.Equal("false", effective.RestoreToDefault["NoSpatialAudio"].Json, "NoSpatialAudio volta ao padrao do esquema");
            Check.Equal("\"High\"", effective.RestoreToDefault["CrowdDensity"].Json, "CrowdDensity volta ao padrao");
            Check.False(effective.MenuValues.ContainsKey("CrowdDensity"), "a opcao exclusiva da variante nao e aplicada");
        });

        suite.Add("5.5 o plano lista as alteracoes sem escrever nada", () =>
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
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root, root);
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
                    new[] { "TestOptA: true -> false (Variant)" }));

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

        // ------------------------------------------------------------ duas camadas

        suite.Add("5.7 a variante le as duas camadas e o resumo em linguagem corrente", () =>
        {
            var profile = ProfileModel.Parse(TwoLayerProfileJson);

            Check.Equal(2, profile.Variants.Count, "variantes");

            var full = profile.Variants["testVariant"];
            Check.Equal("Variante de teste que alcanca as duas camadas.", full.Summary, "resumo");
            Check.Equal(2, full.Highlights.Count, "destaques");
            Check.Equal(1, full.UserIni.Count, "secoes de cvar da variante");
            Check.Equal(2, full.UserIni["Rendering/AsyncCompute"].Count, "chaves de cvar da variante");
            Check.Equal(2, full.UserSettingsValues.Count, "opcoes de menu da variante");

            // uma variante gravada no formato anterior continua sendo lida
            var menuOnly = profile.Variants["somenteMenu"];
            Check.Equal(1, menuOnly.UserSettingsValues.Count, "opcoes da variante");
            Check.Equal(0, menuOnly.UserIni.Count, "a variante sem cvar nao alcanca o motor");
            Check.Null(menuOnly.Summary, "variante sem resumo");
            Check.Equal(0, menuOnly.Highlights.Count, "variante sem destaques");
        });

        suite.Add("5.8 a variante ligada substitui a chave de cvar do perfil base", () =>
        {
            var profile = ProfileModel.Parse(TwoLayerProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string> { "testVariant" }, schema);

            Check.Equal(
                "True",
                effective.UserIniValues["Rendering/AsyncCompute"]["Enable"],
                "a chave da variante substitui a do perfil base");
            Check.Equal(
                "False",
                effective.UserIniValues["Rendering/AsyncCompute"]["HairClears"],
                "a variante acrescenta a chave que o perfil base nao tem");
            Check.Equal(0, effective.RemovableUserIniKeys.Count, "ligada, nada a remover");
        });

        suite.Add("5.9 a variante sem cvar nao altera a camada do motor", () =>
        {
            var profile = ProfileModel.Parse(TwoLayerProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string> { "somenteMenu" }, schema);

            Check.Equal(
                "False",
                effective.UserIniValues["Rendering/AsyncCompute"]["Enable"],
                "o perfil base continua no motor");
            Check.False(
                effective.UserIniValues["Rendering/AsyncCompute"].ContainsKey("HairClears"),
                "a outra variante nao vazou para o motor");
        });

        suite.Add("5.10 desligar a variante remove a chave de cvar que ela acrescentou", () =>
        {
            var profile = ProfileModel.Parse(TwoLayerProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string>(), schema);

            // Enable pertence ao perfil base: nao e removida
            Check.False(
                effective.RemovableUserIniKeys.Any(k => k.Key == "Enable"),
                "a chave do perfil base nao e removida");

            // HairClears pertence so a variante: e removida
            var removable = effective.RemovableUserIniKeys.FirstOrDefault(k => k.Key == "HairClears");
            Check.NotNull(removable, "a chave da variante foi marcada para remocao");
            Check.Equal("Rendering/AsyncCompute", removable!.Section, "secao da chave");
            Check.Equal("False", removable.Value, "valor esperado da chave");
        });

        suite.Add("5.11 a remocao da chave acontece quando o valor ainda e o da variante", () =>
        {
            var ini = "[Rendering/AsyncCompute]\nEnable = False\nHairClears = False\n";
            var removable = new List<UserIniKey>
            {
                new("Rendering/AsyncCompute", "HairClears", "False"),
            };

            var result = new EngineIniWriter().Plan(ini, SemChaves, null, removable);

            Check.DoesNotContain(result.NewText, "HairClears", "a chave foi removida");
            Check.Contains(result.NewText, "Enable = False", "as demais chaves ficaram");
            Check.Equal(1, result.Changes.Count, "uma mudanca");
            Check.Null(result.Changes[0].After, "a mudanca e uma remocao");
            Check.Equal("False", result.Changes[0].Before, "valor anterior");
            Check.Equal(0, result.SkippedRemovals.Count, "nada a reportar");
        });

        suite.Add("5.12 a remocao nao apaga o que o usuario ajustou a mao", () =>
        {
            var ini = "[Rendering/AsyncCompute]\nHairClears = True\n";
            var removable = new List<UserIniKey>
            {
                new("Rendering/AsyncCompute", "HairClears", "False"),
            };

            var result = new EngineIniWriter().Plan(ini, SemChaves, null, removable);

            Check.Contains(result.NewText, "HairClears = True", "a chave ficou");
            Check.Equal(0, result.Changes.Count, "nenhuma mudanca");
            Check.Equal(1, result.SkippedRemovals.Count, "a divergencia foi reportada");
            Check.Equal("True", result.SkippedRemovals[0].CurrentValue, "valor em vigor");
            Check.Equal("False", result.SkippedRemovals[0].ExpectedValue, "valor esperado");
        });

        suite.Add("5.13 a chave ausente ja e o padrao do jogo", () =>
        {
            var ini = "[Rendering/AsyncCompute]\nEnable = False\n";
            var removable = new List<UserIniKey>
            {
                new("Rendering/AsyncCompute", "HairClears", "False"),
            };

            var result = new EngineIniWriter().Plan(ini, SemChaves, null, removable);

            Check.Equal(0, result.Changes.Count, "nada a fazer");
            Check.Equal(0, result.SkippedRemovals.Count, "nada a reportar");
            Check.Equal(ini, result.NewText, "o arquivo nao foi tocado");
        });

        suite.Add("5.14 a variante escreve a chave de cvar na secao correta", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-var-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var iniPath = Path.Combine(root, @"engine\config\platform\pc\user.ini");
                Directory.CreateDirectory(Path.GetDirectoryName(iniPath)!);
                File.WriteAllText(iniPath, "[Rendering/AsyncCompute]\nEnable = False\n");

                var profile = ProfileModel.Parse(TwoLayerProfileJson);
                var schema = BuildSchema();
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root, root);
                var applier = new ProfileApplier(
                    locator,
                    new CP2077Performance.Core.Backup.BackupService(Path.Combine(root, "backups")));

                var result = applier.Apply(profile, new HashSet<string> { "testVariant" }, schema, null);

                if (!result.Ok && result.Errors.Any(e => e.Contains("execucao", StringComparison.OrdinalIgnoreCase)))
                {
                    // o jogo esta em execucao: a operacao e recusada por design
                    return;
                }

                Check.True(result.Ok, "aplicacao concluida: " + string.Join("; ", result.Errors));

                var written = File.ReadAllText(iniPath);
                Check.Contains(written, "[Rendering/AsyncCompute]", "a secao existe");
                Check.Contains(written, "Enable = True", "a chave da variante foi escrita");
                Check.Contains(written, "HairClears = False", "a chave acrescentada foi escrita");
                Check.DoesNotContain(written, "[Sec]", "nenhuma secao alheia foi criada");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("5.15 uma chave de cvar inexistente na variante e recusada na validacao", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-val-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var iniPath = Path.Combine(root, @"engine\config\platform\pc\user.ini");
                Directory.CreateDirectory(Path.GetDirectoryName(iniPath)!);
                File.WriteAllText(iniPath, "[Rendering/AsyncCompute]\n");

                var profile = ProfileModel.Parse(TwoLayerProfileJson);
                var schema = BuildSchema();
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root, root);
                var applier = new ProfileApplier(
                    locator,
                    new CP2077Performance.Core.Backup.BackupService(Path.Combine(root, "backups")));

                // os tokens do executavel nao incluem as chaves da variante
                var tokens = new HashSet<string>(StringComparer.Ordinal) { "Rendering/AsyncCompute" };

                var plan = applier.Plan(profile, new HashSet<string> { "testVariant" }, schema, tokens);

                Check.False(plan.CanApply, "o plano e recusado");
                Check.True(
                    plan.Validation.Errors.Any(e => e.Name == "Enable"),
                    "a chave inexistente da variante e apontada");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("5.16 um id de variante salvo que o perfil nao declara mais e ignorado sem erro", () =>
        {
            var profile = ProfileModel.Parse(ProfileJson);
            var schema = BuildSchema();

            var effective = VariantResolver.Resolve(profile, new HashSet<string> { "hudMinimal" }, schema);

            Check.False(effective.EnabledVariants.Contains("hudMinimal"), "id desconhecido nao fica em EnabledVariants");
            Check.False(effective.MenuValues.ContainsKey("minimap"), "nenhuma opcao da variante removida e aplicada");
            Check.False(effective.RestoreToDefault.ContainsKey("minimap"), "nenhuma opcao da variante removida e restaurada");
        });

        suite.Add("5.17 aplicar o perfil nao cria nem escreve no diretorio de mods do CET", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-nomods-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var iniPath = Path.Combine(root, @"engine\config\platform\pc\user.ini");
                Directory.CreateDirectory(Path.GetDirectoryName(iniPath)!);
                File.WriteAllText(iniPath, "[Rendering/AsyncCompute]\nEnable = True\n");

                var profile = ProfileModel.Parse(ProfileJson);
                var schema = BuildSchema();
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root, root);
                var applier = new ProfileApplier(
                    locator,
                    new CP2077Performance.Core.Backup.BackupService(Path.Combine(root, "backups")));

                var result = applier.Apply(profile, new HashSet<string> { "hudMinimal" }, schema, null);
                if (!result.Ok && result.Errors.Any(e => e.Contains("execucao", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                Check.True(result.Ok, "aplicacao concluida: " + string.Join("; ", result.Errors));
                Check.False(Directory.Exists(locator.CetModsPath), "nenhum diretorio de mods foi criado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });
    }
}
