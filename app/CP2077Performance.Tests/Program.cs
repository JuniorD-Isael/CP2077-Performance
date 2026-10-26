namespace CP2077Performance.Tests;

internal static class Program
{
    private static int Main()
    {
        var suite = new TestSuite();

        InstallationTests.Register(suite);
        SchemaTests.Register(suite);
        BinaryTests.Register(suite);
        ProfileTests.Register(suite);
        ProfileFormatTests.Register(suite);
        WritingTests.Register(suite);
        BackupTests.Register(suite);
        ThemeTests.Register(suite);
        DisplayTests.Register(suite);
        DisplayApplyTests.Register(suite);

        Console.WriteLine("CP2077 Performance - verificacoes");
        Console.WriteLine();
        return suite.Run();
    }
}
