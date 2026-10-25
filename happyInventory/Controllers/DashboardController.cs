using happyInventory.DTOs.Dashboard;
using happyInventory.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace happyInventory.Controllers;

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
