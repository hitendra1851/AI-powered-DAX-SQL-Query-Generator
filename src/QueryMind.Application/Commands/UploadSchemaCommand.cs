using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryMind.Application.DTOs;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Persistence;
using QueryMind.Infrastructure.Services;
using System.Text.Json;

namespace QueryMind.Application.Commands;

public record UploadSchemaCommand(
    Guid TenantId,
    string FileName,
    Stream FileContent,
    string ContentType,
    long FileSizeBytes,
    string? Description,
    SchemaType? ExplicitType = null
) : IRequest<SchemaDto>;

public class UploadSchemaCommandHandler(
    QueryMindDbContext db,
    IStorageService storage,
    ISchemaSearchService search,
    SchemaParserFactory parserFactory,
    IAuditService audit
) : IRequestHandler<UploadSchemaCommand, SchemaDto>
{
    public async Task<SchemaDto> Handle(UploadSchemaCommand request, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([request.TenantId], ct)
            ?? throw new InvalidOperationException("Tenant not found");

        var schemaCount = await db.Schemas.CountAsync(s => s.TenantId == request.TenantId, ct);
        if (schemaCount >= tenant.GetSchemaLimit())
            throw new InvalidOperationException($"Schema limit of {tenant.GetSchemaLimit()} reached for {tenant.Plan} plan");

        var schemaType = request.ExplicitType ?? parserFactory.DetectType(request.FileName, request.ContentType);

        var fileUrl = await storage.UploadAsync(request.TenantId, request.FileName, request.FileContent, request.ContentType, ct);

        var schema = new Domain.Entities.Schema
        {
            TenantId = request.TenantId,
            Name = request.Description ?? Path.GetFileNameWithoutExtension(request.FileName),
            Type = schemaType,
            FileUrl = fileUrl,
            FileSizeBytes = request.FileSizeBytes,
            Description = request.Description
        };

        db.Schemas.Add(schema);
        await db.SaveChangesAsync(ct);

        _ = ProcessSchemaAsync(schema.Id, schemaType, fileUrl, ct);

        await audit.LogAsync(request.TenantId, null, "SchemaUploaded", "Schema", schema.Id.ToString(), request.FileName, null, ct);

        return ToDto(schema);
    }

    private async Task ProcessSchemaAsync(Guid schemaId, SchemaType schemaType, string fileUrl, CancellationToken ct)
    {
        try
        {
            var schema = await db.Schemas.FindAsync([schemaId], ct);
            if (schema == null) return;

            var fileStream = await new BlobStorageService(null!).DownloadAsync(fileUrl, ct);
            var parser = parserFactory.GetParser(schemaType);
            var context = await parser.ParseAsync(fileStream, schema.Name, ct);

            schema.ParsedJson = JsonSerializer.Serialize(context);
            schema.IsProcessed = true;
            schema.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            var tenantId = schema.TenantId;
            await search.IndexSchemaAsync(schemaId, tenantId, context, ct);
        }
        catch (Exception ex)
        {
            var schema = await db.Schemas.FindAsync([schemaId], ct);
            if (schema != null)
            {
                schema.ProcessingError = ex.Message;
                schema.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static SchemaDto ToDto(Domain.Entities.Schema s) => new(
        s.Id, s.Name, s.Type, s.FileSizeBytes, s.IsProcessed, s.Description, s.ProcessingError, s.CreatedAt);
}
