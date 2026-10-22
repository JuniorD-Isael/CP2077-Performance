using System.Drawing;

namespace CP2077Performance.Ui.Theme;

/// <summary>
/// A paleta Cyberpunk: Edgerunners do aplicativo: quatro cores-base, e tudo o
/// mais derivado delas.
/// </summary>
/// <remarks>
/// As quatro cores-base sao fixas e inspiradas na arte promocional de
/// <em>Cyberpunk: Edgerunners</em>:
/// <list type="bullet">
/// <item><description><c>f4f7fa</c> - superficie clara, e texto principal sobre fundo escuro.</description></item>
/// <item><description><c>fcee0a</c> - amarelo acido Edgerunners (acao principal, moldura e cabeçalho HUD).</description></item>
/// <item><description><c>00ff66</c> - verde neon Sandevistan (destaque e estado ativo/sucesso).</description></item>
/// <item><description><c>04101c</c> - azul-petroleo abissal de Night City (fundo escuro e texto sobre o amarelo).</description></item>
/// </list>
/// </remarks>
public sealed record CyberpunkPalette(
    Color Background,
    Color Surface,
    Color SurfaceRaised,
    Color Outline,
    Color OnSurface,
    Color OnSurfaceMuted,
    Color Primary,
    Color PrimaryText,
    Color OnPrimary,
    Color Accent,
    Color OnAccent,
    Color Error,
    Color OnError,
    int CornerRadius,
    int Spacing)
{
    /// <summary>Superficie clara, e texto sobre fundo escuro.</summary>
    public static readonly Color BaseLight = ColorTranslator.FromHtml("#f4f7fa");

    /// <summary>Amarelo acido Edgerunners (acao principal e moldura HUD).</summary>
    public static readonly Color BasePrimary = ColorTranslator.FromHtml("#fcee0a");

    /// <summary>Verde neon Sandevistan (destaque e estado aplicado).</summary>
    public static readonly Color BaseAccent = ColorTranslator.FromHtml("#00ff66");

    /// <summary>Azul-petroleo profundo de Night City (fundo escuro).</summary>
    public static readonly Color BaseDark = ColorTranslator.FromHtml("#04101c");

    /// <summary>Tamanho do chanfro 45 graus nos cantos, em pixels de 100%.</summary>
    public const int BaseCornerRadius = 10;

    /// <summary>Grade de espacamento, em pixels de 100%.</summary>
    public const int BaseSpacing = 8;

    /// <summary>Contraste minimo de um texto sobre a superficie.</summary>
    public const double MinimumTextContrast = 4.5;

    /// <summary>Contraste minimo do rotulo sobre o preenchimento principal.</summary>
    public const double PrimaryFillLabelContrast = 4.5;

    /// <summary>Rotacao de matiz que leva o amarelo acido ao vermelho cyberware.</summary>
    public const double ErrorHueRotation = -60;

    /// <summary>A paleta da variante escura.</summary>
    public static readonly CyberpunkPalette Dark = BuildDark();

    /// <summary>A paleta da variante clara.</summary>
    public static readonly CyberpunkPalette Light = BuildLight();

    /// <summary>A paleta da variante pedida.</summary>
    public static CyberpunkPalette For(bool dark) => dark ? Dark : Light;

    private static CyberpunkPalette BuildDark()
    {
        var background = BaseDark;
        var onSurface = BaseLight;
        var primary = BasePrimary;
        var accent = BaseAccent;

        var error = EnsureContrast(
            RotateHue(primary, ErrorHueRotation),
            background,
            MinimumTextContrast);

        return new CyberpunkPalette(
            Background: background,
            Surface: Mix(background, onSurface, 0.07),
            SurfaceRaised: Mix(background, onSurface, 0.13),
            Outline: Mix(background, onSurface, 0.28),
            OnSurface: onSurface,
            OnSurfaceMuted: EnsureContrast(
                Mix(onSurface, background, 0.36),
                background,
                MinimumTextContrast),
            Primary: primary,
            PrimaryText: EnsureContrast(primary, background, MinimumTextContrast),
            OnPrimary: background,
            Accent: EnsureContrast(accent, background, MinimumTextContrast),
            OnAccent: background,
            Error: error,
            OnError: background,
            CornerRadius: BaseCornerRadius,
            Spacing: BaseSpacing);
    }

    private static CyberpunkPalette BuildLight()
    {
        var background = BaseLight;
        var onSurface = BaseDark;
        var primary = BasePrimary;
        var accent = BaseAccent;

        var readablePrimary = EnsureContrast(primary, background, MinimumTextContrast);
        var readableAccent = EnsureContrast(accent, background, MinimumTextContrast);
        var readableError = EnsureContrast(
            RotateHue(primary, ErrorHueRotation),
            background,
            MinimumTextContrast);

        return new CyberpunkPalette(
            Background: background,
            Surface: Mix(background, onSurface, 0.05),
            SurfaceRaised: Mix(background, onSurface, 0.10),
            Outline: Mix(background, onSurface, 0.26),
            OnSurface: onSurface,
            OnSurfaceMuted: EnsureContrast(
                Mix(onSurface, background, 0.42),
                background,
                MinimumTextContrast),
            Primary: primary,
            PrimaryText: readablePrimary,
            OnPrimary: onSurface,
            Accent: readableAccent,
            OnAccent: BaseLight,
            Error: readableError,
            OnError: BaseLight,
            CornerRadius: BaseCornerRadius,
            Spacing: BaseSpacing);
    }

    // ---------------------------------------------------------------- derivacao

    /// <summary>Mistura duas cores. <c>t = 0</c> devolve a primeira.</summary>
    public static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(a.R + (b.R - a.R) * t),
            (int)Math.Round(a.G + (b.G - a.G) * t),
            (int)Math.Round(a.B + (b.B - a.B) * t));
    }

    /// <summary>Multiplica os canais da cor por um fator.</summary>
    public static Color Scale(Color c, double factor) => Color.FromArgb(
        Math.Clamp((int)Math.Round(c.R * factor), 0, 255),
        Math.Clamp((int)Math.Round(c.G * factor), 0, 255),
        Math.Clamp((int)Math.Round(c.B * factor), 0, 255));

    /// <summary>Rotaciona o matiz da cor, preservando saturacao e luminosidade.</summary>
    public static Color RotateHue(Color c, double degrees)
    {
        var (h, s, l) = ToHsl(c);
        return FromHsl(h + degrees, s, l);
    }

    /// <summary>
    /// Devolve a cor, escurecida ou clareada, que atinge o contraste minimo
    /// sobre o fundo. A direcao segue o fundo: escurece sobre fundo claro,
    /// clareia sobre fundo escuro.
    /// </summary>
    public static Color EnsureContrast(Color c, Color background, double minimum)
    {
        if (Contrast(c, background) >= minimum)
        {
            return c;
        }

        var lighten = Luminance(background) < 0.5;
        var best = c;
        var bestContrast = Contrast(c, background);

        for (var i = 0; i < 48; i++)
        {
            var factor = 1 - (i + 1) / 48.0;
            var candidate = lighten
                ? Mix(c, Color.White, 1 - factor)
                : Scale(c, factor);

            var contrast = Contrast(candidate, background);
            if (contrast > bestContrast)
            {
                best = candidate;
                bestContrast = contrast;
            }

            if (contrast >= minimum)
            {
                break;
            }
        }

        return best;
    }

    /// <summary>Luminancia relativa, como a WCAG define.</summary>
    public static double Luminance(Color c) =>
        0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);

    /// <summary>Razao de contraste entre duas cores.</summary>
    public static double Contrast(Color a, Color b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Channel(byte value)
    {
        var v = value / 255.0;
        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static (double H, double S, double L) ToHsl(Color c)
    {
        var r = c.R / 255.0;
        var g = c.G / 255.0;
        var b = c.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2.0;

        if (Math.Abs(max - min) < double.Epsilon)
        {
            return (0, 0, l);
        }

        var d = max - min;
        var s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

        double h;
        if (Math.Abs(max - r) < double.Epsilon)
        {
            h = (g - b) / d + (g < b ? 6 : 0);
        }
        else if (Math.Abs(max - g) < double.Epsilon)
        {
            h = (b - r) / d + 2;
        }
        else
        {
            h = (r - g) / d + 4;
        }

        return (h * 60, s, l);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360 / 360.0;

        if (s <= double.Epsilon)
        {
            var grey = (int)Math.Round(Math.Clamp(l, 0, 1) * 255);
            return Color.FromArgb(grey, grey, grey);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;

        return Color.FromArgb(
            (int)Math.Round(255 * HueToRgb(p, q, h + 1.0 / 3)),
            (int)Math.Round(255 * HueToRgb(p, q, h)),
            (int)Math.Round(255 * HueToRgb(p, q, h - 1.0 / 3)));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6)
        {
            return p + (q - p) * 6 * t;
        }

        if (t < 1.0 / 2)
        {
            return q;
        }

        if (t < 2.0 / 3)
        {
            return p + (q - p) * (2.0 / 3 - t) * 6;
        }

        return p;
    }
}
