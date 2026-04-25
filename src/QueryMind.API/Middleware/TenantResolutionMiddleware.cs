using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.API.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, QueryMindDbContext db)
    {
        // Prefer JWT sub claim (Azure Entra), fallback to X-Api-Key
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tid") ?? context.User.FindFirst("tenant_id");
            if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var jwtTenantId))
            {
                context.Items["TenantId"] = jwtTenantId;
                await next(context);
                return;
            }
        }

        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.ToString()))).ToLower();
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.ApiKeyHash == hash && t.IsActive);

            if (tenant != null)
            {
                context.Items["TenantId"] = tenant.Id;
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        await next(context);
    }
}

public static class TenantContextExtensions
{
    public static Guid GetTenantId(this HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var v) && v is Guid id)
            return id;
        throw new UnauthorizedAccessException("Tenant context not resolved");
    }
}
