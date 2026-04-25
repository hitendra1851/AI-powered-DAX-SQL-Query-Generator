using MediatR;
using QueryMind.Application.DTOs;
using QueryMind.Domain.Enums;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Commands;

public record CreateSessionCommand(
    Guid TenantId,
    Guid? SchemaId,
    QueryDialect DefaultDialect,
    string? Title
) : IRequest<SessionDto>;

public class CreateSessionCommandHandler(QueryMindDbContext db) : IRequestHandler<CreateSessionCommand, SessionDto>
{
    public async Task<SessionDto> Handle(CreateSessionCommand request, CancellationToken ct)
    {
        if (request.SchemaId.HasValue)
        {
            var exists = await db.Schemas.FindAsync([request.SchemaId.Value], ct);
            if (exists == null || exists.TenantId != request.TenantId)
                throw new InvalidOperationException("Schema not found");
        }

        var session = new Domain.Entities.QuerySession
        {
            TenantId = request.TenantId,
            SchemaId = request.SchemaId,
            DefaultDialect = request.DefaultDialect,
            Title = request.Title
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        return new SessionDto(session.Id, session.SchemaId, null, session.DefaultDialect, session.Title, 0, session.CreatedAt, session.UpdatedAt);
    }
}
