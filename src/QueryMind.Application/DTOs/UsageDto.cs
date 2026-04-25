using QueryMind.Domain.Enums;

namespace QueryMind.Application.DTOs;

public record TenantUsageDto(
    Guid TenantId,
    string TenantName,
    PlanType Plan,
    int MonthlyQueryCount,
    int QueryLimit,
    int TotalSessions,
    int TotalSchemas,
    long TotalInputTokens,
    long TotalOutputTokens,
    DateTime QueryCountResetAt
);

public record TemplateDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    QueryDialect Dialect,
    string QueryText,
    string NaturalLanguagePrompt,
    string[] Tags,
    int UsageCount
);
