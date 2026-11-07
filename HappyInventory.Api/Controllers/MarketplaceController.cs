using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Marketplace;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/marketplace")]
[AllowAnonymous]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceCatalogService _catalog;

    public MarketplaceController(IMarketplaceCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(PagedResponse<MarketplaceProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MarketplaceProductDto>>> GetProducts(
        [FromQuery] MarketplaceProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalog.GetProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("categories")]
    [ProducesResponseType(
        typeof(IReadOnlyList<MarketplaceCategoryDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MarketplaceCategoryDto>>> GetCategories(
        CancellationToken cancellationToken = default)
    {
        var result = await _catalog.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }
}
