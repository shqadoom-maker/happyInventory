using System.Security.Claims;
using Bariza.Application.Common;
using Bariza.Application.DTOs.SellerProducts;
using Bariza.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bariza.Api.Controllers;

[ApiController]
[Route("api/seller/products")]
[Authorize]
public class SellerProductsController : ControllerBase
{
    private readonly ISellerProductService _service;

    public SellerProductsController(ISellerProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SellerProductDto>>> Get(CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetProductsAsync(GetUserId(), cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<SellerProductDto>>> GetCategories(CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetCategoriesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SellerProductDto>> Create([FromBody] CreateSellerProductRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.CreateProductAsync(GetUserId(), request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SellerProductDto>> Update(int id, [FromBody] UpdateSellerProductRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.UpdateProductAsync(GetUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        await _service.DeleteProductAsync(GetUserId(), id, cancellationToken);
        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
