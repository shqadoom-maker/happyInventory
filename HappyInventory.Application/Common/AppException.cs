namespace HappyInventory.Application.Common;

/// <summary>
/// Base type for expected application failures. These exceptions describe the
/// domain outcome and intentionally do not contain HTTP status codes; the API
/// layer decides how to represent them in a response.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Optional stable error code that clients can use without depending on
    /// localized messages.
    /// </summary>
    public string? Code { get; }
}
