namespace CP2077Performance.Tests;

/// <summary>Afirmacoes de uso do runner.</summary>
internal static class Check
{
    public static void True(bool condition, string what)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"esperava verdadeiro: {what}");
        }
    }

    public static void False(bool condition, string what)
    {
        if (condition)
        {
            throw new InvalidOperationException($"esperava falso: {what}");
        }
    }

    public static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{what}: esperava '{expected}', obtive '{actual}'");
        }
    }

    public static void NotEqual<T>(T unexpected, T actual, string what)
    {
        if (EqualityComparer<T>.Default.Equals(unexpected, actual))
        {
            throw new InvalidOperationException($"{what}: nao devia ser '{unexpected}'");
        }
    }

    public static void NotNull(object? value, string what)
    {
        if (value is null)
        {
            throw new InvalidOperationException($"esperava nao nulo: {what}");
        }
    }

    public static void Null(object? value, string what)
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"esperava nulo: {what}");
        }
    }

    public static void Contains(string haystack, string needle, string what)
    {
        if (haystack is null || !haystack.Contains(needle, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{what}: '{needle}' nao encontrado em '{haystack?.Replace("\n", "\\n")}'");
        }
    }

    public static void DoesNotContain(string haystack, string needle, string what)
    {
        if (haystack is not null && haystack.Contains(needle, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{what}: '{needle}' presente sem deber");
        }
    }

    public static void Throws<TException>(Action body, string what) where TException : Exception
    {
        try
        {
            body();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"{what}: esperava {typeof(TException).Name}");
    }
}
