using System.Text.Json;

namespace CP2077Performance.Core.Schema;

/// <summary>O esquema de opcoes declarado pelo proprio jogo.</summary>
public sealed class GameSchema
{
    private readonly Dictionary<string, GameOptionSchema> _options;

    public GameSchema(IEnumerable<GameOptionSchema> options)
    {
        _options = new Dictionary<string, GameOptionSchema>(StringComparer.Ordinal);
        foreach (var o in options)
        {
            // o primeiro arquivo que declara a opcao vence, como no jogo
            if (!_options.ContainsKey(o.Name))
            {
                _options[o.Name] = o;
            }
        }
    }

    public int Count => _options.Count;

    public IReadOnlyCollection<GameOptionSchema> Options => _options.Values;

    public bool TryGet(string name, out GameOptionSchema option) =>
        _options.TryGetValue(name, out option!);
}

/// <summary>Le o esquema de opcoes do jogo.</summary>
public static class GameSchemaReader
{
    public static GameSchema Read(IEnumerable<string> schemaPaths)
    {
        var options = new List<GameOptionSchema>();

        foreach (var path in schemaPaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("options", out var groups))
            {
                continue;
            }

            foreach (var group in groups.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var groupOptions))
                {
                    continue;
                }

                foreach (var option in groupOptions.EnumerateArray())
                {
                    options.Add(GameOptionSchema.FromJson(option));
                }
            }
        }

        return new GameSchema(options);
    }
}
