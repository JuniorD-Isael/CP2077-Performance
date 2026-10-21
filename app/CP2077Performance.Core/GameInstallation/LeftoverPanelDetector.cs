namespace CP2077Performance.Core.GameInstallation;

public sealed record LeftoverPanel(string Path, int FileCount);

/// <summary>
/// Detecta o painel in-game de versoes anteriores, que nao aplica mais nada.
/// </summary>
/// <remarks>
/// O painel vivia no sandbox Lua do Cyber Engine Tweaks, que confine o <c>io</c>
/// ao diretorio do mod. Ele nunca conseguiu escrever no jogo, mas continua
/// carregado a cada boot de quem o tenha instalado, sem efeito e sem que
/// ninguem avise.
/// </remarks>
public sealed class LeftoverPanelDetector
{
    public const string NoticeMessage =
        "Encontrado um painel in-game de uma versao anterior. Ele deixou de " +
        "aplicar qualquer coisa: o sandbox do Cyber Engine Tweaks nao permite " +
        "escrever fora do diretorio do mod. Voce pode remove-lo.";

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
