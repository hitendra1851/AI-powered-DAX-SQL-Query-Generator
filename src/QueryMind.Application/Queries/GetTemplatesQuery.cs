using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryMind.Application.DTOs;
using QueryMind.Domain.Enums;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Queries;

public record GetTemplatesQuery(QueryDialect? Dialect = null, string? Category = null) : IRequest<IReadOnlyList<TemplateDto>>;

public class GetTemplatesQueryHandler(QueryMindDbContext db) : IRequestHandler<GetTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    public async Task<IReadOnlyList<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken ct)
    {
        var query = db.Templates.AsQueryable();

        if (request.Dialect.HasValue)
            query = query.Where(t => t.Dialect == request.Dialect.Value);

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(t => t.Category == request.Category);

        return await query
            .OrderByDescending(t => t.UsageCount)
            .Select(t => new TemplateDto(
                t.Id, t.Title, t.Description, t.Category, t.Dialect,
                t.QueryText, t.NaturalLanguagePrompt,
                t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries),
                t.UsageCount))
            .ToListAsync(ct);
    }
}
