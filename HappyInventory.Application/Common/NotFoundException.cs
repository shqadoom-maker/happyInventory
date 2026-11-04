namespace HappyInventory.Application.Common;

/// <summary>Raised when a requested application resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
