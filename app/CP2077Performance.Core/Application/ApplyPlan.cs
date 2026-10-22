using CP2077Performance.Core.Schema;
using CP2077Performance.Core.Writing;

namespace CP2077Performance.Core.Application;

/// <summary>De onde veio o valor aplicado a uma opcao.</summary>
public enum ValueOrigin
{
    ProfileBase,
    Variant,
    SchemaDefault,

    /// <summary>
    /// De uma opcao de exibicao, que vem da maquina e nao do perfil.
    /// </summary>
    Display,
}

public sealed record ApplyPlan(
    ValidationResult Validation,
    IReadOnlyList<MenuOptionChange> MenuChanges,
    IReadOnlyList<IniChange> IniChanges,
    IReadOnlyList<PatchChange> CetChanges,
    IReadOnlyList<string> MissingMenuOptions,
    IReadOnlyDictionary<string, ValueOrigin> Origins,
    IReadOnlyList<string> FilesToWrite,
    IReadOnlyList<IniRemovalSkipped> IniRemovalsSkipped)
{
    public bool CanApply => !Validation.HasErrors && MissingMenuOptions.Count == 0;

    public bool ChangesAnything =>
        MenuChanges.Count > 0 || IniChanges.Count > 0 || CetChanges.Count > 0;
}

public sealed record ApplyResult(
    bool Ok,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<string> BackedUpFiles,
    IReadOnlyList<WriteVerificationItem> Verification,
    IReadOnlyList<string> Errors)
{
    public static ApplyResult Failed(string error) =>
        new(false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<WriteVerificationItem>(), new[] { error });
}
