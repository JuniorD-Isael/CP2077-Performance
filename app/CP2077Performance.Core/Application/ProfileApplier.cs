using CP2077Performance.Core.Backup;
using CP2077Performance.Core.Binary;
using CP2077Performance.Core.GameInstallation;
using CP2077Performance.Core.Profile;
using CP2077Performance.Core.Schema;
using CP2077Performance.Core.Writing;

namespace CP2077Performance.Core.Application;

/// <summary>
/// Planeja e aplica um perfil nas tres camadas de configuracao do jogo.
/// </summary>
public sealed class ProfileApplier
{
    private readonly GameLocator _locator;
    private readonly BackupService _backups;
    private readonly GameRunningGuard _guard = new();

    public ProfileApplier(GameLocator locator, BackupService backups)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _backups = backups ?? throw new ArgumentNullException(nameof(backups));
    }

    /// <summary>Camadas disponiveis na instalacao.</summary>
    public IReadOnlyList<LayerAvailability> Layers() =>
        new InstallationValidator(_locator).Evaluate();

    /// <summary>
    /// Monta o plano de aplicacao sem escrever nada.
    /// </summary>
    public ApplyPlan Plan(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema,
        IReadOnlySet<string>? executableTokens)
    {
        var issues = new List<ValidationIssue>();

        var effective = VariantResolver.Resolve(profile, enabledVariants, schema);

        // valores a aplicar: perfil base + variantes ligadas
        var desired = new Dictionary<string, ProfileValue>(effective.MenuValues, StringComparer.Ordinal);
        var origins = new Dictionary<string, ValueOrigin>(StringComparer.Ordinal);

        foreach (var (option, value) in profile.UserSettingsValues)
        {
            origins[option] = ValueOrigin.ProfileBase;
        }

        foreach (var variantId in effective.EnabledVariants)
        {
            if (profile.Variants.TryGetValue(variantId, out var variant))
            {
                foreach (var (option, value) in variant.UserSettingsValues)
                {
                    origins[option] = ValueOrigin.Variant;
                }
            }
        }

        foreach (var (option, value) in effective.RestoreToDefault)
        {
            desired[option] = value;
            origins[option] = ValueOrigin.SchemaDefault;
        }

        // validacao contra o esquema do jogo
        var menuValues = desired.ToDictionary(
            k => k.Key,
            k => k.Value.AsString() ?? k.Value.Json,
            StringComparer.Ordinal);
        issues.AddRange(ProfileValidator.ValidateMenuOptions(menuValues, schema).Issues);

        // validacao das chaves de cvar contra o executavel
        if (executableTokens is not null)
        {
            issues.AddRange(CvarKeyValidator.Validate(profile.UserIni, executableTokens).Issues);
        }

        var validation = new ValidationResult(issues);

        // plano de escrita
        var menuChanges = new List<MenuOptionChange>();
        var missing = new List<string>();
        var menuText = string.Empty;
        var menuNewText = string.Empty;

        if (File.Exists(_locator.SettingsPath))
        {
            menuText = File.ReadAllText(_locator.SettingsPath);
            var menuResult = new MenuOptionsWriter().Plan(menuText, desired, schema);
            menuChanges = menuResult.Changes.ToList();
            missing = menuResult.MissingOptions.ToList();
            menuNewText = menuResult.NewText;
        }

        var iniChanges = new List<IniChange>();
        var iniNewText = string.Empty;
        if (File.Exists(_locator.EngineIniPath))
        {
            var iniText = File.ReadAllText(_locator.EngineIniPath);
            var iniResult = new EngineIniWriter().Plan(
                iniText,
                profile.UserIni,
                PreservedKeys(profile));
            iniChanges = iniResult.Changes.ToList();
            iniNewText = iniResult.NewText;
        }

        var cetChanges = new List<PatchChange>();
        var cetNewText = string.Empty;
        if (File.Exists(_locator.CetConfigPath))
        {
            var cetText = File.ReadAllText(_locator.CetConfigPath);
            var cetResult = new CetConfigWriter().Plan(cetText, profile.CetPatches);
            cetChanges = cetResult.Changes.ToList();
            cetNewText = cetResult.NewText;
        }

        var files = new List<string>();
        if (menuChanges.Count > 0) files.Add(_locator.SettingsPath);
        if (iniChanges.Count > 0) files.Add(_locator.EngineIniPath);
        if (cetChanges.Count > 0) files.Add(_locator.CetConfigPath);

        return new ApplyPlan(
            validation,
            menuChanges,
            iniChanges,
            cetChanges,
            missing,
            origins,
            files);
    }

    /// <summary>
    /// Aplica o plano: grava os arquivos, verifica o resultado e registra.
    /// </summary>
    public ApplyResult Apply(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema,
        IReadOnlySet<string>? executableTokens,
        ChangeLog? changeLog = null)
    {
        if (!_guard.CanProceed(out var reason))
        {
            return ApplyResult.Failed(reason!);
        }

        var plan = Plan(profile, enabledVariants, schema, executableTokens);

        if (!plan.CanApply)
        {
            var planErrors = plan.Validation.Errors.Select(e => e.ToString()).ToList();
            planErrors.AddRange(plan.MissingMenuOptions.Select(m => $"a opcao '{m}' nao existe no arquivo de opcoes do jogo"));
            return ApplyResult.Failed(string.Join(Environment.NewLine, planErrors));
        }

        if (!plan.ChangesAnything)
        {
            return new ApplyResult(true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<WriteVerificationItem>(), Array.Empty<string>());
        }

        // backup antes de escrever
        var backup = _backups.Create(plan.FilesToWrite);
        var backedUp = backup?.Files ?? (IReadOnlyList<string>)Array.Empty<string>();

        // escrita
        var changed = new List<string>();

        if (plan.MenuChanges.Count > 0)
        {
            var text = File.ReadAllText(_locator.SettingsPath);
            var result = new MenuOptionsWriter().Plan(text, DesiredValues(profile, enabledVariants, schema), schema);
            File.WriteAllText(_locator.SettingsPath, result.NewText);
            changed.Add(_locator.SettingsPath);
        }

        if (plan.IniChanges.Count > 0)
        {
            var text = File.ReadAllText(_locator.EngineIniPath);
            var result = new EngineIniWriter().Plan(text, profile.UserIni, PreservedKeys(profile));
            File.WriteAllText(_locator.EngineIniPath, result.NewText);
            changed.Add(_locator.EngineIniPath);
        }

        if (plan.CetChanges.Count > 0)
        {
            var text = File.ReadAllText(_locator.CetConfigPath);
            var result = new CetConfigWriter().Plan(text, profile.CetPatches);
            File.WriteAllText(_locator.CetConfigPath, result.NewText);
            changed.Add(_locator.CetConfigPath);
        }

        // verificacao pos-escrita
        var verification = new List<WriteVerificationItem>();

        if (changed.Contains(_locator.SettingsPath))
        {
            var after = File.ReadAllText(_locator.SettingsPath);
            verification.AddRange(WriteVerifier.VerifyMenu(after, DesiredValues(profile, enabledVariants, schema), schema).Items);
        }

        if (changed.Contains(_locator.EngineIniPath))
        {
            var after = File.ReadAllText(_locator.EngineIniPath);
            verification.AddRange(WriteVerifier.VerifyIni(after, profile.UserIni).Items);
        }

        var failures = verification.Where(v => !v.Ok).ToList();
        var errors = failures.Select(f => $"{f.Layer}/{f.Target}: {f.Detail}").ToList();

        changeLog?.Append(new ChangeLogEntry(
            DateTime.Now,
            changed,
            plan.MenuChanges
                .Select(c => $"{c.Option}: {c.Before} -> {c.After} ({OriginOf(plan, c.Option)})")
                .Concat(plan.IniChanges.Select(c => $"{c.Section}/{c.Key}: {c.Before ?? "(novo)"} -> {c.After}"))
                .Concat(plan.CetChanges.Select(c => $"{c.Patch}: {c.Before} -> {c.After}"))
                .ToList()));

        return new ApplyResult(errors.Count == 0, changed, backedUp, verification, errors);
    }

    private static string OriginOf(ApplyPlan plan, string option) =>
        plan.Origins.TryGetValue(option, out var origin) ? origin.ToString() : "perfil";

    private static IReadOnlyDictionary<string, ProfileValue> DesiredValues(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema)
    {
        var effective = VariantResolver.Resolve(profile, enabledVariants, schema);
        var desired = new Dictionary<string, ProfileValue>(effective.MenuValues, StringComparer.Ordinal);
        foreach (var (option, value) in effective.RestoreToDefault)
        {
            desired[option] = value;
        }

        return desired;
    }

    private static IReadOnlySet<string> PreservedKeys(ProfileModel profile)
    {
        var preserved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (section, keys) in profile.PreservedUserIni)
        {
            foreach (var key in keys)
            {
                preserved.Add(section + "\0" + key);
            }
        }

        return preserved;
    }
}
