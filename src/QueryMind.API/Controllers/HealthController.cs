using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(QueryMindDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow, version = "1.0.0" });
        }
        catch
        {
            return StatusCode(503, new { status = "degraded", timestamp = DateTime.UtcNow });
        }
    }
}
