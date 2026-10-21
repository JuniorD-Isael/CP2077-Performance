namespace CP2077Performance.Core.Schema;

public enum ValidationSeverity
{
    Error,
    Warning,
}

public enum ValidationSubject
{
    MenuOption,
    CvarKey,
    CvarSection,
    Profile,
    Installation,
}

public sealed record ValidationIssue(
    ValidationSubject Subject,
    string Name,
    string Message,
    ValidationSeverity Severity)
{
    public override string ToString() =>
        $"[{Severity}] {Subject}/{Name}: {Message}";
}

/// <summary>Resultado de uma validacao.</summary>
public sealed class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationIssue> issues) =>
        Issues = issues.ToList();

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);

    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error);

    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);
}
