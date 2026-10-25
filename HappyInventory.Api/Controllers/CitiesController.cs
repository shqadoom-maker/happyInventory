using HappyInventory.Application.DTOs.Cities;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/cities")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cities;

    public CitiesController(ICityService cities)
    {
        _cities = cities;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> GetAll(
        [FromQuery] int? countryId,
        CancellationToken cancellationToken)
    {
        return Ok(await _cities.GetAllAsync(countryId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CityDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _cities.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CityDto>> Create(
        [FromBody] CityRequest request,
        CancellationToken cancellationToken)
    {
        var city = await _cities.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = city.Id }, city);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CityDto>> Update(
        int id,
        [FromBody] CityRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _cities.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _cities.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
