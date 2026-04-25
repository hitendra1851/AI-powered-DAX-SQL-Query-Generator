using QueryMind.Domain.Enums;

namespace QueryMind.Application.DTOs;

public record SessionDto(
    Guid Id,
    Guid? SchemaId,
    string? SchemaName,
    QueryDialect DefaultDialect,
    string? Title,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
