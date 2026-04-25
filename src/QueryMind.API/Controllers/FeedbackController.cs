using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryMind.API.Middleware;
using QueryMind.Application.Commands;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest body, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        var result = await mediator.Send(new SubmitFeedbackCommand(tenantId, body.MessageId, body.Rating, body.Comment), ct);
        return Ok(result);
    }
}

public record SubmitFeedbackRequest(Guid MessageId, int Rating, string? Comment);
