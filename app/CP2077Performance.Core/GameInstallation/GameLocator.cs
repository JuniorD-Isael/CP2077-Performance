namespace CP2077Performance.Core.GameInstallation;

/// <summary>
/// Deriva todos os alvos de configuracao a partir de dois dados apenas:
/// a pasta do jogo, informada pelo usuario, e o diretorio de dados local do
/// sistema operacional.
/// </summary>
/// <remarks>
/// O diretorio de save do Cyberpunk 2077 e definido pela CD PROJEKT RED e
/// nao pela loja: a mesma instalacao servida por Steam, Epic ou GOG le e
/// grava no mesmo lugar. Por isso uma unica build cobre as tres.
/// </remarks>
public sealed class GameLocator
{
    public const string GameExecutableRelativePath = @"bin\x64\Cyberpunk2077.exe";
    public const string EngineIniRelativePath = @"engine\config\platform\pc\user.ini";
    public const string CetConfigRelativePath = @"bin\x64\plugins\cyber_engine_tweaks\config.json";
    public const string CetModsRelativePath = @"bin\x64\plugins\cyber_engine_tweaks\mods";
    public const string GameSchemaRelativePath = @"r6\config\settings\options.json";
    public const string GameSchemaPlatformRelativePath = @"r6\config\settings\platform\pc\options.json";

    public const string SaveDirectoryName = @"CD Projekt Red\Cyberpunk 2077";
    public const string SettingsFileName = "UserSettings.json";
    public const string ToolDirectoryName = "CP2077-Performance";
    public const string UserProfileFileName = "profile.json";
    public const string BackupsDirectoryName = "backups";

    /// <summary>Nome do painel in-game que existiu em versoes anteriores.</summary>
    public const string LegacyPanelDirectoryName = "PerformanceProfile";

    public GameLocator(string gameRoot)
    {
        GameRoot = gameRoot ?? throw new ArgumentNullException(nameof(gameRoot));
    }

    public string GameRoot { get; }

    public string GameExecutablePath => Path.Combine(GameRoot, GameExecutableRelativePath);

    public string EngineIniPath => Path.Combine(GameRoot, EngineIniRelativePath);

    public string CetConfigPath => Path.Combine(GameRoot, CetConfigRelativePath);

    public string CetModsPath => Path.Combine(GameRoot, CetModsRelativePath);

    public string LegacyPanelPath => Path.Combine(CetModsPath, LegacyPanelDirectoryName);

    public IReadOnlyList<string> GameSchemaPaths => new[]
    {
        Path.Combine(GameRoot, GameSchemaRelativePath),
        Path.Combine(GameRoot, GameSchemaPlatformRelativePath),
    };

    /// <summary>Raiz de dados do usuario. Derivada do sistema, nao da loja.</summary>
    public string UserDataRoot { get; } =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>Raiz de dados da ferramenta, independente da instalacao do jogo.</summary>
    public static string DefaultToolDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ToolDirectoryName);

    public string SaveDirectoryPath => Path.Combine(UserDataRoot, SaveDirectoryName);

    public string SettingsPath => Path.Combine(SaveDirectoryPath, SettingsFileName);

    /// <summary>Diretorio de dados da propria ferramenta.</summary>
    public string ToolDataRoot => Path.Combine(UserDataRoot, ToolDirectoryName);

    public string UserProfilePath => Path.Combine(ToolDataRoot, UserProfileFileName);

    public string BackupRoot => Path.Combine(ToolDataRoot, BackupsDirectoryName);
}
