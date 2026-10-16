using System.Diagnostics.CodeAnalysis;

namespace Curator.Application.Common;

/// <summary>Kinds of expected failures. The Api maps each to an HTTP status.</summary>
public enum ErrorKind
{
    NotFound,
    Forbidden,
    Conflict,
}

/// <summary>An expected failure of a use case, such as "not found". Bugs are still exceptions.</summary>
public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error NotFound(string message) => new(ErrorKind.NotFound, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
}

/// <summary>
/// The outcome of a use case that can fail: a value or an <see cref="Common.Error"/>. Handlers return either one
/// directly, thanks to the implicit conversions.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value) => _value = value;

    private Result(Error error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public T Value => IsFailure
        ? throw new InvalidOperationException($"The result is a failure: {Error.Message}")
        : _value!;

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
