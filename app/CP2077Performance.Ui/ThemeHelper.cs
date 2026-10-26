using CP2077Performance.Ui.Theme;
using Microsoft.Win32;

namespace CP2077Performance.Ui;

/// <summary>
/// Detecta a preferencia de tema do sistema operacional e a aplica.
/// </summary>
internal static class ThemeHelper
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Verdadeiro quando o sistema pede aplicativo em tema escuro.
    /// </summary>
    public static bool SystemPrefersDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            if (key?.GetValue("AppsUseLightTheme") is int light)
            {
                return light == 0;
            }
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    /// <summary>
    /// A paleta Cyberpunk da variante pedida pelo sistema.
    /// </summary>
    /// <remarks>
    /// A preferencia do sistema escolhe a variante, clara ou escura; a cor vem
    /// da paleta do aplicativo, nao do Windows. A moldura nativa da janela
    /// continua seguindo o sistema, para que as caixas de dialogo do Windows
    /// nao destoem.
    /// </remarks>
    public static CyberpunkPalette Palette => CyberpunkPalette.For(SystemPrefersDark());

    /// <summary>Aplica o tema do sistema a aplicacao.</summary>
    public static void ApplySystemTheme()
    {
        if (Environment.GetEnvironmentVariable("CP2077_NO_SETCOLORMODE") == "1")
        {
            return;
        }

        try
        {
            // no tema escuro o modo e fixado; no claro, o modo 'System' acompanha
            // a preferencia do sistema operacional
            Application.SetColorMode(
                SystemPrefersDark()
                    ? System.Windows.Forms.SystemColorMode.Dark
                    : System.Windows.Forms.SystemColorMode.System);
        }
        catch (PlatformNotSupportedException)
        {
            // sem suporte a cor nativa: o padrao do WinForms vale
        }
    }
}
