using System.Text.Json;
using System.Text.Json.Nodes;
using CP2077Performance.Core.Application;
using CP2077Performance.Core.Backup;
using CP2077Performance.Core.Binary;
using CP2077Performance.Core.GameInstallation;
using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;

namespace CP2077Performance.Ui;

/// <summary>Estado compartilhado pela interface.</summary>
internal sealed class AppState
{
    private const string UiSettingsFileName = "ui.json";

    private readonly string _toolDataRoot = GameLocator.DefaultToolDataRoot;

    public AppState(string embeddedDefaultProfileJson)
    {
        EmbeddedDefaultProfileJson = embeddedDefaultProfileJson;
        Store = new ProfileStore(_toolDataRoot);
        Backups = new BackupService(Path.Combine(_toolDataRoot, "backups"));
        Log = new ChangeLog(_toolDataRoot);

        var load = Store.LoadOrExtract(embeddedDefaultProfileJson);
        Profile = load.Profile;
        ProfileWasExtracted = load.ExtractedDefault;

        var saved = ReadUiSettings();
        GamePath = saved["gamePath"]?.GetValue<string>() ?? "";
        EnabledVariants = new HashSet<string>(StringComparer.Ordinal);
    }

    public string EmbeddedDefaultProfileJson { get; }

    public ProfileStore Store { get; }

    public BackupService Backups { get; }

    public ChangeLog Log { get; }

    public ProfileModel Profile { get; private set; }

    public bool ProfileWasExtracted { get; }

    public string GamePath { get; private set; }

    public HashSet<string> EnabledVariants { get; }

    public GameLocator? Locator { get; private set; }

    public GameSchema? Schema { get; private set; }

    public HashSet<string>? ExecutableTokens { get; private set; }

    public InstallationValidator? Validator { get; private set; }

    public IReadOnlyList<LayerAvailability> Layers { get; private set; } = Array.Empty<LayerAvailability>();

    public string? LastError { get; private set; }

    // ---------------------------------------------------------------- operacoes

    /// <summary>Define a pasta do jogo e revalida tudo.</summary>
    public bool SetGamePath(string path)
    {
        GamePath = path ?? "";
        LastError = null;

        if (string.IsNullOrWhiteSpace(GamePath))
        {
            Locator = null;
            Schema = null;
            ExecutableTokens = null;
            Validator = null;
            Layers = Array.Empty<LayerAvailability>();
            return false;
        }

        Locator = new GameLocator(GamePath);
        Validator = new InstallationValidator(Locator);
        Layers = Validator.Evaluate();

        if (!Validator.IsValidInstallation())
        {
            LastError = "InvalidFolder";
            return false;
        }

        try
        {
            Schema = GameSchemaReader.Read(Locator.GameSchemaPaths);
        }
        catch (JsonException)
        {
            LastError = "InvalidFolder";
            return false;
        }

        // os tokens do executavel sao usados para validar as chaves de cvar
        try
        {
            if (File.Exists(Locator.GameExecutablePath))
            {
                ExecutableTokens = BinaryTokenScanner.ExtractTokens(Locator.GameExecutablePath);
            }
        }
        catch (IOException)
        {
            ExecutableTokens = null;
        }

        WriteUiSettings();
        return true;
    }

    /// <summary>Recarrega o perfil do usuario.</summary>
    public void ReloadProfile()
    {
        if (File.Exists(Store.UserProfilePath))
        {
            Profile = ProfileModel.Parse(File.ReadAllText(Store.UserProfilePath));
        }
    }

    /// <summary>Descarta as edicoes e volta ao perfil curado.</summary>
    public void ResetProfile()
    {
        Profile = ProfileModel.Parse(EmbeddedDefaultProfileJson);
        Store.SaveUserProfile(Profile);
        EnabledVariants.Clear();
    }

    public void SaveProfile()
    {
        Store.SaveUserProfile(Profile);
    }

    /// <summary>Plano de aplicacao, sem escrever nada.</summary>
    public ApplyPlan? Plan()
    {
        if (Locator is null || Schema is null)
        {
            return null;
        }

        var applier = new ProfileApplier(Locator, Backups);
        return applier.Plan(Profile, EnabledVariants, Schema, ExecutableTokens);
    }

    /// <summary>Aplica o perfil.</summary>
    public ApplyResult? Apply()
    {
        if (Locator is null || Schema is null)
        {
            return null;
        }

        var applier = new ProfileApplier(Locator, Backups);
        return applier.Apply(Profile, EnabledVariants, Schema, ExecutableTokens, Log);
    }

    /// <summary>Le as opcoes de menu efetivamente em vigor no arquivo do jogo.</summary>
    public IReadOnlyDictionary<string, string> ReadEffectiveMenuValues()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (Locator is null || !File.Exists(Locator.SettingsPath))
        {
            return result;
        }

        try
        {
            var text = File.ReadAllText(Locator.SettingsPath);
            foreach (var (option, fields) in Core.Writing.JsonFieldLocator.Locate(text))
            {
                result[option] = text[fields[0].Value.Start..fields[0].Value.End];
            }
        }
        catch (IOException)
        {
        }

        return result;
    }

    /// <summary>Uma chave de cvar lida do arquivo do motor.</summary>
    internal sealed record EngineIniEntry(string Section, string Key, string Value, string? Comment);

    /// <summary>Le as cvars do motor e os comentarios do proprio jogo.</summary>
    public IReadOnlyDictionary<string, EngineIniEntry> ReadEngineIni()
    {
        var result = new Dictionary<string, EngineIniEntry>(StringComparer.Ordinal);
        if (Locator is null || !File.Exists(Locator.EngineIniPath))
        {
            return result;
        }

        string? comment = null;
        string section = "";

        foreach (var raw in File.ReadLines(Locator.EngineIniPath))
        {
            var trimmed = raw.Trim();

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                section = trimmed[1..^1];
                comment = null;
                continue;
            }

            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                comment = trimmed.TrimStart(';', '#').Trim();
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = trimmed[..eq].Trim();
            var value = trimmed[(eq + 1)..].Trim();
            result[$"{section}/{key}"] = new EngineIniEntry(section, key, value, comment);
            comment = null;
        }

        return result;
    }

    /// <summary>Painel in-game remanescente de versoes anteriores.</summary>
    public LeftoverPanel? DetectLeftoverPanel() =>
        Locator is null ? null : new LeftoverPanelDetector(Locator).Detect();

    public bool RemoveLeftoverPanel(LeftoverPanel panel) =>
        Locator is not null && new LeftoverPanelDetector(Locator).Remove(panel);

    // ---------------------------------------------------------------- persistencia da UI

    private string UiSettingsPath => Path.Combine(_toolDataRoot, UiSettingsFileName);

    private JsonObject ReadUiSettings()
    {
        try
        {
            if (File.Exists(UiSettingsPath))
            {
                return JsonNode.Parse(File.ReadAllText(UiSettingsPath)) as JsonObject ?? new JsonObject();
            }
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }

        return new JsonObject();
    }

    private void WriteUiSettings()
    {
        try
        {
            Directory.CreateDirectory(_toolDataRoot);
            var settings = new JsonObject { ["gamePath"] = GamePath };
            File.WriteAllText(UiSettingsPath, settings.ToJsonString());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
