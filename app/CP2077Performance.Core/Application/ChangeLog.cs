using System.Text.Json;
using System.Text.Json.Nodes;

namespace CP2077Performance.Core.Application;

public sealed record ChangeLogEntry(
    DateTime Timestamp,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Details);

/// <summary>
/// Registro das aplicacoes, para que o usuario veja o que mudou e de onde
/// veio cada valor.
/// </summary>
public sealed class ChangeLog
{
    private readonly string _path;

    public ChangeLog(string toolDataRoot)
    {
        // a propriedade Path sombreia System.IO.Path neste escopo
        _path = System.IO.Path.Combine(toolDataRoot, "changes.jsonl");
    }

    public string Path => _path;

    public void Append(ChangeLogEntry entry)
    {
        var directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        var line = new JsonObject
        {
            ["timestamp"] = entry.Timestamp.ToString("o"),
            ["files"] = new JsonArray(entry.Files.Select(f => (JsonNode)f!).ToArray()),
            ["details"] = new JsonArray(entry.Details.Select(d => (JsonNode)d!).ToArray()),
        }.ToJsonString();

        File.AppendAllText(_path, line + Environment.NewLine);
    }

    public IReadOnlyList<ChangeLogEntry> Read(int limit = 100)
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<ChangeLogEntry>();
        }

        var entries = new List<ChangeLogEntry>();

        foreach (var line in File.ReadLines(_path).Reverse().Take(limit))
        {
            try
            {
                var node = JsonNode.Parse(line) as JsonObject;
                if (node is null)
                {
                    continue;
                }

                entries.Add(new ChangeLogEntry(
                    DateTime.TryParse(node["timestamp"]?.GetValue<string>(), out var ts)
                        ? ts
                        : DateTime.MinValue,
                    (node["files"] as JsonArray)?.Select(n => n?.GetValue<string>() ?? "").ToList() ?? new List<string>(),
                    (node["details"] as JsonArray)?.Select(n => n?.GetValue<string>() ?? "").ToList() ?? new List<string>()));
            }
            catch (JsonException)
            {
                // uma linha corrompida nao invalida o registro inteiro
            }
        }

        return entries;
    }
}
