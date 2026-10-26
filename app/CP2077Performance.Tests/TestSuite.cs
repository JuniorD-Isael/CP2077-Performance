namespace CP2077Performance.Tests;

/// <summary>
/// Runner de teste minimalista, sem dependencia de pacote externo, para que o
/// projeto construa e teste offline. Sai com codigo 1 se alguma verificacao
/// falhar.
/// </summary>
internal sealed class TestSuite
{
    private readonly List<(string Name, Action Body)> _tests = new();

    public void Add(string name, Action body) => _tests.Add((name, body));

    public int Run()
    {
        int passed = 0;
        var failures = new List<string>();

        foreach (var (name, body) in _tests)
        {
            try
            {
                body();
                passed++;
                Console.WriteLine($"  ok    {name}");
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"  FALHA {name}");
                Console.WriteLine($"        {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{passed} de {_tests.Count} verificacoes passaram.");

        if (failures.Count == 0)
        {
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("Falhas:");
        foreach (var f in failures)
        {
            Console.WriteLine($"  - {f}");
        }
        return 1;
    }
}
