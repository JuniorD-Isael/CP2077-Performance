namespace CP2077Performance.Tests;

using System.Drawing;
using System.Windows.Forms;
using CP2077Performance.Ui.Theme;

internal static class ThemeTests
{
    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static void Register(TestSuite suite)
    {
        suite.Add("8.1 a paleta escura Edgerunners usa as quatro cores-base nos seus papeis", () =>
        {
            var palette = CyberpunkPalette.Dark;

            Check.Equal("#04101C", Hex(palette.Background), "fundo azul-petroleo Night City");
            Check.Equal("#F4F7FA", Hex(palette.OnSurface), "texto claro");
            Check.Equal("#FCEE0A", Hex(palette.Primary), "amarelo acido Edgerunners");
            Check.Equal("#04101C", Hex(palette.OnPrimary), "texto escuro sobre o amarelo acido");
            Check.Equal("#00FF66", Hex(palette.Accent), "verde neon Sandevistan");
        });

        suite.Add("8.2 a paleta clara Edgerunners usa as quatro cores-base nos seus papeis", () =>
        {
            var palette = CyberpunkPalette.Light;

            Check.Equal("#F4F7FA", Hex(palette.Background), "fundo claro");
            Check.Equal("#04101C", Hex(palette.OnSurface), "texto escuro");
            Check.Equal("#FCEE0A", Hex(palette.Primary), "acao principal");
            Check.Equal("#04101C", Hex(palette.OnPrimary), "texto escuro sobre o amarelo acido");
        });

        suite.Add("8.3 todo papel de texto tem contraste suficiente sobre o fundo", () =>
        {
            foreach (var palette in new[] { CyberpunkPalette.Dark, CyberpunkPalette.Light })
            {
                var mode = palette == CyberpunkPalette.Dark ? "escura" : "clara";

                foreach (var (name, color) in new (string, Color)[]
                {
                    ("texto", palette.OnSurface),
                    ("texto suave", palette.OnSurfaceMuted),
                    ("principal como texto", palette.PrimaryText),
                    ("destaque", palette.Accent),
                    ("erro", palette.Error),
                })
                {
                    var contrast = CyberpunkPalette.Contrast(color, palette.Background);
                    Check.True(
                        contrast >= CyberpunkPalette.MinimumTextContrast,
                        $"paleta {mode}: {name} com contraste {contrast:F2}");
                }
            }
        });

        suite.Add("8.3b o rotulo sobre o preenchimento principal alcanca o contraste WCAG", () =>
        {
            foreach (var palette in new[] { CyberpunkPalette.Dark, CyberpunkPalette.Light })
            {
                var mode = palette == CyberpunkPalette.Dark ? "escura" : "clara";
                var contrast = CyberpunkPalette.Contrast(palette.OnPrimary, palette.Primary);

                Check.True(
                    contrast >= CyberpunkPalette.PrimaryFillLabelContrast,
                    $"paleta {mode}: rotulo sobre o preenchimento principal com contraste {contrast:F2}");
            }
        });

        suite.Add("8.4 as superficies e o contorno derivam das cores-base", () =>
        {
            var dark = CyberpunkPalette.Dark;
            Check.Equal(
                Hex(CyberpunkPalette.Mix(dark.Background, dark.OnSurface, 0.07)),
                Hex(dark.Surface),
                "a superficie escura deriva do fundo e do texto");
            Check.Equal(
                Hex(CyberpunkPalette.Mix(dark.Background, dark.OnSurface, 0.28)),
                Hex(dark.Outline),
                "o contorno escuro deriva do fundo e do texto");

            var light = CyberpunkPalette.Light;
            Check.Equal(
                Hex(CyberpunkPalette.Mix(light.Background, light.OnSurface, 0.05)),
                Hex(light.Surface),
                "a superficie clara deriva do fundo e do texto");
        });

        suite.Add("8.5 o papel de erro e a rotacao de matiz da cor principal", () =>
        {
            var dark = CyberpunkPalette.Dark;
            var expectedDark = CyberpunkPalette.EnsureContrast(
                CyberpunkPalette.RotateHue(dark.Primary, CyberpunkPalette.ErrorHueRotation),
                dark.Background,
                CyberpunkPalette.MinimumTextContrast);
            Check.True(
                Math.Abs(expectedDark.R - dark.Error.R) <= 1 &&
                Math.Abs(expectedDark.G - dark.Error.G) <= 1 &&
                Math.Abs(expectedDark.B - dark.Error.B) <= 1,
                $"o erro escuro e a rotacao ajustada ao contraste (esperado {Hex(expectedDark)})");

            var light = CyberpunkPalette.Light;
            var expectedLight = CyberpunkPalette.EnsureContrast(
                CyberpunkPalette.RotateHue(light.Primary, CyberpunkPalette.ErrorHueRotation),
                light.Background,
                CyberpunkPalette.MinimumTextContrast);

            Check.True(
                Math.Abs(expectedLight.R - light.Error.R) <= 1 &&
                Math.Abs(expectedLight.G - light.Error.G) <= 1 &&
                Math.Abs(expectedLight.B - light.Error.B) <= 1,
                $"o erro claro e a rotacao ajustada ao contraste (esperado {Hex(expectedLight)})");
        });

        suite.Add("8.6 o destaque da variante clara e legivel sobre o fundo claro", () =>
        {
            var light = CyberpunkPalette.Light;

            Check.NotEqual(
                Hex(CyberpunkPalette.BaseAccent),
                Hex(light.Accent),
                "o destaque escureceu para virar legivel");

            var contrast = CyberpunkPalette.Contrast(light.Accent, light.Background);
            Check.True(
                contrast >= CyberpunkPalette.MinimumTextContrast,
                $"contraste do destaque claro: {contrast:F2}");
        });

        suite.Add("8.7 o recorte Edgerunners gera poligono chanfrado em 45 graus", () =>
        {
            using var path = MaterialTheme.ChamferedPath(new Rectangle(0, 0, 120, 48), 10);
            Check.Equal(6, path.PointCount, "o poligono chanfrado possui 6 vertices");
            var pts = path.PathPoints;
            Check.Equal(10f, pts[0].X, "chanfro superior esquerdo X");
            Check.Equal(0f, pts[0].Y, "chanfro superior esquerdo Y");
            Check.Equal(0f, pts[5].X, "chanfro lateral esquerdo X");
            Check.Equal(10f, pts[5].Y, "chanfro lateral esquerdo Y");
        });

        suite.Add("8.10 a tela principal Edgerunners e enxuta sem seletor de monitor e sem detalhe tecnico", () =>
        {
            var root = InstallationTests.CreateFakeInstall();
            try
            {
                var locator = new CP2077Performance.Core.GameInstallation.GameLocator(root);
                Directory.CreateDirectory(Path.Combine(root, @"r6\config\settings"));
                File.WriteAllText(
                    Path.Combine(root, @"r6\config\settings\options.json"),
                    """{ "options": [ { "group_name": "/video/display", "options": [ { "name": "WindowMode", "type": "string_list" }, { "name": "Resolution", "type": "string_list" } ] } ] }""");
                Directory.CreateDirectory(locator.LegacyHudModPath);
                File.WriteAllText(Path.Combine(locator.LegacyHudModPath, "init.lua"), "-- legacy mod");

                var repoRoot = FindRepositoryRoot();
                var profileJson = File.ReadAllText(Path.Combine(repoRoot, "profile", "lowspec-30fps.json"));
                var state = new CP2077Performance.Ui.AppState(profileJson, Path.Combine(root, "tooldata"));
                state.SetGamePath(root);

                using var form = new CP2077Performance.Ui.MainForm(state);
                form.Show();
                Application.DoEvents();
                form.PerformLayout();
                Application.DoEvents();

                using var bmp = new Bitmap(form.Width, form.Height);
                DrawControlTree(form, bmp, Point.Empty);

                var capturePath = Path.Combine(Path.GetTempPath(), "cp2077-mainform-capture.png");
                bmp.Save(capturePath, System.Drawing.Imaging.ImageFormat.Png);

                var allControls = CollectControls(form);
                var allLabels = CollectLabels(form);
                form.Close();

                Check.True(File.Exists(capturePath) && new FileInfo(capturePath).Length > 1000, "captura da tela principal gerada");
                Check.True(Directory.Exists(locator.LegacyHudModPath), "diretorio do mod antigo permanece intacto");

                Check.False(allControls.Any(c => c is ComboBox), "nenhum ComboBox de monitor na tela principal");
                Check.False(allControls.Any(c => c is CheckBox), "nenhum CheckBox de escolha intermediaria na tela principal");
                Check.False(allControls.Any(c => c is TabControl), "nenhum TabControl de detalhe tecnico na tela principal");
                Check.False(allControls.Any(c => c is DataGridView), "nenhuma tabela DataGridView na tela principal");

                Check.True(
                    allLabels.Any(t => t.Contains("CP2077Performance", StringComparison.OrdinalIgnoreCase)),
                    "aviso do mod antigo exibido na tela principal: " + string.Join(" | ", allLabels));
                Check.False(
                    allLabels.Any(t =>
                        t.Contains("Mostrar detalhe", StringComparison.OrdinalIgnoreCase) ||
                        t.Contains("Show technical", StringComparison.OrdinalIgnoreCase) ||
                        t.Contains("O que aplicar", StringComparison.OrdinalIgnoreCase) ||
                        t.Contains("What to apply", StringComparison.OrdinalIgnoreCase)),
                    "nenhum texto de 'O que aplicar' ou 'Mostrar detalhe tecnico'");
            }
            finally
            {
                InstallationTests.Cleanup(root);
            }
        });
    }

    private static void DrawControlTree(Control control, Bitmap target, Point offset)
    {
        if (!control.Visible || control.Width <= 0 || control.Height <= 0)
        {
            return;
        }

        control.DrawToBitmap(target, new Rectangle(offset.X, offset.Y, control.Width, control.Height));
        var clientOffset = new Point(
            offset.X + (control.Width - control.ClientSize.Width) / 2,
            offset.Y + (control.Height - control.ClientSize.Height) - (control.Width - control.ClientSize.Width) / 2);
        foreach (Control child in control.Controls)
        {
            DrawControlTree(child, target, new Point(clientOffset.X + child.Left, clientOffset.Y + child.Top));
        }
    }

    private static List<Control> CollectControls(Control parent)
    {
        var list = new List<Control>();
        foreach (Control c in parent.Controls)
        {
            list.Add(c);
            list.AddRange(CollectControls(c));
        }
        return list;
    }

    private static List<string> CollectLabels(Control parent)
    {
        var list = new List<string>();
        foreach (Control c in parent.Controls)
        {
            if (!string.IsNullOrEmpty(c.Text))
            {
                list.Add(c.Text);
            }
            list.AddRange(CollectLabels(c));
        }
        return list;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "profile", "lowspec-30fps.json")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Raiz nao encontrada");
    }
}
