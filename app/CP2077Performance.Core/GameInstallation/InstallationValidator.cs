namespace CP2077Performance.Core.GameInstallation;

public enum ConfigLayer
{
    /// <summary>Cvars do motor, em <c>user.ini</c>.</summary>
    Engine,

    /// <summary>Opcoes do menu, em <c>UserSettings.json</c>.</summary>
    Menu,

    /// <summary>Patches do Cyber Engine Tweaks, em <c>config.json</c>.</summary>
    Cet,
}

public sealed record LayerAvailability(
    ConfigLayer Layer,
    string TargetPath,
    bool Available,
    string Origin)
{
    public string DisplayName => Layer switch
    {
        ConfigLayer.Engine => "Motor (user.ini)",
        ConfigLayer.Menu => "Menu (UserSettings.json)",
        ConfigLayer.Cet => "Patches do CET (config.json)",
        _ => Layer.ToString(),
    };
}

/// <summary>
/// Valida a pasta de instalacao e determina quais camadas de configuracao
/// podem ser escritas.
/// </summary>
public sealed class InstallationValidator
{
    public InstallationValidator(GameLocator locator) =>
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));

    public GameLocator Locator { get; }

    /// <summary>Uma pasta e uma instalacao valida se contem o executavel do jogo.</summary>
    public bool IsValidInstallation() => File.Exists(Locator.GameExecutablePath);

    /// <summary>
    /// Avalia a disponibilidade de cada camada. A camada de patches e opcional:
    /// uma instalacao sem o Cyber Engine Tweaks e valida e opera sem ela.
    /// </summary>
    public IReadOnlyList<LayerAvailability> Evaluate()
    {
        var engineOrigin = $"{Locator.GameRoot} (pasta do jogo)";
        var menuOrigin = $"{Locator.UserDataRoot} (dados locais do sistema)";

        return new[]
        {
            new LayerAvailability(
                ConfigLayer.Engine,
                Locator.EngineIniPath,
                File.Exists(Locator.EngineIniPath),
                engineOrigin),

            new LayerAvailability(
                ConfigLayer.Menu,
                Locator.SettingsPath,
                File.Exists(Locator.SettingsPath),
                menuOrigin),

            new LayerAvailability(
                ConfigLayer.Cet,
                Locator.CetConfigPath,
                File.Exists(Locator.CetConfigPath),
                engineOrigin),
        };
    }

    /// <summary>Camadas que nao podem ser escritas porque o arquivo nao existe.</summary>
    public IReadOnlyList<LayerAvailability> MissingLayers() =>
        Evaluate().Where(l => !l.Available).ToList();

    /// <summary>Camadasessential para aplicar um perfil completo.</summary>
    public bool CanApplyProfile() =>
        Evaluate().Where(l => l.Layer is ConfigLayer.Engine or ConfigLayer.Menu).All(l => l.Available);
}
