using System.Resources;

namespace CP2077Performance.Ui;

/// <summary>
/// Textos da interface.
/// </summary>
/// <remarks>
/// O recurso neutro, em ingles, vai no assembly principal; o recurso
/// <c>pt-BR</c> vira um assembly satellite. O aplicativo escolhe o idioma
/// conforme <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>, e
/// um locale desconhecido cai no ingles, nunca numa tela sem traducao.
/// </remarks>
internal static class Strings
{
    private static readonly ResourceManager Resources = new(
        "CP2077Performance.Ui.Strings",
        typeof(Strings).Assembly);

    public static string Get(string key)
    {
        try
        {
            return Resources.GetString(key) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }
}
