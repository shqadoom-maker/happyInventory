using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/seller/orders")]
[Authorize]
public class SellerOrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public SellerOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SellerOrderDto>>> GetOrders(CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetSellerOrdersAsync(GetUserId(), cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SellerOrderDto>> GetOrder(int id, CancellationToken cancellationToken = default)
    {
        return Ok(await _orders.GetSellerOrderAsync(GetUserId(), id, cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await _orders.UpdateSellerOrderStatusAsync(GetUserId(), id, request.Status, cancellationToken);
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
