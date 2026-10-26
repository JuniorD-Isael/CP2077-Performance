using CP2077Performance.Core.Backup;
using CP2077Performance.Core.Binary;
using CP2077Performance.Core.Display;
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
    /// <param name="profile">O perfil em uso.</param>
    /// <param name="enabledVariants">As variantes ligadas.</param>
    /// <param name="schema">O esquema do jogo.</param>
    /// <param name="executableTokens">As strings do executavel, para validar as cvars.</param>
    /// <param name="display">
    /// As opcoes de exibicao a escrever. Vem de fora do perfil porque a
    /// resolucao de saida e uma propriedade da maquina, nao do perfil.
    /// </param>
    public ApplyPlan Plan(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema,
        IReadOnlySet<string>? executableTokens,
        DisplayOptions? display = null)
    {
        var issues = new List<ValidationIssue>();

        var effective = VariantResolver.Resolve(profile, enabledVariants, schema);

        // o arquivo de opcoes do jogo: as opcoes de exibicao so sao escritas
        // quando ja existem nele
        var menuText = string.Empty;
        if (File.Exists(_locator.SettingsPath))
        {
            menuText = File.ReadAllText(_locator.SettingsPath);
        }

        var menu = BuildMenuPlan(profile, effective, display, menuText);

        // validacao contra o esquema do jogo
        var menuValues = menu.Desired.ToDictionary(
            k => k.Key,
            k => k.Value.AsString() ?? k.Value.Json,
            StringComparer.Ordinal);
        issues.AddRange(ProfileValidator.ValidateMenuOptions(menuValues, schema).Issues);

        // validacao das chaves de cvar contra o executavel: as do perfil base
        // e as de todas as variantes, ligadas ou nao, para que uma variante com
        // chave escrita errada seja recusada antes de o usuario liga-la
        if (executableTokens is not null)
        {
            issues.AddRange(CvarKeyValidator.Validate(AllUserIni(profile), executableTokens).Issues);
        }

        var validation = new ValidationResult(issues);

        // plano de escrita
        var menuChanges = new List<MenuOptionChange>();
        var missing = new List<string>();
        var menuNewText = string.Empty;

        if (File.Exists(_locator.SettingsPath))
        {
            var menuResult = new MenuOptionsWriter().Plan(menuText, menu.Desired, schema, menu.Indexes);
            menuChanges = menuResult.Changes.ToList();
            missing = menuResult.MissingOptions.ToList();
            menuNewText = menuResult.NewText;
        }

        var iniChanges = new List<IniChange>();
        var iniNewText = string.Empty;
        var iniRemovalsSkipped = new List<IniRemovalSkipped>();
        if (File.Exists(_locator.EngineIniPath))
        {
            var iniText = File.ReadAllText(_locator.EngineIniPath);
            var iniResult = new EngineIniWriter().Plan(
                iniText,
                effective.UserIniValues,
                PreservedKeys(profile),
                effective.RemovableUserIniKeys);
            iniChanges = iniResult.Changes.ToList();
            iniNewText = iniResult.NewText;
            iniRemovalsSkipped = iniResult.SkippedRemovals.ToList();
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
            menu.Origins,
            files,
            iniRemovalsSkipped);
    }

    /// <summary>
    /// Aplica o plano: grava os arquivos, verifica o resultado e registra.
    /// </summary>
    public ApplyResult Apply(
        ProfileModel profile,
        IReadOnlySet<string> enabledVariants,
        GameSchema schema,
        IReadOnlySet<string>? executableTokens,
        ChangeLog? changeLog = null,
        DisplayOptions? display = null)
    {
        if (!_guard.CanProceed(out var reason))
        {
            return ApplyResult.Failed(reason!);
        }

        var plan = Plan(profile, enabledVariants, schema, executableTokens, display);

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

        // a resolucao das variantes nao depende dos arquivos: pode ser feita uma vez
        var effective = VariantResolver.Resolve(profile, enabledVariants, schema);

        // backup antes de escrever
        var backup = _backups.Create(plan.FilesToWrite);
        var backedUp = backup?.Files ?? (IReadOnlyList<string>)Array.Empty<string>();

        // escrita
        var changed = new List<string>();

        if (plan.MenuChanges.Count > 0)
        {
            var text = File.ReadAllText(_locator.SettingsPath);
            var menu = BuildMenuPlan(profile, effective, display, text);
            var result = new MenuOptionsWriter().Plan(text, menu.Desired, schema, menu.Indexes);
            File.WriteAllText(_locator.SettingsPath, result.NewText);
            changed.Add(_locator.SettingsPath);
        }

        if (plan.IniChanges.Count > 0)
        {
            var text = File.ReadAllText(_locator.EngineIniPath);
            var result = new EngineIniWriter().Plan(
                text,
                effective.UserIniValues,
                PreservedKeys(profile),
                effective.RemovableUserIniKeys);
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
            var menu = BuildMenuPlan(profile, effective, display, after);
            verification.AddRange(WriteVerifier.VerifyMenu(after, menu.Desired, schema).Items);
        }

        if (changed.Contains(_locator.EngineIniPath))
        {
            var after = File.ReadAllText(_locator.EngineIniPath);
            verification.AddRange(
                WriteVerifier.VerifyIni(after, effective.UserIniValues, effective.RemovableUserIniKeys).Items);
        }

        var failures = verification.Where(v => !v.Ok).ToList();
        var errors = failures.Select(f => $"{f.Layer}/{f.Target}: {f.Detail}").ToList();

        changeLog?.Append(new ChangeLogEntry(
            DateTime.Now,
            changed,
            plan.MenuChanges
                .Select(c => $"{c.Option}: {c.Before} -> {c.After} ({OriginOf(plan, c.Option)})")
                .Concat(plan.IniChanges.Select(c => DescribeIniChange(c)))
                .Concat(plan.CetChanges.Select(c => $"{c.Patch}: {c.Before} -> {c.After}"))
                .ToList()));

        return new ApplyResult(errors.Count == 0, changed, backedUp, verification, errors);
    }

    /// <summary>
    /// Os valores de menu a escrever: o perfil base, as variantes ligadas, o
    /// que volta ao padrao do jogo, e as opcoes de exibicao.
    /// </summary>
    private sealed record MenuPlan(
        Dictionary<string, ProfileValue> Desired,
        Dictionary<string, int> Indexes,
        Dictionary<string, ValueOrigin> Origins);

    private static MenuPlan BuildMenuPlan(
        ProfileModel profile,
        EffectiveProfile effective,
        DisplayOptions? display,
        string menuText)
    {
        var desired = new Dictionary<string, ProfileValue>(effective.MenuValues, StringComparer.Ordinal);
        var origins = new Dictionary<string, ValueOrigin>(StringComparer.Ordinal);
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);

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

        var activeDisplay = display ?? DisplayOptions.FullscreenOnly;
        if (!activeDisplay.IsEmpty)
        {
            var located = JsonFieldLocator.Locate(menuText);

            if (!string.IsNullOrEmpty(activeDisplay.WindowMode) && located.ContainsKey(DisplayModeIndex.WindowModeOption))
            {
                desired[DisplayModeIndex.WindowModeOption] = ProfileValue.FromString(activeDisplay.WindowMode);
                origins[DisplayModeIndex.WindowModeOption] = ValueOrigin.Display;
                if (activeDisplay.WindowModeIndex is int windowModeIndex)
                {
                    indexes[DisplayModeIndex.WindowModeOption] = windowModeIndex;
                }
            }
        }

        return new MenuPlan(desired, indexes, origins);
    }

    /// <summary>
    /// Todas as chaves de cvar do perfil: as do perfil base e as de todas as
    /// variantes, ligadas ou nao.
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> AllUserIni(ProfileModel profile)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

        foreach (var (section, keys) in profile.UserIni)
        {
            result[section] = keys;
        }

        foreach (var variant in profile.Variants.Values)
        {
            foreach (var (section, keys) in variant.UserIni)
            {
                var map = result.TryGetValue(section, out var existing)
                    ? new Dictionary<string, string>(existing, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var (key, value) in keys)
                {
                    map[key] = value;
                }

                result[section] = map;
            }
        }

        return result;
    }

    private static string DescribeIniChange(IniChange change) =>
        change.After is null
            ? $"{change.Section}/{change.Key}: {change.Before} -> (removida)"
            : $"{change.Section}/{change.Key}: {change.Before ?? "(novo)"} -> {change.After}";

    private static string OriginOf(ApplyPlan plan, string option) =>
        plan.Origins.TryGetValue(option, out var origin) ? origin.ToString() : "perfil";

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
