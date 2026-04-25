using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Interfaces;

public interface ISchemaParser
{
    SchemaType SupportedType { get; }
    Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

public record SchemaContext(
    string Name,
    SchemaType Type,
    IReadOnlyList<TableDefinition> Tables,
    IReadOnlyList<MeasureDefinition> Measures,
    IReadOnlyList<RelationshipDefinition> Relationships
);

public record TableDefinition(
    string Name,
    string? Description,
    IReadOnlyList<ColumnDefinition> Columns
);

public record ColumnDefinition(
    string Name,
    string DataType,
    bool IsKey,
    bool IsForeignKey,
    string? Description
);

public record MeasureDefinition(
    string Name,
    string? Expression,
    string? FormatString,
    string? Description
);

public record RelationshipDefinition(
    string FromTable,
    string FromColumn,
    string ToTable,
    string ToColumn,
    string Cardinality
);
