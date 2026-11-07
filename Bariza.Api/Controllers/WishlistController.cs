using System.Security.Claims;
using Bariza.Application.DTOs.Marketplace;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMarketplaceCatalogService _catalog;

    public WishlistController(IMarketplaceCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WishlistItemDto>>> Get(CancellationToken cancellationToken = default)
    {
        return Ok(await _catalog.GetWishlistAsync(GetUserId(), cancellationToken));
    }

    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId, CancellationToken cancellationToken = default)
    {
        await _catalog.AddToWishlistAsync(GetUserId(), productId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId, CancellationToken cancellationToken = default)
    {
        await _catalog.RemoveFromWishlistAsync(GetUserId(), productId, cancellationToken);
        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
