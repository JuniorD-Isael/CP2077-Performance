namespace CP2077Performance.Core.Writing;

public sealed record IniChange(string Section, string Key, string? Before, string After);

public sealed record IniWriteResult(
    string NewText,
    IReadOnlyList<IniChange> Changes)
{
    public bool Changed => Changes.Count > 0;
}

/// <summary>
/// Manipula o arquivo de cvars do motor preservando comentarios, ordem de
/// secoes e ordem de chaves.
/// </summary>
/// <remarks>
/// O arquivo do motor carrega comentarios que documentam efeitos colaterais
/// (<c>;Don't disable. (breaks lightmaps)</c>), e a ordem das secoes e das
/// chaves e informacao. Um parser de INI de uso geral nao preserva nada disso
/// por padrao, entao a manipulacao e feita sobre o texto.
/// </remarks>
public sealed class EngineIniWriter
{
    public IniWriteResult Plan(
        string text,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> desired,
        IReadOnlySet<string>? preservedKeys = null)
    {
        var lines = text.Split('\n').ToList();
        var changes = new List<IniChange>();

        // mapa: secao -> indice da linha do cabecalho
        var sectionStarts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < lines.Count; i++)
        {
            var name = ReadSectionName(lines[i]);
            if (name is not null && !sectionStarts.ContainsKey(name))
            {
                sectionStarts[name] = i;
            }
        }

        foreach (var (section, keys) in desired)
        {
            var isNewSection = !sectionStarts.TryGetValue(section, out var sectionStart);

            if (isNewSection)
            {
                // secao nova ao final do arquivo
                if (lines.Count > 0 && lines[^1].Trim().Length > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add($"[{section}]");
                foreach (var (key, value) in keys)
                {
                    if (preservedKeys is not null && preservedKeys.Contains(KeyOf(section, key)))
                    {
                        continue;
                    }

                    lines.Add($"{key} = {value}");
                    changes.Add(new IniChange(section, key, null, value));
                }

                sectionStarts[section] = lines.Count - 1 - keys.Count;
                continue;
            }

            // limites da secao
            var sectionEnd = lines.Count;
            for (var i = sectionStart + 1; i < lines.Count; i++)
            {
                if (ReadSectionName(lines[i]) is not null)
                {
                    sectionEnd = i;
                    break;
                }
            }

            // indice da proxima chave a inserir, para manter a ordem
            var insertAt = sectionEnd;

            foreach (var (key, value) in keys)
            {
                if (preservedKeys is not null && preservedKeys.Contains(KeyOf(section, key)))
                {
                    continue;
                }

                var existing = FindKeyLine(lines, sectionStart, sectionEnd, key);
                if (existing >= 0)
                {
                    var before = ReadValue(lines[existing]);
                    if (!string.Equals(before, value, StringComparison.Ordinal))
                    {
                        lines[existing] = $"{key} = {value}";
                        changes.Add(new IniChange(section, key, before, value));
                    }

                    continue;
                }

                lines.Insert(insertAt, $"{key} = {value}");
                changes.Add(new IniChange(section, key, null, value));
                insertAt++;
                sectionEnd++;
            }
        }

        return new IniWriteResult(string.Join('\n', lines), changes);
    }

    private static string KeyOf(string section, string key) => section + "\0" + key;

    private static string? ReadSectionName(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length > 1 && trimmed[0] == '[' && trimmed[^1] == ']'
            ? trimmed[1..^1]
            : null;
    }

    private static int FindKeyLine(List<string> lines, int sectionStart, int sectionEnd, string key)
    {
        for (var i = sectionStart + 1; i < sectionEnd && i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0 || trimmed[0] == ';' || trimmed[0] == '#')
            {
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var name = trimmed[..eq].Trim();
            if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string? ReadValue(string line)
    {
        var eq = line.IndexOf('=');
        return eq < 0 ? null : line[(eq + 1)..].Trim();
    }
}
