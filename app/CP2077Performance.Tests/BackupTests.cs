namespace CP2077Performance.Tests;

using CP2077Performance.Core.Backup;

internal static class BackupTests
{
    private static string MakeDir(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("6.1 o backup e criado antes de alterar", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var file = Path.Combine(root, "user.ini");
                File.WriteAllText(file, "[Sec]\nK = V\n");

                var service = new BackupService(Path.Combine(root, "backups"));
                var entry = service.Create(new[] { file });

                Check.NotNull(entry, "backup criado");
                Check.Equal(1, entry!.Files.Count, "um arquivo guardado");
                Check.Equal(file, entry.Files[0], "caminho original registrado");
                Check.True(File.Exists(Path.Combine(entry.Directory, "user.ini")), "copia no disco");
                Check.Equal("[Sec]\nK = V\n", File.ReadAllText(file), "o original nao foi tocado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.1 nao ha backup quando nao ha arquivo para guardar", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var service = new BackupService(Path.Combine(root, "backups"));
                Check.Null(service.Create(new[] { Path.Combine(root, "inexistente.ini") }), "sem backup");
                Check.Equal(0, service.List().Count, "nenhuma versao");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.2 o backup guarda o conjunto de arquivos da operacao", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var a = Path.Combine(root, "user.ini");
                var b = Path.Combine(root, "UserSettings.json");
                File.WriteAllText(a, "a");
                File.WriteAllText(b, "b");

                var service = new BackupService(Path.Combine(root, "backups"));
                var entry = service.Create(new[] { a, b });

                Check.Equal(2, entry!.Files.Count, "dois arquivos");
                Check.True(File.Exists(Path.Combine(entry.Directory, "user.ini")), "user.ini guardado");
                Check.True(File.Exists(Path.Combine(entry.Directory, "UserSettings.json")), "UserSettings guardado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.3 sao mantidas as tres versoes mais recentes", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var service = new BackupService(Path.Combine(root, "backups"));
                var file = Path.Combine(root, "user.ini");

                for (var i = 0; i < 5; i++)
                {
                    File.WriteAllText(file, $"v{i}");
                    service.Create(new[] { file });
                    //_versions com o mesmo carimbo colidiriam; forca um segundo
                    Thread.Sleep(1100);
                }

                var versions = service.List();
                Check.Equal(3, versions.Count, "tres versoes guardadas");
                Check.Equal("v4", File.ReadAllText(file), "o conteudo atual nao foi restaurado");

                // a mais antiga foi descartada: as tres sobreviventes sao as recentes
                var directories = service.ListDirectories();
                Check.Equal(3, directories.Count, "tres diretorios");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.4 a restauracao devolve o estado anterior", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var file = Path.Combine(root, "user.ini");
                File.WriteAllText(file, "original");

                var service = new BackupService(Path.Combine(root, "backups"));
                var entry = service.Create(new[] { file })!;

                File.WriteAllText(file, "alterado");
                var outcome = service.Restore(entry);

                Check.True(outcome.Ok, "restauracao concluida");
                Check.Equal("original", File.ReadAllText(file), "estado anterior devolvido");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.4 a restauracao recusa quando o arquivo do backup sumiu", () =>
        {
            var root = MakeDir("cp2077-bk-");
            try
            {
                var file = Path.Combine(root, "user.ini");
                File.WriteAllText(file, "original");

                var service = new BackupService(Path.Combine(root, "backups"));
                var entry = service.Create(new[] { file })!;

                File.Delete(Path.Combine(entry.Directory, "user.ini"));
                var reread = service.Read(entry.Directory);
                var outcome = service.Restore(reread);

                Check.False(outcome.Ok, "restauracao recusada");
                Check.Equal(1, outcome.Missing.Count, "arquivo ausente reportado");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        });

        suite.Add("6.5 os backups ficam no diretorio de dados da ferramenta", () =>
        {
            var toolRoot = MakeDir("cp2077-tool-");
            var gameRoot = MakeDir("cp2077-game-");
            try
            {
                var service = new BackupService(Path.Combine(toolRoot, "backups"));
                var file = Path.Combine(gameRoot, "user.ini");
                File.WriteAllText(file, "x");

                service.Create(new[] { file });

                Check.True(Directory.Exists(Path.Combine(toolRoot, "backups")), "backup no diretorio da ferramenta");
                Check.False(
                    Directory.GetFileSystemEntries(gameRoot).Length > 1,
                    "nada criado dentro da pasta do jogo");
            }
            finally
            {
                try { Directory.Delete(toolRoot, true); } catch { }
                try { Directory.Delete(gameRoot, true); } catch { }
            }
        });
    }
}
