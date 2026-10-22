namespace CP2077Performance.Tests;

using System.Text.Json.Nodes;

internal static class ProfileFormatTests
{
    /// <summary>
    /// Acha a raiz do repositorio subindo a partir do executavel de teste.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "profile", "profile.schema.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("a raiz do repositorio nao foi encontrada a partir de " + AppContext.BaseDirectory);
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("8.7 o perfil curado valida contra o schema do formato", () =>
        {
            var root = FindRepositoryRoot();
            var schemaPath = Path.Combine(root, "profile", "profile.schema.json");
            var profilePath = Path.Combine(root, "profile", "lowspec-30fps.json");

            var schema = JsonNode.Parse(File.ReadAllText(schemaPath));
            var profile = JsonNode.Parse(File.ReadAllText(profilePath));

            var errors = SchemaFormatValidator.Validate(profile, schema);
            Check.Equal(0, errors.Count, "erros: " + string.Join("; ", errors));
        });

        suite.Add("8.8 o perfil curado declara as duas camadas na variante", () =>
        {
            var root = FindRepositoryRoot();
            var profilePath = Path.Combine(root, "profile", "lowspec-30fps.json");
            var profile = JsonNode.Parse(File.ReadAllText(profilePath));

            var variants = profile?["variants"] as JsonObject;
            Check.NotNull(variants, "o perfil curado declara o bloco de variantes");
            Check.False(variants!.ContainsKey("hudMinimal"), "o perfil curado nao declara hudMinimal");

            var settings = profile?["userSettings"]?["values"] as JsonObject;
            Check.NotNull(settings, "o perfil curado declara opcoes de menu");
            Check.False(settings!.ContainsKey("minimap"), "o perfil curado nao altera minimap");
            Check.False(settings.ContainsKey("quest_tracker"), "o perfil curado nao altera quest_tracker");
            Check.False(settings.ContainsKey("npc_nameplates"), "o perfil curado nao altera npc_nameplates");

            foreach (var (id, node) in variants)
            {
                var variant = node as JsonObject;
                Check.NotNull(variant, $"a variante '{id}' e um objeto");
                Check.NotNull(variant!["label"], $"a variante '{id}' declara rotulo");
                Check.NotNull(variant["summary"], $"a variante '{id}' declara resumo em linguagem corrente");
                Check.NotNull(variant["highlights"], $"a variante '{id}' declara destaques");
                Check.NotNull(variant["userSettings"], $"a variante '{id}' alcanca a camada de menu");
            }
        });

        suite.Add("8.9 o perfil curado segue o padrao de versionamento", () =>
        {
            var root = FindRepositoryRoot();
            var profilePath = Path.Combine(root, "profile", "lowspec-30fps.json");
            var profile = JsonNode.Parse(File.ReadAllText(profilePath));

            var version = profile?["version"]?.GetValue<string>() ?? "";
            Check.True(
                System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"),
                $"a versao '{version}' segue SemVer");
        });
    }
}
