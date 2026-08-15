using System.Collections;

namespace Test.Shared;

/// <summary>
/// Thrown when a <see cref="Check"/> assertion fails. Touchstone treats any
/// exception escaping a test case delegate as a failure, so this is all the
/// assertion machinery the shared suites need.
/// </summary>
public sealed class CheckException : Exception
{
    public CheckException(string message) : base(message) { }
}

/// <summary>
/// A tiny, framework-agnostic assertion helper used by the shared Touchstone
/// suites. Named <c>Check</c> (rather than <c>Assert</c>) so it never collides
/// with the assertion types of the xUnit/NUnit host adapters.
/// </summary>
public static class Check
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
            throw new CheckException(message ?? "Expected condition to be true, but it was false.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
            throw new CheckException(message ?? "Expected condition to be false, but it was true.");
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new CheckException(message ??
                $"Expected: <{Describe(expected)}>{Environment.NewLine}Actual:   <{Describe(actual)}>");
    }

    public static void NotEqual<T>(T notExpected, T actual, string? message = null)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
            throw new CheckException(message ??
                $"Expected a value different from <{Describe(notExpected)}>, but they were equal.");
    }

    public static void Null(object? value, string? message = null)
    {
        if (value != null)
            throw new CheckException(message ?? $"Expected null, but was <{Describe(value)}>.");
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value == null)
            throw new CheckException(message ?? "Expected a non-null value, but was null.");
    }

    public static void Empty(IEnumerable collection, string? message = null)
    {
        foreach (var _ in collection)
            throw new CheckException(message ?? "Expected an empty collection, but it had elements.");
    }

    public static void Count(int expected, IEnumerable collection, string? message = null)
    {
        var actual = 0;
        foreach (var _ in collection) actual++;
        if (actual != expected)
            throw new CheckException(message ?? $"Expected collection count <{expected}>, but was <{actual}>.");
    }

    /// <summary>Asserts that <paramref name="action"/> throws an exception of type <typeparamref name="T"/>.</summary>
    public static T Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new CheckException($"Expected exception of type <{typeof(T).Name}>, but got <{ex.GetType().Name}>: {ex.Message}");
        }

        throw new CheckException($"Expected exception of type <{typeof(T).Name}>, but no exception was thrown.");
    }

    /// <summary>Asserts that <paramref name="action"/> throws an exception of type <typeparamref name="T"/>.</summary>
    public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new CheckException($"Expected exception of type <{typeof(T).Name}>, but got <{ex.GetType().Name}>: {ex.Message}");
        }

        throw new CheckException($"Expected exception of type <{typeof(T).Name}>, but no exception was thrown.");
    }

    private static string Describe(object? value) => value?.ToString() ?? "null";
}
