namespace CP2077Performance.Core.Display;

/// <summary>
/// O que o aplicativo escreve nas opcoes de exibicao do jogo.
/// </summary>
/// <remarks>
/// O aplicativo escreve sempre <c>WindowMode = "Fullscreen"</c> com o indice
/// <c>2</c> do motor e preserva intacta a opcao <c>Resolution</c> configurada
/// pelo usuario no jogo, qualquer que seja o tamanho do monitor ou a resolucao
/// escolhida.
/// </remarks>
/// <param name="WindowMode">O valor do modo de janela, ou vazio.</param>
/// <param name="WindowModeIndex">O indice do modo de janela no motor.</param>
public sealed record DisplayOptions(
    string WindowMode,
    int? WindowModeIndex)
{
    /// <summary>Nada a escrever.</summary>
    public static DisplayOptions None { get; } = new("", null);

    /// <summary>
    /// Modo de janela em tela cheia (<c>Fullscreen</c>, indice <c>2</c>), sem
    /// tocar na resolucao do jogo.
    /// </summary>
    public static DisplayOptions FullscreenOnly { get; } = new(
        DisplayModeIndex.Fullscreen,
        DisplayModeIndex.FullscreenIndex);

    /// <summary>Verdadeiro quando nao ha nada a escrever.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(WindowMode);
}
