using CP2077Performance.Ui;

namespace CP2077Performance.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        UiBootstrap.Run();
    }
}
