using happyInventory.DTOs.Countries;
using happyInventory.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace happyInventory.Controllers;

[ApiController]
[Route("api/countries")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countries;

    public CountriesController(ICountryService countries)
    {
        _countries = countries;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CountryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CountryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _countries.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CountryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _countries.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CountryDto>> Create(
        [FromBody] CountryRequest request,
        CancellationToken cancellationToken)
    {
        var country = await _countries.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = country.Id }, country);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CountryDto>> Update(
        int id,
        [FromBody] CountryRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _countries.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _countries.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
