using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin,Management")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public AdminOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetAdminOrdersAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetAdminOrderAsync(id, cancellationToken));
    }

    [HttpPut("seller-orders/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        await _orders.UpdateAdminSellerOrderStatusAsync(id, request.Status, cancellationToken);
        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
