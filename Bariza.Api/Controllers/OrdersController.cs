using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(
        [FromBody] ShippingAddressRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.CheckoutAsync(GetUserId(), request, idempotencyKey, cancellationToken));
    }

    [HttpGet("orders/my-orders")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders(CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetMyOrdersAsync(GetUserId(), cancellationToken));
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetOrderAsync(GetUserId(), id, cancellationToken));
    }

    [HttpPost("orders/{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, CancellationToken cancellationToken = default)
    {
        await _orders.CancelOrderAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var id))
        {
            throw new AuthenticationException("The current user could not be identified.", "invalid_user");
        }

        return id;
    }
}
