using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.WarehouseItems;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/warehouse-items")]
public class WarehouseItemsController : ControllerBase
{
    private readonly IWarehouseItemService _items;

    public WarehouseItemsController(IWarehouseItemService items)
    {
        _items = items;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WarehouseItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WarehouseItemDto>>> GetPaged(
        [FromQuery] int warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _items.GetPagedAsync(warehouseId, page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WarehouseItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseItemDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _items.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WarehouseItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseItemDto>> Create(
        [FromBody] WarehouseItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await _items.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(WarehouseItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseItemDto>> Update(
        int id,
        [FromBody] WarehouseItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _items.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _items.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
