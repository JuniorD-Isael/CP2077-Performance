using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CP2077Performance.Ui.Theme;

/// <summary>Como um botao se apresenta.</summary>
public enum ButtonVariant
{
    /// <summary>Preenchido com o amarelo acido Edgerunners.</summary>
    Filled,

    /// <summary>Preenchido com a superficie elevada e borda tecnica.</summary>
    Tonal,

    /// <summary>Superficie com contorno destacado.</summary>
    Outlined,

    /// <summary>Sem preenchimento e sem contorno.</summary>
    Text,
}

/// <summary>Papel de um texto.</summary>
public enum LabelRole
{
    Title,
    Body,
    Muted,
    Caption,
}

/// <summary>
/// A identidade visual Cyberpunk: Edgerunners do aplicativo: tokens, recortes
/// poligonais chanfrados em 45 graus, cabecalho HUD e controles.
/// </summary>
public static class MaterialTheme
{
    private static CyberpunkPalette _palette = CyberpunkPalette.Dark;

    /// <summary>A paleta em uso.</summary>
    public static CyberpunkPalette Palette => _palette;

    /// <summary>
    /// Aplica a paleta a uma janela: fundo, cor de texto e fonte.
    /// </summary>
    public static void Apply(Form form, CyberpunkPalette palette)
    {
        _palette = palette;
        form.BackColor = palette.Background;
        form.ForeColor = palette.OnSurface;
        form.Font = BodyFont();
    }

    /// <summary>Converte px de 100% para px da resolucao atual.</summary>
    public static int Scale(int value)
    {
        using var probe = new Control();
        var dpi = probe.DeviceDpi > 0 ? probe.DeviceDpi : 96;
        return (int)Math.Round(value * dpi / 96.0);
    }

    // ---------------------------------------------------------------- fontes

    private static Font BodyFont() => new("Segoe UI", 9.5F, FontStyle.Regular);

    private static Font TitleFont() => new("Segoe UI", 12F, FontStyle.Bold);

    private static Font CaptionFont() => new("Consolas", 8.5F, FontStyle.Regular);

    private static Font ButtonFont() => new("Segoe UI", 9.5F, FontStyle.Bold);

    // ---------------------------------------------------------------- controles

    /// <summary>Rotulo com o papel pedido.</summary>
    public static Label MakeLabel(string text, LabelRole role, CyberpunkPalette palette)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = role switch
            {
                LabelRole.Title => palette.PrimaryText,
                LabelRole.Muted => palette.OnSurfaceMuted,
                LabelRole.Caption => palette.OnSurfaceMuted,
                _ => palette.OnSurface,
            },
            Font = role switch
            {
                LabelRole.Title => TitleFont(),
                LabelRole.Caption => CaptionFont(),
                _ => BodyFont(),
            },
        };

        return label;
    }

    /// <summary>Botao com recorte chanfrado Edgerunners.</summary>
    public static Button MakeButton(string text, ButtonVariant variant, CyberpunkPalette palette)
    {
        var button = new Button
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Font = ButtonFont(),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Padding = new Padding(Scale(12), Scale(5), Scale(12), Scale(5)),
            Margin = new Padding(0, 0, Scale(10), 0),
        };

        var (background, foreground, border) = variant switch
        {
            ButtonVariant.Filled => (palette.Primary, palette.OnPrimary, palette.Primary),
            ButtonVariant.Tonal => (palette.SurfaceRaised, palette.PrimaryText, palette.PrimaryText),
            ButtonVariant.Outlined => (palette.Surface, palette.OnSurface, palette.Outline),
            _ => (palette.Background, palette.PrimaryText, palette.Background),
        };

        button.BackColor = background;
        button.ForeColor = foreground;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = variant == ButtonVariant.Filled
            ? CyberpunkPalette.Mix(palette.Primary, palette.Accent, 0.22)
            : CyberpunkPalette.Mix(background, palette.Primary, 0.16);
        button.FlatAppearance.MouseDownBackColor = variant == ButtonVariant.Filled
            ? CyberpunkPalette.Mix(palette.Primary, palette.Background, 0.15)
            : CyberpunkPalette.Mix(background, palette.Primary, 0.26);

        button.Paint += (_, e) =>
        {
            if (button.Width <= 2 || button.Height <= 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (!button.Enabled)
            {
                using var disabledBg = new SolidBrush(palette.Surface);
                e.Graphics.FillRectangle(disabledBg, button.ClientRectangle);
                TextRenderer.DrawText(
                    e.Graphics,
                    button.Text,
                    button.Font,
                    button.ClientRectangle,
                    palette.OnSurfaceMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }

            var cut = Scale(palette.CornerRadius);
            using var borderPath = ChamferedPath(
                new Rectangle(0, 0, button.Width - 1, button.Height - 1),
                cut);
            var activeBorder = button.Enabled
                ? border
                : palette.Outline;
            using var pen = new Pen(activeBorder, variant == ButtonVariant.Filled ? 1.5f : 1.2f);
            e.Graphics.DrawPath(pen, borderPath);

            // marcador lateral verde neon no botao principal
            if (variant == ButtonVariant.Filled && button.Enabled)
            {
                using var accentBrush = new SolidBrush(palette.OnPrimary);
                var tickH = Math.Max(4, button.Height / 4);
                e.Graphics.FillRectangle(accentBrush, 0, (button.Height - tickH) / 2, Scale(3), tickH);
            }
        };

        Round(button, palette.CornerRadius);
        return button;
    }

    /// <summary>
    /// Painel de superficie com cantos chanfrados em 45 graus e marcadores
    /// tecnicos inspirados em Cyberpunk: Edgerunners.
    /// </summary>
    public static Panel MakeCard(CyberpunkPalette palette, bool outlined = true)
    {
        return new MaterialCard(palette, palette.CornerRadius, outlined);
    }

    /// <summary>
    /// Cabecalho visual inspirado no poster de Cyberpunk: Edgerunners:
    /// moldura lateral em amarelo acido, barras paralelas inclinadas,
    /// linhas geometricas diagonais e feixe verde neon Sandevistan.
    /// </summary>
    public static Panel MakeEdgerunnersHeader(string title, string subtitle, CyberpunkPalette palette)
    {
        return new EdgerunnersHeaderPanel(title, subtitle, palette);
    }

    /// <summary>Aplica o recorte poligonal chanfrado ao controle.</summary>
    public static void Round(Control control, int radius)
    {
        void Apply()
        {
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            using var path = ChamferedPath(control.ClientRectangle, Scale(radius));
            var region = new Region(path);
            var previous = control.Region;
            control.Region = region;
            previous?.Dispose();
        }

        Apply();
        control.Resize += (_, _) => Apply();
    }

    /// <summary>
    /// Contorno poligonal com chanfro de 45 graus nos cantos superior-esquerdo
    /// e inferior-direito, seguindo a geometria HUD de Cyberpunk: Edgerunners.
    /// </summary>
    public static GraphicsPath ChamferedPath(Rectangle bounds, int cut)
    {
        var path = new GraphicsPath();

        if (cut <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var c = Math.Min(cut, Math.Min(bounds.Width, bounds.Height) / 3);
        var l = bounds.Left;
        var t = bounds.Top;
        var r = bounds.Right;
        var b = bounds.Bottom;

        var points = new[]
        {
            new Point(l + c, t),
            new Point(r, t),
            new Point(r, b - c),
            new Point(r - c, b),
            new Point(l, b),
            new Point(l, t + c),
        };

        path.AddPolygon(points);
        return path;
    }

    /// <summary>Mantido como alias para compatibilidade com chamadores existentes.</summary>
    public static GraphicsPath RoundedPath(Rectangle bounds, int radius) =>
        ChamferedPath(bounds, radius);

    /// <summary>Painel de superficie com recorte chanfrado e faixa lateral Edgerunners.</summary>
    private sealed class MaterialCard : Panel
    {
        private readonly CyberpunkPalette _palette;
        private readonly int _cut;
        private readonly bool _outlined;

        public MaterialCard(CyberpunkPalette palette, int cut, bool outlined)
        {
            _palette = palette;
            _cut = cut;
            _outlined = outlined;

            BackColor = palette.Surface;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Width <= 2 || Height <= 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var cut = MaterialTheme.Scale(_cut);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // linha diagonal sutil ao fundo, remetendo ao poster do anime
            var diagColor = CyberpunkPalette.Mix(BackColor, _palette.Primary, 0.12);
            using (var diagPen = new Pen(diagColor, 1f))
            {
                var startX = Math.Max(Width / 2, Width - Height * 2);
                e.Graphics.DrawLine(diagPen, startX, Height - 1, Width - 1, 0);
            }

            // barra vertical de destaque a esquerda em amarelo acido
            using (var barBrush = new SolidBrush(_palette.Primary))
            {
                var barWidth = MaterialTheme.Scale(4);
                e.Graphics.FillRectangle(barBrush, 0, cut, barWidth, Math.Max(0, Height - cut * 2));
            }

            if (!_outlined)
            {
                return;
            }

            using var path = ChamferedPath(rect, cut);
            using var pen = new Pen(_palette.Outline, 1.2f);
            e.Graphics.DrawPath(pen, path);

            // cantoneira superior direita em verde neon Sandevistan
            using var accentPen = new Pen(_palette.Accent, 1.5f);
            var bracket = MaterialTheme.Scale(14);
            e.Graphics.DrawLine(accentPen, Width - 1 - bracket, 0, Width - 1, 0);
            e.Graphics.DrawLine(accentPen, Width - 1, 0, Width - 1, bracket);
        }
    }

    /// <summary>
    /// Cabecalho inspirado na arte promocional de Cyberpunk: Edgerunners.
    /// </summary>
    private sealed class EdgerunnersHeaderPanel : Panel
    {
        private readonly string _title;
        private readonly string _subtitle;
        private readonly CyberpunkPalette _palette;

        public EdgerunnersHeaderPanel(string title, string subtitle, CyberpunkPalette palette)
        {
            _title = title;
            _subtitle = subtitle;
            _palette = palette;

            Dock = DockStyle.Fill;
            Height = MaterialTheme.Scale(78);
            MinimumSize = new Size(0, MaterialTheme.Scale(78));
            Margin = new Padding(0, 0, 0, MaterialTheme.Scale(12));
            BackColor = palette.SurfaceRaised;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Width <= 4 || Height <= 4)
            {
                return;
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // fundo em gradiente azul-petroleo profundo de Night City
            var darkBg = CyberpunkPalette.BaseDark;
            var deepTeal = CyberpunkPalette.Mix(darkBg, CyberpunkPalette.BaseAccent, 0.14);
            using (var bgBrush = new LinearGradientBrush(
                       ClientRectangle,
                       darkBg,
                       deepTeal,
                       LinearGradientMode.Horizontal))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // geometria diagonal amarela do poster Edgerunners
            var yellow = CyberpunkPalette.BasePrimary;
            var neonGreen = CyberpunkPalette.BaseAccent;
            using (var geoPen = new Pen(CyberpunkPalette.Mix(darkBg, yellow, 0.35), 1f))
            {
                g.DrawLine(geoPen, Width - MaterialTheme.Scale(220), Height, Width - MaterialTheme.Scale(130), 0);
                g.DrawLine(geoPen, Width - MaterialTheme.Scale(130), 0, Width - MaterialTheme.Scale(50), Height);
            }

            // moldura lateral esquerda em amarelo acido com recorte chanfrado
            var leftWing = new[]
            {
                new Point(0, 0),
                new Point(MaterialTheme.Scale(18), 0),
                new Point(MaterialTheme.Scale(10), MaterialTheme.Scale(14)),
                new Point(MaterialTheme.Scale(10), Height - MaterialTheme.Scale(14)),
                new Point(MaterialTheme.Scale(18), Height),
                new Point(0, Height),
            };
            using (var yellowBrush = new SolidBrush(yellow))
            {
                g.FillPolygon(yellowBrush, leftWing);
                g.FillRectangle(yellowBrush, 0, 0, Width, MaterialTheme.Scale(3));

                // marcadores HUD na borda direita
                var rightW = MaterialTheme.Scale(6);
                g.FillRectangle(yellowBrush, Width - rightW, 0, rightW, Height);
                using var darkBrush = new SolidBrush(darkBg);
                g.FillRectangle(darkBrush, Width - rightW, MaterialTheme.Scale(18), rightW, MaterialTheme.Scale(6));
                g.FillRectangle(darkBrush, Width - rightW, Height - MaterialTheme.Scale(24), rightW, MaterialTheme.Scale(6));
            }

            // feixe horizontal verde neon Sandevistan (como o brilho na mao/coluna do David)
            var flareY = Height - MaterialTheme.Scale(8);
            using (var greenPen = new Pen(neonGreen, MaterialTheme.Scale(2)))
            {
                g.DrawLine(greenPen, MaterialTheme.Scale(24), flareY, Width - MaterialTheme.Scale(24), flareY);
            }

            // estrela/nodo de brilho verde neon a direita
            var flareX = Width - MaterialTheme.Scale(46);
            using (var flareBrush = new SolidBrush(neonGreen))
            {
                var d = MaterialTheme.Scale(5);
                var diamond = new[]
                {
                    new Point(flareX, flareY - d),
                    new Point(flareX + d, flareY),
                    new Point(flareX, flareY + d),
                    new Point(flareX - d, flareY),
                };
                g.FillPolygon(flareBrush, diamond);
            }

            // titulo principal em amarelo acido com leve sombra verde neon
            using var titleFont = new Font("Segoe UI", 15F, FontStyle.Bold);
            var titleText = _title.ToUpperInvariant();
            var titleX = MaterialTheme.Scale(24);
            var titleY = MaterialTheme.Scale(10);

            using (var shadowBrush = new SolidBrush(CyberpunkPalette.Mix(darkBg, neonGreen, 0.55)))
            {
                g.DrawString(titleText, titleFont, shadowBrush, titleX + MaterialTheme.Scale(1), titleY + MaterialTheme.Scale(1));
            }

            using (var titleBrush = new SolidBrush(yellow))
            {
                g.DrawString(titleText, titleFont, titleBrush, titleX, titleY);
            }

            // faixa estilo "///////// EDGERUNNERS" abaixo do titulo
            var badgeY = MaterialTheme.Scale(42);
            var stripeX = titleX;
            using (var stripePen = new Pen(neonGreen, MaterialTheme.Scale(2)))
            {
                for (var i = 0; i < 9; i++)
                {
                    var sx = stripeX + i * MaterialTheme.Scale(6);
                    g.DrawLine(stripePen, sx + MaterialTheme.Scale(4), badgeY, sx, badgeY + MaterialTheme.Scale(16));
                }
            }

            var badgeLeft = stripeX + MaterialTheme.Scale(60);
            using var subFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            var subText = _subtitle.ToUpperInvariant();
            var subSize = g.MeasureString(subText, subFont);
            var badgeWidth = (int)Math.Ceiling(subSize.Width) + MaterialTheme.Scale(18);
            var badgeHeight = MaterialTheme.Scale(18);

            var badgePoly = new[]
            {
                new Point(badgeLeft + MaterialTheme.Scale(5), badgeY),
                new Point(badgeLeft + badgeWidth, badgeY),
                new Point(badgeLeft + badgeWidth - MaterialTheme.Scale(5), badgeY + badgeHeight),
                new Point(badgeLeft, badgeY + badgeHeight),
            };
            using (var badgeBrush = new SolidBrush(yellow))
            using (var badgeTextBrush = new SolidBrush(darkBg))
            {
                g.FillPolygon(badgeBrush, badgePoly);
                g.DrawString(subText, subFont, badgeTextBrush, badgeLeft + MaterialTheme.Scale(8), badgeY + MaterialTheme.Scale(1));
            }
        }
    }
}
