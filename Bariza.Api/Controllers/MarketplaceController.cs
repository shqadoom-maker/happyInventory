using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Marketplace;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

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

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProductById(int id, CancellationToken cancellationToken = default)
    {
        var result = await _catalog.GetProductDetailsAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("products/slug/{slug}")]
    public async Task<ActionResult<ProductDetailsDto>> GetProductBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var result = await _catalog.GetProductDetailsBySlugAsync(slug, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("products/{id:int}/related")]
    public async Task<ActionResult<IReadOnlyList<MarketplaceProductDto>>> GetRelatedProducts(int id, CancellationToken cancellationToken = default)
    {
        return Ok(await _catalog.GetRelatedProductsAsync(id, cancellationToken));
    }

    [HttpGet("products/{id:int}/reviews")]
    public async Task<ActionResult<IReadOnlyList<ProductReviewDto>>> GetReviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return Ok(await _catalog.GetReviewsAsync(id, page, pageSize, cancellationToken));
    }

    [HttpGet("products/{id:int}/reviews/summary")]
    public async Task<ActionResult<ReviewSummaryDto>> GetReviewSummary(int id, CancellationToken cancellationToken = default)
    {
        return Ok(await _catalog.GetReviewSummaryAsync(id, cancellationToken));
    }

    [HttpPost("products/{id:int}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int id, [FromBody] AddReviewRequest request, CancellationToken cancellationToken = default)
    {
        await _catalog.AddReviewAsync(GetUserId(), id, request.Rating, request.Comment, cancellationToken);
        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<MarketplaceCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MarketplaceCategoryDto>>> GetCategories(
        CancellationToken cancellationToken = default)
    {
        var result = await _catalog.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}

public class AddReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
