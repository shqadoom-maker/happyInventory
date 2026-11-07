namespace Bariza.Application.Common;

/// <summary>
/// Raised when a request is valid but conflicts with the current state,
/// for example when an inventory deduction exceeds the available quantity.
/// The API layer normally translates this to HTTP 409.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message, string? code = null)
        : base(message, code)
    {
    }
}
