using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IOrderService _orders;

    public AddressesController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShippingAddressDto>>> Get(CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetAddressesAsync(GetUserId(), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ShippingAddressDto>> Create([FromBody] ShippingAddressRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.CreateAddressAsync(GetUserId(), request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShippingAddressDto>> Update(int id, [FromBody] ShippingAddressRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.UpdateAddressAsync(GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _orders.DeleteAddressAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
