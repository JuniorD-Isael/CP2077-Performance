namespace CP2077Performance.Tests;

using CP2077Performance.Core.Display;

internal static class DisplayTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("7.1 o modo de exibicao padrao aplica tela cheia sem tocar na resolucao", () =>
        {
            var fullscreen = DisplayOptions.FullscreenOnly;
            Check.False(fullscreen.IsEmpty, "FullscreenOnly nao e vazio");
            Check.Equal("Fullscreen", fullscreen.WindowMode, "o modo de janela e Fullscreen");
            Check.Equal(2, fullscreen.WindowModeIndex, "o indice do modo de janela no motor e 2");

            Check.True(DisplayOptions.None.IsEmpty, "None e vazio");
        });

        suite.Add("7.2 o modo de janela em tela cheia tem o indice que o motor usa", () =>
        {
            Check.Equal("WindowMode", DisplayModeIndex.WindowModeOption, "nome da opcao WindowMode");
            Check.Equal("Resolution", DisplayModeIndex.ResolutionOption, "nome da opcao Resolution");
            Check.Equal("Fullscreen", DisplayModeIndex.Fullscreen, "valor de tela cheia");
            Check.Equal(2, DisplayModeIndex.FullscreenIndex, "indice de tela cheia no motor");
        });
    }
}
