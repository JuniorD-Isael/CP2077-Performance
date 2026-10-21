using CP2077Performance.Core.Schema;

namespace CP2077Performance.Core.Profile;

/// <summary>
/// Resultado da resolucao das variantes: o que aplicar, e o que precisa
/// voltar ao padrao do jogo.
/// </summary>
public sealed record EffectiveProfile(
    IReadOnlyDictionary<string, ProfileValue> MenuValues,
    IReadOnlyDictionary<string, ProfileValue> RestoreToDefault,
    IReadOnlyList<string> EnabledVariants,
    IReadOnlyList<string> VariantsWithoutDefault)
{
    public bool HasWork => MenuValues.Count > 0 || RestoreToDefault.Count > 0;
}

/// <summary>
/// Resolve a combinacao de perfil base com as variantes ligadas.
/// </summary>
/// <remarks>
/// Uma variante ligada tem seus valores aplicados por cima do perfil base.
/// Uma variante desligada tem as opcoes que ela afeta devolvidas ao
/// <c>default_value</c> do esquema do jogo, de modo que desligar a variante
/// desfaz o que a ligadura dela fez.
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
        }

        return new EffectiveProfile(
            values,
            restore,
            enabledVariants.ToList(),
            withoutDefault);
    }
}
