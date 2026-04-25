using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryMind.Application.DTOs;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Queries;

public record GetSessionHistoryQuery(Guid TenantId, Guid SessionId) : IRequest<IReadOnlyList<MessageDto>>;

public class GetSessionHistoryQueryHandler(QueryMindDbContext db) : IRequestHandler<GetSessionHistoryQuery, IReadOnlyList<MessageDto>>
{
    public async Task<IReadOnlyList<MessageDto>> Handle(GetSessionHistoryQuery request, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([request.SessionId], ct)
            ?? throw new InvalidOperationException("Session not found");

        if (session.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied");

        return await db.Messages
            .Where(m => m.SessionId == request.SessionId)
            .Include(m => m.Feedback)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id, m.Role, m.Content, m.GeneratedQuery, m.Dialect, m.Explanation, m.SchemaContextUsed,
                m.InputTokens, m.OutputTokens, m.LatencyMs,
                m.Feedback == null ? null : new FeedbackDto(m.Feedback.Id, m.Feedback.Rating, m.Feedback.Comment),
                m.CreatedAt))
            .ToListAsync(ct);
    }
}
