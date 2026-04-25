using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueryMind.API.Middleware;
using QueryMind.Application.Commands;
using QueryMind.Application.Queries;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.API.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController(
    ISender mediator,
    IQueryMindAgent agent,
    QueryMindDbContext db
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest body, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        var result = await mediator.Send(new CreateSessionCommand(tenantId, body.SchemaId, body.DefaultDialect, body.Title), ct);
        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task SendMessage(Guid sessionId, [FromBody] SendMessageRequest body, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();

        var tenant = await db.Tenants.FindAsync([tenantId], ct)
            ?? throw new InvalidOperationException("Tenant not found");

        if (!tenant.HasQueryCapacity())
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await Response.WriteAsJsonAsync(new
            {
                error = "Monthly query limit reached",
                limit = tenant.GetQueryLimit(),
                current = tenant.MonthlyQueryCount,
                plan = tenant.Plan.ToString(),
                resetAt = tenant.QueryCountResetAt
            }, ct);
            return;
        }

        var session = await db.Sessions
            .Include(s => s.Messages)
            .Include(s => s.Schema)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Session not found");

        var history = session.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationTurn(m.Role, m.Content))
            .ToList();

        string? schemaContext = null;
        if (session.Schema?.ParsedJson != null)
            schemaContext = $"[Schema: {session.Schema.Name}]\n";

        var agentRequest = new AgentRequest(
            sessionId,
            tenantId,
            body.Message,
            body.Dialect ?? session.DefaultDialect,
            history,
            schemaContext
        );

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var userMessage = new Domain.Entities.QueryMessage
        {
            SessionId = sessionId,
            Role = MessageRole.User,
            Content = body.Message
        };
        db.Messages.Add(userMessage);
        await db.SaveChangesAsync(ct);

        var assistantMessage = new Domain.Entities.QueryMessage
        {
            SessionId = sessionId,
            Role = MessageRole.Assistant
        };

        var sw = Stopwatch.StartNew();
        var fullContent = new StringBuilder();

        await foreach (var evt in agent.GenerateQueryAsync(agentRequest, ct))
        {
            switch (evt.Type)
            {
                case AgentStreamEventType.TextDelta:
                    fullContent.Append(evt.Delta);
                    var sseData = JsonSerializer.Serialize(new { type = "delta", text = evt.Delta });
                    await Response.WriteAsync($"data: {sseData}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                    break;

                case AgentStreamEventType.Complete:
                    assistantMessage.Content = evt.FullContent ?? fullContent.ToString();
                    assistantMessage.GeneratedQuery = evt.GeneratedQuery;
                    assistantMessage.Explanation = evt.Explanation;
                    assistantMessage.SchemaContextUsed = evt.SchemaContextUsed;
                    assistantMessage.InputTokens = evt.InputTokens;
                    assistantMessage.OutputTokens = evt.OutputTokens;
                    assistantMessage.LatencyMs = (int)sw.ElapsedMilliseconds;
                    assistantMessage.ModelVersion = "claude-sonnet-4-20250514";
                    assistantMessage.Dialect = body.Dialect ?? session.DefaultDialect;

                    db.Messages.Add(assistantMessage);

                    tenant.MonthlyQueryCount++;
                    tenant.UpdatedAt = DateTime.UtcNow;

                    session.UpdatedAt = DateTime.UtcNow;
                    if (session.Title == null)
                        session.Title = body.Message.Length > 60 ? body.Message[..57] + "..." : body.Message;

                    await db.SaveChangesAsync(ct);

                    var completeData = JsonSerializer.Serialize(new
                    {
                        type = "complete",
                        messageId = assistantMessage.Id,
                        generatedQuery = evt.GeneratedQuery,
                        explanation = evt.Explanation,
                        inputTokens = evt.InputTokens,
                        outputTokens = evt.OutputTokens,
                        latencyMs = assistantMessage.LatencyMs
                    });
                    await Response.WriteAsync($"data: {completeData}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                    break;

                case AgentStreamEventType.Error:
                    var errorData = JsonSerializer.Serialize(new { type = "error", message = evt.FullContent });
                    await Response.WriteAsync($"data: {errorData}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                    break;
            }
        }
    }

    [HttpGet("{sessionId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid sessionId, CancellationToken ct)
    {
        var tenantId = HttpContext.GetTenantId();
        var history = await mediator.Send(new GetSessionHistoryQuery(tenantId, sessionId), ct);
        return Ok(history);
    }
}

public record CreateSessionRequest(Guid? SchemaId, QueryDialect DefaultDialect, string? Title);
public record SendMessageRequest(string Message, QueryDialect? Dialect);
