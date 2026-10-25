namespace HappyInventory.Application.Common;

/// <summary>
/// Raised when a request is valid but conflicts with the current state,
/// for example when an inventory deduction exceeds the available quantity.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
