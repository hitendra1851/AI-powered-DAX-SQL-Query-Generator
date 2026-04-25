using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryMind.API.Middleware;
using QueryMind.Application.Commands;
using QueryMind.Application.Queries;
using QueryMind.Domain.Enums;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/schemas")]
[Authorize]
public class SchemasController(ISender mediator) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? description, [FromForm] SchemaType? schemaType, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new UploadSchemaCommand(
            tenantId,
            file.FileName,
            stream,
            file.ContentType,
            file.Length,
            description,
            schemaType
        ), ct);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        var schemas = await mediator.Send(new GetSchemasQuery(tenantId), ct);
        return Ok(schemas);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        await mediator.Send(new DeleteSchemaCommand(tenantId, id), ct);
        return NoContent();
    }
}
