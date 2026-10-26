using CP2077Performance.Core.Profile;

namespace CP2077Performance.Core.Writing;

public sealed record IniChange(string Section, string Key, string? Before, string? After);

/// <summary>
/// Chave que deveria ter sido removida e nao foi, porque o valor em vigor
/// deixou de ser o da variante. O valor foi ajustado a mao pelo usuario.
/// </summary>
public sealed record IniRemovalSkipped(string Section, string Key, string CurrentValue, string ExpectedValue);

public sealed record IniWriteResult(
    string NewText,
    IReadOnlyList<IniChange> Changes,
    IReadOnlyList<IniRemovalSkipped> SkippedRemovals)
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
///
/// A escrita tambem remove chaves. O padrao do jogo para uma cvar e a ausencia
/// dela, e nao um valor: o arquivo que o jogo entrega nao tem a chave. Por isso
/// desligar uma variante tem de apagar o que ela escrevesse. A remocao so
/// acontece quando o valor em vigor ainda e exatamente o da variante; quando o
/// usuario ajustou o valor a mao, a chave fica e a divergencia e reportada.
/// </remarks>
public sealed class EngineIniWriter
{
    public IniWriteResult Plan(
        string text,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> desired,
        IReadOnlySet<string>? preservedKeys = null,
        IReadOnlyList<UserIniKey>? removableKeys = null)
    {
        var lines = text.Split('\n').ToList();
        var changes = new List<IniChange>();
        var skipped = new List<IniRemovalSkipped>();

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

        if (removableKeys is not null)
        {
            RemoveKeys(lines, desired, preservedKeys, removableKeys, changes, skipped);
        }

        return new IniWriteResult(string.Join('\n', lines), changes, skipped);
    }

    /// <summary>
    /// Remove as chaves que uma variante desligada tinha acrescentado. As
    /// secoes sao relocalizadas a cada remocao, porque as linhas mudam.
    /// </summary>
    private static void RemoveKeys(
        List<string> lines,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> desired,
        IReadOnlySet<string>? preservedKeys,
        IReadOnlyList<UserIniKey> removableKeys,
        List<IniChange> changes,
        List<IniRemovalSkipped> skipped)
    {
        foreach (var candidate in removableKeys)
        {
            // o valor desejado assume o controle da chave
            if (desired.TryGetValue(candidate.Section, out var wanted) && wanted.ContainsKey(candidate.Key))
            {
                continue;
            }

            if (preservedKeys is not null && preservedKeys.Contains(KeyOf(candidate.Section, candidate.Key)))
            {
                continue;
            }

            var sectionStart = FindSectionStart(lines, candidate.Section);
            if (sectionStart < 0)
            {
                // a secao nao existe: a chave ja esta no padrao do jogo
                continue;
            }

            var sectionEnd = FindSectionEnd(lines, sectionStart);
            var line = FindKeyLine(lines, sectionStart, sectionEnd, candidate.Key);
            if (line < 0)
            {
                // a chave ja esta ausente: o padrao do jogo
                continue;
            }

            var current = ReadValue(lines[line]);
            if (current is null || !string.Equals(current, candidate.Value, StringComparison.Ordinal))
            {
                skipped.Add(new IniRemovalSkipped(
                    candidate.Section,
                    candidate.Key,
                    current ?? "",
                    candidate.Value));
                continue;
            }

            lines.RemoveAt(line);
            changes.Add(new IniChange(candidate.Section, candidate.Key, current, null));
        }
    }

    private static int FindSectionStart(List<string> lines, string section)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var name = ReadSectionName(lines[i]);
            if (name is not null && string.Equals(name, section, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindSectionEnd(List<string> lines, int sectionStart)
    {
        for (var i = sectionStart + 1; i < lines.Count; i++)
        {
            if (ReadSectionName(lines[i]) is not null)
            {
                return i;
            }
        }

        return lines.Count;
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
