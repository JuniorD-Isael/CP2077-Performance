namespace CP2077Performance.Core.Display;

/// <summary>
/// Os nomes das opcoes de exibicao do jogo e o indice do modo de janela em
/// tela cheia que o motor usa.
/// </summary>
/// <remarks>
/// O esquema do jogo declara <c>WindowMode</c> e <c>Resolution</c> com
/// <c>is_dynamic: true</c> e <em>sem</em> lista de valores no schema estatico.
/// A medicao em runtime (<c>docs/FINDINGS.md</c> §15.3–§15.4) provou que a
/// lista de modos de janela do motor grava <c>Fullscreen</c> com indice
/// <c>2</c> e resolve pelo campo <c>value</c>.
/// </remarks>
public static class DisplayModeIndex
{
    /// <summary>A opcao de modo de janela do jogo.</summary>
    public const string WindowModeOption = "WindowMode";

    /// <summary>A opcao de resolucao do jogo.</summary>
    public const string ResolutionOption = "Resolution";

    /// <summary>O modo de janela em tela cheia.</summary>
    public const string Fullscreen = "Fullscreen";

    /// <summary>O indice que o motor atribui a <c>Fullscreen</c>.</summary>
    public const int FullscreenIndex = 2;
}
