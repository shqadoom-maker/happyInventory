using HappyInventory.Application.DTOs.Dashboard;
using HappyInventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace HappyInventory.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _dashboard.GetAsync(cancellationToken));
    }
}
