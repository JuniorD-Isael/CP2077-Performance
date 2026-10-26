namespace CP2077Performance.Core.Schema;

/// <summary>
/// Confere os valores de opcao de menu de um perfil contra o esquema do jogo.
/// </summary>
public static class ProfileValidator
{
    public static ValidationResult ValidateMenuOptions(
        IReadOnlyDictionary<string, string> values,
        GameSchema schema)
    {
        var issues = new List<ValidationIssue>();

        foreach (var (name, value) in values)
        {
            if (!schema.TryGet(name, out var option))
            {
                issues.Add(new ValidationIssue(
                    ValidationSubject.MenuOption,
                    name,
                    "a opcao nao existe no esquema do jogo",
                    ValidationSeverity.Error));
                continue;
            }

            if (option.HasAcceptedValues && !option.Accepts(value))
            {
                issues.Add(new ValidationIssue(
                    ValidationSubject.MenuOption,
                    name,
                    $"valor '{value}' nao e aceito; o jogo aceita: {string.Join(", ", option.AcceptedValues)}",
                    ValidationSeverity.Error));
            }
        }

        return new ValidationResult(issues);
    }
}
