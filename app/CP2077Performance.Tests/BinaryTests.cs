namespace CP2077Performance.Tests;

using System.Text;
using CP2077Performance.Core.Binary;

internal static class BinaryTests
{
    /// <summary>Escreve um arquivo com strings separadas por byte nulo.</summary>
    private static string CreateStubBinary(params string[] tokens)
    {
        var path = Path.Combine(Path.GetTempPath(), "cp2077-bin-" + Guid.NewGuid().ToString("N")[..8] + ".bin");
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        foreach (var t in tokens)
        {
            var bytes = Encoding.ASCII.GetBytes(t);
            stream.Write(bytes);
            stream.WriteByte(0);
        }
        return path;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("3.3 os tokens sao extraidos do binario", () =>
        {
            var path = CreateStubBinary("Alpha", "BetaGamma", "Delta");
            try
            {
                var tokens = BinaryTokenScanner.ExtractTokens(path);
                Check.True(tokens.Contains("Alpha"), "token Alpha");
                Check.True(tokens.Contains("BetaGamma"), "token BetaGamma");
                Check.True(tokens.Contains("Delta"), "token Delta");
                Check.Equal(3, tokens.Count, "quantidade de tokens");
            }
            finally
            {
                File.Delete(path);
            }
        });

        suite.Add("3.3 chave presente e aceita", () =>
        {
            var path = CreateStubBinary("MultiFrustumUseGlobalOcclusion", "Occlusion");
            try
            {
                var tokens = BinaryTokenScanner.ExtractTokens(path);
                var userIni = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["Occlusion"] = new Dictionary<string, string> { ["MultiFrustumUseGlobalOcclusion"] = "False" },
                };

                var result = CvarKeyValidator.Validate(userIni, tokens);

                Check.False(result.HasErrors, "sem erros");
                Check.Equal(0, result.Issues.Count, "nenhuma issue");
            }
            finally
            {
                File.Delete(path);
            }
        });

        suite.Add("3.3 chave inventada e recusada", () =>
        {
            var path = CreateStubBinary("MultiFrustumUseGlobalOcclusion", "Vehicle");
            try
            {
                var tokens = BinaryTokenScanner.ExtractTokens(path);
                var userIni = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["Vehicle"] = new Dictionary<string, string> { ["EnableAirResistane"] = "False" },
                };

                var result = CvarKeyValidator.Validate(userIni, tokens);

                Check.True(result.HasErrors, "deve haver erro");
                var issue = result.Errors.Single();
                Check.Equal("EnableAirResistane", issue.Name, "chave reportada");
                Check.Contains(issue.Message, "ignora chaves desconhecidas", "explicacao do erro");
            }
            finally
            {
                File.Delete(path);
            }
        });

        suite.Add("3.4 chave conhecida como ausente produz aviso e segue", () =>
        {
            var path = CreateStubBinary("MultiFrustumUseGlobalOcclusion", "Rendering/FrostedGlass");
            try
            {
                var tokens = BinaryTokenScanner.ExtractTokens(path);
                var userIni = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["Rendering/FrostedGlass"] = new Dictionary<string, string> { ["GlassUseMipChain"] = "False" },
                };

                var result = CvarKeyValidator.Validate(userIni, tokens);

                Check.False(result.HasErrors, "nao pode ser erro");
                Check.True(result.HasWarnings, "deve haver aviso");
                var issue = result.Warnings.Single();
                Check.Equal("GlassUseMipChain", issue.Name, "chave reportada");
                Check.Contains(issue.Message, "FINDINGS.md", "referencia a documentacao");
            }
            finally
            {
                File.Delete(path);
            }
        });

        suite.Add("3.4 a excecao conhecida esta documentada", () =>
        {
            Check.True(
                CvarKeyValidator.KnownAbsentKeys.Contains("GlassUseMipChain"),
                "GlassUseMipChain deve estar na lista de excecoes");
            Check.Contains(CvarKeyValidator.KnownAbsentDocumentation, "FINDINGS.md", "referencia a documentacao");
        });
    }
}
