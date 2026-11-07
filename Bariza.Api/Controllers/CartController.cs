using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart)
    {
        _cart = cart;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken = default)
    {
        return Ok(await _cart.GetCartAsync(GetUserId(), cancellationToken));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _cart.AddItemAsync(GetUserId(), request, cancellationToken));
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(
        int id,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _cart.UpdateItemAsync(GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveItem(
        int id,
        CancellationToken cancellationToken = default)
    {
        await _cart.RemoveItemAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken = default)
    {
        await _cart.ClearCartAsync(GetUserId(), cancellationToken);
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
