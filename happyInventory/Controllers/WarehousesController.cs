using happyInventory.Common;
using happyInventory.DTOs.WarehouseItems;
using happyInventory.DTOs.Warehouses;
using happyInventory.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace happyInventory.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouses;
    private readonly IWarehouseItemService _items;

    public WarehousesController(IWarehouseService warehouses, IWarehouseItemService items)
    {
        _warehouses = warehouses;
        _items = items;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Common.PagedResult<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Common.PagedResult<WarehouseDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _warehouses.GetPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _warehouses.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _warehouses.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet("{warehouseId:int}/items")]
    [ProducesResponseType(typeof(Common.PagedResult<WarehouseItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Common.PagedResult<WarehouseItemDto>>> GetItems(
        int warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _items.GetPagedAsync(warehouseId, page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WarehouseDto>> Create(
        [FromBody] WarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _warehouses.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseDto>> Update(
        int id,
        [FromBody] WarehouseRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _warehouses.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _warehouses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
