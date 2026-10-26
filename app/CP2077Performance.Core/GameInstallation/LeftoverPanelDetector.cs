namespace CP2077Performance.Core.GameInstallation;

public sealed record LeftoverPanel(string Path, int FileCount);

/// <summary>Mod de HUD remanescente de versoes anteriores, que não deve ser removido automaticamente.</summary>
public sealed record LeftoverHudMod(string Path, int FileCount);

/// <summary>
/// Detecta o painel in-game e o mod de HUD de versoes anteriores.
/// </summary>
/// <remarks>
/// O painel vivia no sandbox Lua do Cyber Engine Tweaks, que confine o <c>io</c>
/// ao diretorio do mod. Ele nunca conseguiu escrever no jogo, mas continua
/// carregado a cada boot de quem o tenha instalado, sem efeito e sem que
/// ninguem avise. O mod de HUD (`CP2077Performance`) saiu para um projeto
/// separado e o aplicativo apenas reporta a presenca dele, sem apaga-lo.
/// </remarks>
public sealed class LeftoverPanelDetector
{
    public const string NoticeMessage =
        "Encontrado um painel in-game de uma versao anterior. Ele deixou de " +
        "aplicar qualquer coisa: o sandbox do Cyber Engine Tweaks nao permite " +
        "escrever fora do diretorio do mod. Voce pode remove-lo.";

    public const string HudModNoticeMessage =
        "Encontrado o mod CP2077Performance de uma versao anterior. A alternancia " +
        "de HUD agora pertence a um projeto separado e este aplicativo nao o " +
        "gerencia mais.";

    private readonly GameLocator _locator;

    public LeftoverPanelDetector(GameLocator locator) =>
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));

    public LeftoverPanel? Detect()
    {
        var path = _locator.LegacyPanelPath;
        if (!Directory.Exists(path))
        {
            return null;
        }

        var count = Directory.Exists(path)
            ? Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length
            : 0;

        return new LeftoverPanel(path, count);
    }

    /// <summary>
    /// Detecta o mod de HUD de versao anterior, sem oferecer remocao.
    /// </summary>
    public LeftoverHudMod? DetectHudMod()
    {
        var path = _locator.LegacyHudModPath;
        if (!Directory.Exists(path))
        {
            return null;
        }

        var count = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
        return new LeftoverHudMod(path, count);
    }

    /// <summary>Remove o painel remanescente.</summary>
    public bool Remove(LeftoverPanel panel)
    {
        try
        {
            if (Directory.Exists(panel.Path))
            {
                Directory.Delete(panel.Path, recursive: true);
            }

            return !Directory.Exists(panel.Path);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
