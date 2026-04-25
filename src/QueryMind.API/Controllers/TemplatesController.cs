using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryMind.Application.Queries;
using QueryMind.Domain.Enums;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplatesController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] QueryDialect? dialect, [FromQuery] string? category, CancellationToken ct)
    {
        var templates = await mediator.Send(new GetTemplatesQuery(dialect, category), ct);
        return Ok(templates);
    }
}
