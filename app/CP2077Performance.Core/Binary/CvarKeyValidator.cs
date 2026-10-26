using CP2077Performance.Core.Schema;

namespace CP2077Performance.Core.Binary;

/// <summary>
/// Confere as chaves de cvar de um perfil contra as strings do executavel.
/// </summary>
/// <remarks>
/// A ausencia de um token e sinal forte de erro de digitacao: o motor nao
/// consegue casar um nome que nao esta no executavel, e ignora chaves
/// desconhecidas em silencio.
///
/// A presenca, por outro lado, prova apenas que o <em>nome</em> existe. Nao
/// prova que o motor o leia como cvar: isso exigiria o mapa de xrefs entre a
/// funcao C++ que registra a opcao e a string, que ainda nao foi produzido.
/// </remarks>
public sealed class CvarKeyValidator
{
    /// <summary>
    /// Chaves conhecidas por nao aparecerem como token isolado no executavel:
    /// <c>GlassUseMipChain</c> vem do <c>user.ini</c> original do jogo
    /// (<c>docs/FINDINGS.md</c> secao 10.4) e <c>ExtendedDuringSSR</c> e
    /// confirmada em runtime pelo hook <c>HookGameOptionInit</c> do CET
    /// (<c>docs/FINDINGS.md</c> secao 10.3).
    /// </summary>
    public static readonly IReadOnlySet<string> KnownAbsentKeys =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "GlassUseMipChain",
            "ExtendedDuringSSR",
        };

    public const string KnownAbsentDocumentation = "docs/FINDINGS.md secao 10.4";

    public static ValidationResult Validate(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> userIni,
        IReadOnlySet<string> executableTokens)
    {
        var issues = new List<ValidationIssue>();

        foreach (var (section, keys) in userIni)
        {
            if (!executableTokens.Contains(section))
            {
                issues.Add(new ValidationIssue(
                    ValidationSubject.CvarSection,
                    section,
                    "a secao nao aparece como string no executavel; a associacao secao/chave nao pode ser confirmada",
                    ValidationSeverity.Warning));
            }

            foreach (var key in keys.Keys)
            {
                if (executableTokens.Contains(key))
                {
                    continue;
                }

                if (KnownAbsentKeys.Contains(key))
                {
                    issues.Add(new ValidationIssue(
                        ValidationSubject.CvarKey,
                        key,
                        $"nao existe no executavel, mas vem do user.ini original do proprio jogo ({KnownAbsentDocumentation})",
                        ValidationSeverity.Warning));
                    continue;
                }

                issues.Add(new ValidationIssue(
                    ValidationSubject.CvarKey,
                    key,
                    "nao existe no executavel; o motor ignora chaves desconhecidas em silencio",
                    ValidationSeverity.Error));
            }
        }

        return new ValidationResult(issues);
    }
}
