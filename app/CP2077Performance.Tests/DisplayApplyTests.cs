namespace CP2077Performance.Tests;

using CP2077Performance.Core.Application;
using CP2077Performance.Core.Backup;
using CP2077Performance.Core.Display;
using CP2077Performance.Core.GameInstallation;
using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;
using CP2077Performance.Core.Writing;
using CP2077Performance.Ui;

internal static class DisplayApplyTests
{
    /// <summary>
    /// Um arquivo de opcoes do jogo com as opcoes de exibicao ja presentes,
    /// como o motor as grava.
    /// </summary>
    private const string MenuJson = """
    {
      "version": 140,
      "data": [
        {
          "group_name": "/video/display",
          "options": [
            {
              "name": "OutputMonitor",
              "type": "string_list",
              "value": "1",
              "index": 0
            },
            {
              "name": "WindowMode",
              "type": "string_list",
              "value": "BorderlessWindowed",
              "index": 1
            },
            {
              "name": "Resolution",
              "type": "string_list",
              "value": "1366x768",
              "index": 2
            }
          ]
        }
      ]
    }
    """;

    private static GameSchema BuildSchema()
    {
        const string schemaJson = """
        {
          "options": [
            {
              "group_name": "/video/display",
              "options": [
                { "name": "WindowMode", "type": "string_list" },
                { "name": "Resolution", "type": "string_list" },
                { "name": "OutputMonitor", "type": "string_list" }
              ]
            }
          ]
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), "cp2077-dschema-" + Guid.NewGuid().ToString("N")[..8] + ".json");
        File.WriteAllText(path, schemaJson);
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
        suite.Add("7.6 a opcao de exibicao e escrita com o valor e o indice", () =>
        {
            var desired = new Dictionary<string, ProfileValue>
            {
                ["WindowMode"] = ProfileValue.FromString("Fullscreen"),
            };

            var indexes = new Dictionary<string, int>
            {
                ["WindowMode"] = 2,
            };

            var result = new MenuOptionsWriter().Plan(MenuJson, desired, BuildSchema(), indexes);

            Check.Equal(0, result.MissingOptions.Count, "nenhuma opcao ausente");
            Check.Equal(1, result.Changes.Count, "uma opcao alterada");
            Check.Contains(result.NewText, "\"value\": \"Fullscreen\"", "o modo de janela foi escrito");
            Check.Contains(result.NewText, "\"index\": 2", "o indice foi escrito");
            Check.Contains(result.NewText, "\"value\": \"1366x768\"", "a resolucao ficou intacta");
            Check.Contains(result.NewText, "\"value\": \"1\"", "as demais opcoes ficaram intactas");
        });

        suite.Add("7.7 sem o indice externo, so o valor e escrito", () =>
        {
            var desired = new Dictionary<string, ProfileValue>
            {
                ["WindowMode"] = ProfileValue.FromString("Fullscreen"),
            };

            var result = new MenuOptionsWriter().Plan(MenuJson, desired, BuildSchema());

            Check.Contains(result.NewText, "\"value\": \"Fullscreen\"", "o valor foi escrito");
            Check.Contains(result.NewText, "\"index\": 1", "o indice ficou como estava");
        });

        suite.Add("7.8 a escrita das opcoes de exibicao e quirurgica", () =>
        {
            var before = MenuJson.Split('\n').Length;
            var desired = new Dictionary<string, ProfileValue>
            {
                ["WindowMode"] = ProfileValue.FromString("Fullscreen"),
            };

            var indexes = new Dictionary<string, int> { ["WindowMode"] = 2 };
            var result = new MenuOptionsWriter().Plan(MenuJson, desired, BuildSchema(), indexes);

            Check.Equal(before, result.NewText.Split('\n').Length, "o numero de linhas nao mudou");
            Check.Equal(1, result.Changes.Count, "uma opcao alterada");
        });

        suite.Add("7.9 as opcoes de exibicao ausentes no arquivo do jogo nao impedem a aplicacao", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-disp-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var locator = new GameLocator(root, Path.Combine(root, "appdata"));
                var applier = new ProfileApplier(
                    locator,
                    new BackupService(Path.Combine(root, "backups")));

                var plan = applier.Plan(
                    ProfileModel.Parse("""{ "name": "vazio", "version": "0.1.0" }"""),
                    new HashSet<string>(),
                    BuildSchema(),
                    null);

                Check.True(plan.CanApply, "a aplicacao prossegue: " + string.Join("; ", plan.Validation.Errors));
                Check.Equal(0, plan.MissingMenuOptions.Count, "nenhuma opcao declarada como ausente");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("7.10 aplicar o perfil escreve sempre WindowMode Fullscreen (indice 2) sem alterar Resolution", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-disp-apply-" + Guid.NewGuid().ToString("N")[..8]);
            var userData = Path.Combine(root, "appdata");
            var saveDir = Path.Combine(userData, GameLocator.SaveDirectoryName);
            Directory.CreateDirectory(saveDir);
            var settingsPath = Path.Combine(saveDir, GameLocator.SettingsFileName);
            File.WriteAllText(settingsPath, MenuJson);

            try
            {
                var locator = new GameLocator(root, userData);
                var applier = new ProfileApplier(
                    locator,
                    new BackupService(Path.Combine(root, "backups")));

                var plan = applier.Plan(
                    ProfileModel.Parse("""{ "name": "vazio", "version": "0.1.0" }"""),
                    new HashSet<string>(),
                    BuildSchema(),
                    null);

                Check.Equal(1, plan.MenuChanges.Count, "apenas WindowMode entra no plano");
                Check.Equal("WindowMode", plan.MenuChanges[0].Option, "a opcao planejada e WindowMode");

                var after = MenuOptionsWriterResult(MenuJson, plan);
                Check.Contains(after, "\"value\": \"Fullscreen\"", "WindowMode foi gravado como Fullscreen");
                Check.Contains(after, "\"value\": \"1366x768\"", "Resolution 1366x768 permaneceu intocada");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("7.11 a tela cheia preserva qualquer resolucao configurada no jogo (1366x768, 1280x720, 1600x900, 1920x1080)", () =>
        {
            foreach (var resolution in new[] { "1366x768", "1280x720", "1600x900", "1920x1080", "2560x1440" })
            {
                var input = MenuJson.Replace("\"1366x768\"", $"\"{resolution}\"");
                var desired = new Dictionary<string, ProfileValue>
                {
                    [DisplayModeIndex.WindowModeOption] = ProfileValue.FromString(DisplayOptions.FullscreenOnly.WindowMode),
                };
                var indexes = new Dictionary<string, int>
                {
                    [DisplayModeIndex.WindowModeOption] = DisplayOptions.FullscreenOnly.WindowModeIndex!.Value,
                };

                var result = new MenuOptionsWriter().Plan(input, desired, BuildSchema(), indexes);
                Check.Equal(1, result.Changes.Count, $"resolucao {resolution}: apenas 1 opcao alterada");
                Check.Contains(result.NewText, "\"value\": \"Fullscreen\"", $"resolucao {resolution}: modo Fullscreen escrito");
                Check.Contains(result.NewText, $"\"value\": \"{resolution}\"", $"resolucao {resolution}: resolucao preservada");
            }
        });

        suite.Add("7.12 a tela cheia preserva a escala dinamica de resolucao (DRS)", () =>
        {
            const string menuWithDrs = """
            {
              "version": 140,
              "data": [
                {
                  "group_name": "/video/display",
                  "options": [
                    { "name": "WindowMode", "type": "string_list", "value": "BorderlessWindowed", "index": 1 },
                    { "name": "Resolution", "type": "string_list", "value": "1366x768", "index": 2 },
                    { "name": "DRS_TargetFPS", "type": "int", "value": 30 },
                    { "name": "DRS_MinResolution", "type": "int", "value": 50 },
                    { "name": "DRS_MaxResolution", "type": "int", "value": 85 }
                  ]
                }
              ]
            }
            """;

            var desired = new Dictionary<string, ProfileValue>
            {
                [DisplayModeIndex.WindowModeOption] = ProfileValue.FromString(DisplayOptions.FullscreenOnly.WindowMode),
            };
            var indexes = new Dictionary<string, int>
            {
                [DisplayModeIndex.WindowModeOption] = DisplayOptions.FullscreenOnly.WindowModeIndex!.Value,
            };

            var after = new MenuOptionsWriter().Plan(menuWithDrs, desired, BuildSchema(), indexes).NewText;
            Check.Contains(after, "\"value\": \"Fullscreen\"", "WindowMode alterado para Fullscreen");
            Check.Contains(after, "\"value\": \"1366x768\"", "Resolution 1366x768 preservada");
            Check.Contains(after, "\"name\": \"DRS_MinResolution\", \"type\": \"int\", \"value\": 50", "piso DRS intacto");
            Check.Contains(after, "\"name\": \"DRS_MaxResolution\", \"type\": \"int\", \"value\": 85", "teto DRS intacto");
        });

        suite.Add("7.13 AppState inclui WindowMode Fullscreen na contagem de alteracoes e no estado aplicado", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-state-disp-" + Guid.NewGuid().ToString("N")[..8]);
            var toolData = Path.Combine(root, "tooldata");
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(toolData);

            try
            {
                var exeDir = Path.Combine(root, "bin", "x64");
                var schemaDir = Path.Combine(root, "r6", "config", "settings", "platform", "pc");
                var iniDir = Path.Combine(root, "engine", "config", "platform", "pc");
                Directory.CreateDirectory(exeDir);
                Directory.CreateDirectory(schemaDir);
                Directory.CreateDirectory(iniDir);

                File.WriteAllText(Path.Combine(exeDir, "Cyberpunk2077.exe"), "WindowMode Resolution");
                File.WriteAllText(Path.Combine(schemaDir, "options.json"), """
                {
                  "options": [
                    {
                      "group_name": "/video/display",
                      "options": [
                        { "name": "WindowMode", "type": "string_list" },
                        { "name": "Resolution", "type": "string_list" }
                      ]
                    }
                  ]
                }
                """);
                File.WriteAllText(Path.Combine(iniDir, "user.ini"), "");

                var state = new AppState("""{ "name": "Teste", "version": "0.1.0", "userSettings": {}, "userIni": {}, "cet": {}, "variants": {} }""", toolData);
                Check.True(state.SetGamePath(root), "pasta valida");
                Check.Null(
                    typeof(AppState).GetProperty("DisplaySelection"),
                    "AppState nao expoe mais DisplaySelection");
                Check.Null(
                    typeof(AppState).GetProperty("Monitors"),
                    "AppState nao expoe mais Monitors");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });
    }

    private static string MenuOptionsWriterResult(string menuText, ApplyPlan plan)
    {
        var desired = new Dictionary<string, ProfileValue>
        {
            [DisplayModeIndex.WindowModeOption] = ProfileValue.FromString(DisplayOptions.FullscreenOnly.WindowMode),
        };
        var indexes = new Dictionary<string, int>
        {
            [DisplayModeIndex.WindowModeOption] = DisplayOptions.FullscreenOnly.WindowModeIndex!.Value,
        };
        return new MenuOptionsWriter().Plan(menuText, desired, BuildSchema(), indexes).NewText;
    }
}
