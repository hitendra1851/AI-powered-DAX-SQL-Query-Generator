using QueryMind.Domain.Enums;

namespace QueryMind.Application.DTOs;

public record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    string? GeneratedQuery,
    QueryDialect? Dialect,
    string? Explanation,
    string? SchemaContextUsed,
    int InputTokens,
    int OutputTokens,
    int LatencyMs,
    FeedbackDto? Feedback,
    DateTime CreatedAt
);

public record FeedbackDto(
    Guid Id,
    int Rating,
    string? Comment
);
