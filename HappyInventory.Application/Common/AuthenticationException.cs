namespace HappyInventory.Application.Common;

/// <summary>
/// Raised when credentials are valid enough to identify an account but the
/// account cannot be authenticated, for example because it is disabled.
/// </summary>
public sealed class AuthenticationException : AppException
{
    public AuthenticationException(string message, string code)
        : base(message, code)
    {
    }
}
