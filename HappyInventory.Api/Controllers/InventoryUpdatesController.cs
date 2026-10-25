using HappyInventory.Application.DTOs.Integrations;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

/// <summary>
/// Inbound endpoint for the external eCommerce system shown in the system
/// diagram. The endpoint only exposes integration DTOs and delegates all
/// business logic to the Application layer.
/// </summary>
[ApiController]
[Route("api/inventory/updates")]
[Authorize]
public class InventoryUpdatesController : ControllerBase
{
    private readonly IInventoryUpdateService _updates;

    public InventoryUpdatesController(IInventoryUpdateService updates)
    {
        _updates = updates;
    }

    /// <summary>
    /// Deducts stock for one eCommerce order line. Every call must include a
    /// unique <c>Idempotency-Key</c> so retries cannot deduct stock twice.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryUpdateResponse>> Post(
        [FromBody] InventoryUpdateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException(
                "The Idempotency-Key header is required for inventory updates.");
        }

        var result = await _updates.ApplyAsync(request, idempotencyKey, cancellationToken);
        return Ok(result);
    }
}
