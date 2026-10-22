namespace CP2077Performance.Tests;

using System.Text.Json.Nodes;
using CP2077Performance.Core.GameIntegration;

internal static class HudToggleTests
{
    private static string CriarJogoFalso()
    {
        var root = Path.Combine(Path.GetTempPath(), "cp2077-hud-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);
        return root;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("9.1 o diretorio do mod e derivado da pasta do jogo", () =>
        {
            var root = CriarJogoFalso();
            try
            {
                var hud = new HudToggle(root);
                var esperado = Path.Combine(
                    root, "bin", "x64", "plugins", "cyber_engine_tweaks", "mods", "CP2077Performance");

                Check.Equal(esperado, hud.ModDirectory, "o diretorio do mod");
                Check.Equal(Path.Combine(esperado, "intencao.json"), hud.IntentPath, "o arquivo de intencao");
                Check.Equal(Path.Combine(esperado, "estado.json"), hud.StatePath, "o arquivo de estado");
                Check.False(hud.IsInstalled, "o mod ainda nao esta instalado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("9.2 a intencao carrega o estado do HUD", () =>
        {
            var root = CriarJogoFalso();
            try
            {
                var hud = new HudToggle(root);
                Check.True(hud.WriteIntent(HudState.Minimal), "a intencao foi gravada");

                // o arquivo e um texto simples: o sandbox do mod nao tem json
                Check.Equal("minimo", File.ReadAllText(hud.IntentPath), "o estado no arquivo");

                Check.True(hud.WriteIntent(HudState.Complete), "a intencao foi trocada");
                Check.Equal("completo", File.ReadAllText(hud.IntentPath), "o estado no arquivo");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("9.3 o estado em vigor e o que o mod gravou", () =>
        {
            var root = CriarJogoFalso();
            try
            {
                var hud = new HudToggle(root);
                Check.Null(hud.ReadState(), "sem arquivo, nao ha estado");

                Directory.CreateDirectory(hud.ModDirectory);
                File.WriteAllText(hud.StatePath, "minimo");
                Check.Equal(HudState.Minimal, hud.ReadState(), "o estado minimo");

                File.WriteAllText(hud.StatePath, "completo");
                Check.Equal(HudState.Complete, hud.ReadState(), "o estado completo");

                File.WriteAllText(hud.StatePath, "inexistente");
                Check.Null(hud.ReadState(), "um estado desconhecido nao e estado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("9.4 a tecla e guardada do jeito que o CET guarda", () =>
        {
            // o CET guarda o codigo virtual da tecla deslocado 48 bits: o
            // overlay dele, F1, vale 0x70000000000000
            Check.Equal(31525197391593472L, HudToggle.EncodeVirtualKey(0x70), "F1");
            Check.Equal(0x70, HudToggle.DecodeVirtualKey(31525197391593472L), "F1 decodificado");
            Check.Equal(0x77, HudToggle.DecodeVirtualKey(HudToggle.EncodeVirtualKey(0x77)), "F8 decodificado");
            Check.Equal("F8", HudToggle.DescribeVirtualKey(0x77), "o nome de F8");
            Check.Equal("F1", HudToggle.DescribeVirtualKey(0x70), "o nome de F1");
            Check.Equal(0x77, HudToggle.DefaultVirtualKey, "a tecla padrao e F8");
        });

        suite.Add("9.5 instalar o mod escreve os arquivos", () =>
        {
            var root = CriarJogoFalso();
            try
            {
                var origem = Path.Combine(root, "origem.lua");
                File.WriteAllText(origem, "-- mod de teste\n");

                var hud = new HudToggle(root);
                Check.True(hud.Install(origem), "a instalacao");

                Check.True(hud.IsInstalled, "o mod esta instalado");
                Check.Contains(
                    File.ReadAllText(Path.Combine(hud.ModDirectory, "init.lua")),
                    "mod de teste",
                    "o conteudo do mod foi copiado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("9.6 a virada da tecla nao depende do mod ja ter aplicado", () =>
        {
            var root = CriarJogoFalso();
            try
            {
                var hud = new HudToggle(root);

                // a primeira virada sai do que ja foi pedido
                Check.Equal(HudState.Minimal, hud.ToggleIntent(), "a primeira virada");
                Check.Equal("minimo", File.ReadAllText(hud.IntentPath), "a intencao gravada");

                // a segunda, antes de o mod gravar qualquer estado, continua
                // alternando: o que conta e a intencao, nao o atraso do mod
                Check.Equal(HudState.Complete, hud.ToggleIntent(), "a segunda virada");
                Check.Equal("completo", File.ReadAllText(hud.IntentPath), "a intencao gravada");

                // e o que o mod gravou continua sendo o estado em vigor
                Check.Null(hud.ReadState(), "o mod ainda nao gravou nada");

                // com o estado do mod em disco, ele continua sendo o estado
                // em vigor
                Directory.CreateDirectory(hud.ModDirectory);
                File.WriteAllText(hud.StatePath, "minimo");
                Check.Equal(HudState.Minimal, hud.ReadState(), "o estado em vigor");

                // a virada sai do que o aplicativo pediu, e nao do que o mod
                // gravou por ultimo
                Check.Equal(HudState.Minimal, hud.ToggleIntent(), "a terceira virada");
                Check.Equal("minimo", File.ReadAllText(hud.IntentPath), "a intencao gravada");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });
    }
}
