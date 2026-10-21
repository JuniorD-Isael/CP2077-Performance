using System.Text.Json;
using System.Text.Json.Nodes;

namespace CP2077Performance.Core.Backup;

public sealed record BackupEntry(
    string Directory,
    DateTime CreatedAt,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> MissingFiles)
{
    public string Label => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}

public sealed record RestoreOutcome(bool Ok, IReadOnlyList<string> Restored, IReadOnlyList<string> Missing);

/// <summary>
/// Backup automatico das configuracoes, com retencao das tres versoes mais
/// recentes.
/// </summary>
/// <remarks>
/// Ao criar uma versao nova, a mais antiga e apagada: a mais antiga e
/// descartada, a seguinte passa a mais antiga, e assim por diante. O numero de
/// versoes guardadas nunca passa de tres.
/// </remarks>
public sealed class BackupService
{
    public const int RetainedVersions = 3;
    private const string ManifestFileName = "manifest.json";

    private readonly string _backupRoot;

    public BackupService(string backupRoot) =>
        _backupRoot = backupRoot ?? throw new ArgumentNullException(nameof(backupRoot));

    public string BackupRoot => _backupRoot;

    /// <summary>
    /// Cria um backup dos arquivos indicados. Devolve <c>null</c> quando nao ha
    /// arquivo para guardar.
    /// </summary>
    public BackupEntry? Create(IReadOnlyList<string> files)
    {
        var existing = files.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (existing.Count == 0)
        {
            return null;
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var directory = Path.Combine(_backupRoot, stamp);
        Directory.CreateDirectory(directory);

        var manifest = new JsonArray();
        foreach (var file in existing)
        {
            var name = SafeFileName(file);
            File.Copy(file, Path.Combine(directory, name), overwrite: true);
            manifest.Add(new JsonObject
            {
                ["original"] = file,
                ["stored"] = name,
            });
        }

        File.WriteAllText(
            Path.Combine(directory, ManifestFileName),
            new JsonObject { ["files"] = manifest }.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        Prune();

        return Read(directory);
    }

    /// <summary>Descarta as versoes alem das mais recentes.</summary>
    public void Prune()
    {
        var directories = ListDirectories();
        foreach (var stale in directories.Skip(RetainedVersions))
        {
            try
            {
                Directory.Delete(stale, recursive: true);
            }
            catch (IOException)
            {
                // um backup em uso nao impede os demais
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public IReadOnlyList<string> ListDirectories()
    {
        if (!Directory.Exists(_backupRoot))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(_backupRoot)
            .OrderByDescending(d => Path.GetFileName(d), StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<BackupEntry> List() =>
        ListDirectories().Select(Read).Where(e => e is not null).Select(e => e!).ToList();

    /// <summary>Le uma versao de backup.</summary>
    public BackupEntry Read(string directory)
    {
        var created = DateTime.TryParseExact(
            Path.GetFileName(directory),
            "yyyyMMdd-HHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var parsed)
            ? parsed
            : Directory.GetCreationTime(directory);

        var files = new List<string>();
        var missing = new List<string>();

        var manifestPath = Path.Combine(directory, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?["files"] as JsonArray;
            foreach (var entry in manifest ?? new JsonArray())
            {
                if (entry is not JsonObject obj)
                {
                    continue;
                }

                var original = obj["original"]?.GetValue<string>();
                var stored = obj["stored"]?.GetValue<string>();
                if (original is null || stored is null)
                {
                    continue;
                }

                if (File.Exists(Path.Combine(directory, stored)))
                {
                    files.Add(original);
                }
                else
                {
                    missing.Add(original);
                }
            }
        }

        return new BackupEntry(directory, created, files, missing);
    }

    /// <summary>Restaura os arquivos de uma versao de backup.</summary>
    public RestoreOutcome Restore(BackupEntry entry)
    {
        if (entry.MissingFiles.Count > 0)
        {
            return new RestoreOutcome(false, Array.Empty<string>(), entry.MissingFiles);
        }

        var restored = new List<string>();
        foreach (var original in entry.Files)
        {
            var stored = Path.Combine(entry.Directory, SafeFileName(original));
            if (!File.Exists(stored))
            {
                return new RestoreOutcome(false, restored, new[] { original });
            }

            var directory = Path.GetDirectoryName(original);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(stored, original, overwrite: true);
            restored.Add(original);
        }

        return new RestoreOutcome(true, restored, Array.Empty<string>());
    }

    private static string SafeFileName(string path)
    {
        var name = Path.GetFileName(path);
        return string.IsNullOrEmpty(name) ? "arquivo" : name;
    }
}
