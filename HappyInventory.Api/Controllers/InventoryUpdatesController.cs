using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Integrations;
using HappyInventory.Application.Interfaces.Repositories;
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
    private readonly IInventoryUpdateRepository _records;
    private readonly ILogger<InventoryUpdatesController> _logger;

    public InventoryUpdatesController(
        IInventoryUpdateService updates,
        IInventoryUpdateRepository records,
        ILogger<InventoryUpdatesController> logger)
    {
        _updates = updates;
        _records = records;
        _logger = logger;
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
            _logger.LogWarning(
                "Inventory update rejected for warehouse {WarehouseId}, SKU {SKUCode}: " +
                "the Idempotency-Key header is missing.",
                request.WarehouseId,
                request.SKUCode);

            throw new ValidationException(
                "The Idempotency-Key header is required for inventory updates.");
        }

        var result = await _updates.ApplyAsync(request, idempotencyKey, cancellationToken);

        if (result.Replayed)
        {
            _logger.LogInformation(
                "Inventory update replayed for warehouse {WarehouseId}, SKU {SKUCode}: " +
                "{PreviousQuantity} -> {RemainingQuantity} (key {IdempotencyKey}).",
                result.WarehouseId,
                result.SKUCode,
                result.PreviousQuantity,
                result.RemainingQuantity,
                idempotencyKey);
        }
        else
        {
            _logger.LogInformation(
                "Inventory update applied for warehouse {WarehouseId}, SKU {SKUCode}: " +
                "{PreviousQuantity} -> {RemainingQuantity} (key {IdempotencyKey}).",
                result.WarehouseId,
                result.SKUCode,
                result.PreviousQuantity,
                result.RemainingQuantity,
                idempotencyKey);
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns completed eCommerce inventory deduction records for monitoring
    /// integration activity.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InventoryUpdateRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<InventoryUpdateRecordDto>>> GetPaged(
        [FromQuery] int? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _records.GetPagedAsync(
            warehouseId,
            page,
            pageSize,
            search,
            cancellationToken);

        return Ok(new PagedResult<InventoryUpdateRecordDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
        });
    }
}
