using QueryMind.Domain.Enums;

namespace QueryMind.Application.DTOs;

public record SchemaDto(
    Guid Id,
    string Name,
    SchemaType Type,
    long FileSizeBytes,
    bool IsProcessed,
    string? Description,
    string? ProcessingError,
    DateTime CreatedAt
);

public record SchemaDetailDto(
    Guid Id,
    string Name,
    SchemaType Type,
    long FileSizeBytes,
    bool IsProcessed,
    string? Description,
    string? ProcessingError,
    string? ParsedJson,
    DateTime CreatedAt
);
