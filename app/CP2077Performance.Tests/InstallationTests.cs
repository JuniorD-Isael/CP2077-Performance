namespace CP2077Performance.Tests;

using CP2077Performance.Core.GameInstallation;

internal static class InstallationTests
{
    /// <summary>Cria uma arvore de instalacao ficticia e devolve a pasta raiz.</summary>
    internal static string CreateFakeInstall(bool withCet = true, bool withEngineIni = true)
    {
        var root = Path.Combine(Path.GetTempPath(), "cp2077-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(root, @"bin\x64"));
        File.WriteAllText(Path.Combine(root, @"bin\x64\Cyberpunk2077.exe"), "stub");

        if (withEngineIni)
        {
            Directory.CreateDirectory(Path.Combine(root, @"engine\config\platform\pc"));
            File.WriteAllText(Path.Combine(root, @"engine\config\platform\pc\user.ini"), "[Sec]\r\nK = V\r\n");
        }

        if (withCet)
        {
            Directory.CreateDirectory(Path.Combine(root, @"bin\x64\plugins\cyber_engine_tweaks\mods"));
            File.WriteAllText(Path.Combine(root, @"bin\x64\plugins\cyber_engine_tweaks\config.json"), "{}");
        }

        return root;
    }

    internal static void Cleanup(string root)
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { /* best effort */ }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("2.1 os tres alvos sao derivados da pasta do jogo", () =>
        {
            var root = CreateFakeInstall();
            try
            {
                var locator = new GameLocator(root);

                Check.Equal(
                    Path.Combine(root, @"engine\config\platform\pc\user.ini"),
                    locator.EngineIniPath,
                    "alvo do motor");

                Check.Equal(
                    Path.Combine(root, @"bin\x64\plugins\cyber_engine_tweaks\config.json"),
                    locator.CetConfigPath,
                    "alvo dos patches do CET");

                Check.True(
                    locator.SettingsPath.EndsWith(
                        Path.Combine("CD Projekt Red", "Cyberpunk 2077", "UserSettings.json"),
                        StringComparison.Ordinal),
                    $"alvo do menu termina em CD Projekt Red\\Cyberpunk 2077\\UserSettings.json, obtido '{locator.SettingsPath}'");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.1 o caminho do save nao depende do caminho da instalacao", () =>
        {
            var a = CreateFakeInstall();
            var b = CreateFakeInstall();
            try
            {
                var la = new GameLocator(a);
                var lb = new GameLocator(b);

                Check.Equal(la.SettingsPath, lb.SettingsPath, "caminho do save");
                Check.NotEqual(la.GameRoot, lb.GameRoot, "as instalacoes sao diferentes");
            }
            finally
            {
                Cleanup(a);
                Cleanup(b);
            }
        });

        suite.Add("2.2 a pasta e valida quando contem o executavel do jogo", () =>
        {
            var root = CreateFakeInstall();
            try
            {
                var validator = new InstallationValidator(new GameLocator(root));
                Check.True(validator.IsValidInstallation(), "instalacao valida");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.2 a pasta e invalida sem o executavel do jogo", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "cp2077-test-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(root);
            try
            {
                var validator = new InstallationValidator(new GameLocator(root));
                Check.False(validator.IsValidInstallation(), "instalacao invalida");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.2 a camada de patches e opcional quando o CET nao esta instalado", () =>
        {
            var root = CreateFakeInstall(withCet: false);
            try
            {
                var validator = new InstallationValidator(new GameLocator(root));
                var layers = validator.Evaluate();

                Check.True(validator.IsValidInstallation(), "instalacao continua valida");

                var cet = layers.Single(l => l.Layer == ConfigLayer.Cet);
                Check.False(cet.Available, "camada de patches indisponivel");

                var engine = layers.Single(l => l.Layer == ConfigLayer.Engine);
                Check.True(engine.Available, "camada do motor disponivel");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.2 a origem de cada alvo e apresentada ao usuario", () =>
        {
            var root = CreateFakeInstall();
            try
            {
                var validator = new InstallationValidator(new GameLocator(root));
                var layers = validator.Evaluate();

                var engine = layers.Single(l => l.Layer == ConfigLayer.Engine);
                Check.Contains(engine.Origin, "pasta do jogo", "origem da camada do motor");

                var menu = layers.Single(l => l.Layer == ConfigLayer.Menu);
                Check.Contains(menu.Origin, "dados locais do sistema", "origem da camada de menu");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.3 a guarda recusa a operacao com o jogo em execucao", () =>
        {
            var guard = new GameRunningGuard();
            Check.Contains(GameRunningGuard.RefusalMessage, "em execucao", "explicacao da recusa");
            Check.Contains(GameRunningGuard.RefusalMessage, "ao encerrar", "explicacao da recusa");
            Check.Contains(GameRunningGuard.RefusalMessage, "anularia a aplicacao", "explicacao da recusa");
        });

        suite.Add("2.3 a guarda permite prosseguir quando o jogo nao esta em execucao", () =>
        {
            var guard = new GameRunningGuard();
            var canProceed = guard.CanProceed(out var reason);
            Check.True(canProceed, $"deve prosseguir (motivo: {reason})");
            Check.Null(reason, "sem motivo de recusa");
        });

        suite.Add("2.4 o painel remanescente e detectado quando existe", () =>
        {
            var root = CreateFakeInstall();
            try
            {
                var locator = new GameLocator(root);
                var detector = new LeftoverPanelDetector(locator);

                Check.Null(detector.Detect(), "nada detectado sem o painel");

                Directory.CreateDirectory(locator.LegacyPanelPath);
                File.WriteAllText(Path.Combine(locator.LegacyPanelPath, "init.lua"), "-- stub");

                var panel = detector.Detect();
                Check.NotNull(panel, "painel detectado");
                Check.Equal(1, panel!.FileCount, "quantidade de arquivos");
                Check.Contains(LeftoverPanelDetector.NoticeMessage, "deixou de aplicar", "aviso ao usuario");
            }
            finally
            {
                Cleanup(root);
            }
        });

        suite.Add("2.4 a remocao do painel apaga o diretorio", () =>
        {
            var root = CreateFakeInstall();
            try
            {
                var locator = new GameLocator(root);
                var detector = new LeftoverPanelDetector(locator);

                Directory.CreateDirectory(locator.LegacyPanelPath);
                File.WriteAllText(Path.Combine(locator.LegacyPanelPath, "init.lua"), "-- stub");

                var panel = detector.Detect();
                Check.NotNull(panel, "painel detectado");
                Check.True(detector.Remove(panel!), "removido");
                Check.False(Directory.Exists(locator.LegacyPanelPath), "diretorio apagado");
            }
            finally
            {
                Cleanup(root);
            }
        });
    }
}
