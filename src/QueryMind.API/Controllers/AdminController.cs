using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryMind.API.Middleware;
using QueryMind.Application.Queries;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(ISender mediator) : ControllerBase
{
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage(CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        var usage = await mediator.Send(new GetTenantUsageQuery(tenantId), ct);
        return Ok(usage);
    }
}
