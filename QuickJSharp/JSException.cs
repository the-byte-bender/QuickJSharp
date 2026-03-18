namespace QuickJSharp;

/// <summary>
/// Exception for QuickJS-related errors.
/// </summary>
/// <remarks>
/// This exception is thrown when a QuickJS operation fails, like when an exception is thrown in JavaScript code or when a QuickJS API call returns an error. When appropriate, other more standard exceptions may be thrown instead, such as <see cref="ArgumentException"/> or <see cref="InvalidOperationException"/>.
/// </remarks>
public sealed class JSException : Exception
{
    internal JSException(string message)
        : base(message) { }

    internal JSException(string message, Exception innerException)
        : base(message, innerException) { }
}
