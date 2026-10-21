namespace CP2077Performance.Core.Profile;

public sealed record ProfileLoadResult(ProfileModel Profile, bool ExtractedDefault);

/// <summary>
/// Carrega o perfil em uso: o do usuario, quando existe; o curado do
/// repositorio, quando nao.
/// </summary>
public sealed class ProfileStore
{
    private readonly string _toolDataRoot;

    public ProfileStore(string toolDataRoot) =>
        _toolDataRoot = toolDataRoot ?? throw new ArgumentNullException(nameof(toolDataRoot));

    public string UserProfilePath => Path.Combine(_toolDataRoot, "profile.json");

    public bool UserProfileExists => File.Exists(UserProfilePath);

    /// <summary>
    /// Carrega o perfil do usuario, extraindo o perfil curado na primeira
    /// execucao. O perfil do usuario nunca e sobrescrito pelo curado.
    /// </summary>
    public ProfileLoadResult LoadOrExtract(string embeddedDefaultJson)
    {
        if (File.Exists(UserProfilePath))
        {
            return new ProfileLoadResult(ProfileModel.Parse(File.ReadAllText(UserProfilePath)), ExtractedDefault: false);
        }

        Directory.CreateDirectory(_toolDataRoot);
        File.WriteAllText(UserProfilePath, embeddedDefaultJson);
        return new ProfileLoadResult(ProfileModel.Parse(embeddedDefaultJson), ExtractedDefault: true);
    }

    public void SaveUserProfile(ProfileModel profile)
    {
        Directory.CreateDirectory(_toolDataRoot);
        File.WriteAllText(UserProfilePath, profile.ToJson());
    }
}
