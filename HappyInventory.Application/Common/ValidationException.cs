namespace HappyInventory.Application.Common;

/// <summary>
/// Raised when a request is well-formed but violates an application rule.
/// The API layer normally translates this to HTTP 400.
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message, string? code = null)
        : base(message, code)
    {
    }
}
