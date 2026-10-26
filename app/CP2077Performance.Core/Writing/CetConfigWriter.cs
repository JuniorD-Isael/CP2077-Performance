using System.Text.RegularExpressions;

namespace CP2077Performance.Core.Writing;

public sealed record PatchChange(string Patch, bool Before, bool After);

public sealed record CetWriteResult(
    string NewText,
    IReadOnlyList<PatchChange> Changes,
    bool Available)
{
    public bool Changed => Changes.Count > 0;
}

/// <summary>
/// Escreve os patches do Cyber Engine Tweaks no <c>config.json</c>.
/// </summary>
/// <remarks>
/// A camada e opcional: uma instalacao sem o Cyber Engine Tweaks e valida e
/// opera sem ela, e nao e falha.
/// </remarks>
public sealed class CetConfigWriter
{
    private static readonly Regex PatchPattern = new(
        @"""(?<name>[A-Za-z0-9_]+)""\s*:\s*(?<value>true|false)",
        RegexOptions.Compiled);

    public CetWriteResult Plan(string text, IReadOnlyDictionary<string, bool> desired)
    {
        var located = new Dictionary<string, (int Start, int Length, bool Value)>(StringComparer.Ordinal);

        foreach (Match m in PatchPattern.Matches(text))
        {
            var name = m.Groups["name"].Value;
            var value = m.Groups["value"].Value == "true";
            if (!located.ContainsKey(name))
            {
                located[name] = (m.Groups["value"].Index, m.Groups["value"].Length, value);
            }
        }

        var changes = new List<PatchChange>();
        var edits = new List<(int Start, int Length, string Replacement)>();

        foreach (var (patch, value) in desired)
        {
            if (!located.TryGetValue(patch, out var span))
            {
                // patch declarado no perfil e ausente do config do CET
                continue;
            }

            if (span.Value == value)
            {
                continue;
            }

            edits.Add((span.Start, span.Length, value ? "true" : "false"));
            changes.Add(new PatchChange(patch, span.Value, value));
        }

        var newText = text;
        foreach (var (start, length, replacement) in edits.OrderByDescending(e => e.Start))
        {
            newText = newText[..start] + replacement + newText[(start + length)..];
        }

        return new CetWriteResult(newText, changes, Available: true);
    }
}
