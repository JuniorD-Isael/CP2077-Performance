using CP2077Performance.Core.Schema;

namespace CP2077Performance.Core.Profile;

/// <summary>
/// Resultado da resolucao das variantes: o que aplicar, e o que precisa
/// voltar ao padrao do jogo.
/// </summary>
/// <param name="MenuValues">Valores de menu a aplicar.</param>
/// <param name="RestoreToDefault">Opcoes de menu que voltam ao padrao do jogo.</param>
/// <param name="EnabledVariants">Identificadores das variantes ligadas.</param>
/// <param name="VariantsWithoutDefault">Opcoes sem padrao declarado no esquema.</param>
/// <param name="UserIniValues">Chaves de cvar a escrever, por secao.</param>
/// <param name="RemovableUserIniKeys">
/// Chaves de cvar que uma variante desligada tinha acrescentado e que devem
/// desaparecer do arquivo do motor. O padrao do jogo para uma cvar e a
/// <em>ausencia</em> da chave, nao um valor.
/// </param>
public sealed record EffectiveProfile(
    IReadOnlyDictionary<string, ProfileValue> MenuValues,
    IReadOnlyDictionary<string, ProfileValue> RestoreToDefault,
    IReadOnlyList<string> EnabledVariants,
    IReadOnlyList<string> VariantsWithoutDefault,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> UserIniValues,
    IReadOnlyList<UserIniKey> RemovableUserIniKeys)
{
    public bool HasWork =>
        MenuValues.Count > 0 || RestoreToDefault.Count > 0 || UserIniValues.Count > 0;
}

/// <summary>
/// Resolve a combinacao de perfil base com as variantes ligadas.
/// </summary>
/// <remarks>
/// Uma variante ligada tem seus valores aplicados por cima do perfil base.
/// Uma variante desligada tem as opcoes que ela afeta devolvidas ao
/// <c>default_value</c> do esquema do jogo, de modo que desligar a variante
/// desfaz o que a ligadura dela fez.
///
/// A variante alcanca as duas camadas. Nas opcoes de menu, o padrao do jogo
/// e um valor, e o esquema o declara. Nas chaves de cvar, o padrao do jogo e a
/// <em>ausencia</em> da chave: o arquivo que o jogo entrega nao a tem. Por isso
/// desligar uma variante remove as chaves que ela teria acrescentado, e o
/// valor esperado fica registrado para que a remocao so ocorra quando o valor
/// em vigor ainda e o da variante.
/// </remarks>
public static class VariantResolver
{
    public static EffectiveProfile Resolve(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema)
    {
        var values = new Dictionary<string, ProfileValue>(profile.UserSettingsValues, StringComparer.Ordinal);
        var restore = new Dictionary<string, ProfileValue>(StringComparer.Ordinal);
        var withoutDefault = new List<string>();

        // chaves de cvar: perfil base primeiro
        var iniValues = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (section, keys) in profile.UserIni)
        {
            iniValues[section] = keys;
        }

        // candidatas a remocao: chaves que variantes desligadas acrescentaram
        var removable = new List<UserIniKey>();

        foreach (var (variantId, variant) in profile.Variants)
        {
            var enabled = enabledVariants.Contains(variantId);

            foreach (var (option, value) in variant.UserSettingsValues)
            {
                if (enabled)
                {
                    values[option] = value;
                    restore.Remove(option);
                    continue;
                }

                if (schema.TryGet(option, out var optionSchema) && optionSchema.DefaultValueText is not null)
                {
                    restore[option] = ProfileValue.FromJson(
                        System.Text.Json.JsonDocument.Parse(optionSchema.DefaultValueText).RootElement);
                }
                else if (!values.ContainsKey(option))
                {
                    // sem padrao declarado e sem valor no perfil base: nada a fazer
                    withoutDefault.Add(option);
                }
            }

            foreach (var (section, keys) in variant.UserIni)
            {
                foreach (var (key, value) in keys)
                {
                    if (enabled)
                    {
                        iniValues[section] = Merge(iniValues, section, key, value);
                        continue;
                    }

                    // o perfil base assume o controle da chave: nada a remover
                    if (profile.UserIni.TryGetValue(section, out var baseKeys) && baseKeys.ContainsKey(key))
                    {
                        continue;
                    }

                    removable.Add(new UserIniKey(section, key, value));
                }
            }
        }

        // uma chave que sobrou em uso nao pode ser removida
        var removalCandidates = new List<UserIniKey>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in removable)
        {
            var id = candidate.Section + "\0" + candidate.Key;
            if (!seen.Add(id))
            {
                continue;
            }

            if (iniValues.TryGetValue(candidate.Section, out var keys) && keys.ContainsKey(candidate.Key))
            {
                continue;
            }

            removalCandidates.Add(candidate);
        }

        var knownEnabled = enabledVariants
            .Where(id => profile.Variants.ContainsKey(id))
            .ToList();

        return new EffectiveProfile(
            values,
            restore,
            knownEnabled,
            withoutDefault,
            iniValues,
            removalCandidates);
    }

    private static IReadOnlyDictionary<string, string> Merge(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sections,
        string section,
        string key,
        string value)
    {
        var map = sections.TryGetValue(section, out var existing)
            ? new Dictionary<string, string>(existing, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        map[key] = value;
        return map;
    }
}
