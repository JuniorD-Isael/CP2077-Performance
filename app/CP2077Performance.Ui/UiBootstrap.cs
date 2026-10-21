using CP2077Performance.Core.GameInstallation;

namespace CP2077Performance.Ui;

/// <summary>
/// Ponto de entrada da interface. Mantido no assembly de UI para que o
/// executavel nao precise conhecer detalhes de apresentacao.
/// </summary>
public static class UiBootstrap
{
    /// <summary>Recurso com o perfil curado que acompanha o executavel.</summary>
    public const string EmbeddedProfileResource = "CP2077Performance.Profile.default.json";

    public static void Run()
    {
        ThemeHelper.ApplySystemTheme();

        var embedded = ReadEmbeddedProfile();
        var state = new AppState(embedded);

        Application.Run(new MainForm(state));
    }

    private static string ReadEmbeddedProfile()
    {
        // o recurso e embarcado no assembly do executavel, que referencia a UI
        var appAssembly = System.Reflection.Assembly.GetEntryAssembly();
        using var stream = appAssembly?.GetManifestResourceStream(EmbeddedProfileResource)
            ?? typeof(UiBootstrap).Assembly.GetManifestResourceStream(EmbeddedProfileResource);

        if (stream is null)
        {
            return "{}";
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
