using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueryMind.Infrastructure.Persistence.Migrations;

public partial class InitialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Plan = table.Column<string>(nullable: false, defaultValue: "Starter"),
                ApiKeyHash = table.Column<string>(maxLength: 64, nullable: false),
                MonthlyQueryCount = table.Column<int>(nullable: false, defaultValue: 0),
                QueryCountResetAt = table.Column<DateTime>(nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                StripeCustomerId = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Tenants", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TenantUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TenantId = table.Column<Guid>(nullable: false),
                ExternalUserId = table.Column<string>(maxLength: 200, nullable: false),
                Email = table.Column<string>(maxLength: 300, nullable: false),
                DisplayName = table.Column<string>(maxLength: 200, nullable: false),
                Role = table.Column<string>(nullable: false, defaultValue: "Viewer"),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantUsers", x => x.Id);
                table.ForeignKey("FK_TenantUsers_Tenants", x => x.TenantId, "Tenants", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Schemas",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TenantId = table.Column<Guid>(nullable: false),
                Name = table.Column<string>(maxLength: 300, nullable: false),
                Type = table.Column<string>(nullable: false),
                FileUrl = table.Column<string>(nullable: true),
                ParsedJson = table.Column<string>(type: "jsonb", nullable: true),
                EmbeddingId = table.Column<string>(nullable: true),
                FileSizeBytes = table.Column<long>(nullable: false),
                Description = table.Column<string>(nullable: true),
                IsProcessed = table.Column<bool>(nullable: false, defaultValue: false),
                ProcessingError = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false),
                DeletedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Schemas", x => x.Id);
                table.ForeignKey("FK_Schemas_Tenants", x => x.TenantId, "Tenants", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TenantId = table.Column<Guid>(nullable: false),
                SchemaId = table.Column<Guid>(nullable: true),
                DefaultDialect = table.Column<string>(nullable: false, defaultValue: "Dax"),
                Title = table.Column<string>(maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sessions", x => x.Id);
                table.ForeignKey("FK_Sessions_Tenants", x => x.TenantId, "Tenants", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Sessions_Schemas", x => x.SchemaId, "Schemas", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Messages",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                SessionId = table.Column<Guid>(nullable: false),
                Role = table.Column<string>(nullable: false),
                Content = table.Column<string>(nullable: false),
                GeneratedQuery = table.Column<string>(nullable: true),
                Dialect = table.Column<string>(nullable: true),
                Explanation = table.Column<string>(nullable: true),
                SchemaContextUsed = table.Column<string>(nullable: true),
                InputTokens = table.Column<int>(nullable: false),
                OutputTokens = table.Column<int>(nullable: false),
                LatencyMs = table.Column<int>(nullable: false),
                ModelVersion = table.Column<string>(maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Messages", x => x.Id);
                table.ForeignKey("FK_Messages_Sessions", x => x.SessionId, "Sessions", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Feedback",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                MessageId = table.Column<Guid>(nullable: false),
                Rating = table.Column<int>(nullable: false),
                Comment = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Feedback", x => x.Id);
                table.ForeignKey("FK_Feedback_Messages", x => x.MessageId, "Messages", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Templates",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Title = table.Column<string>(maxLength: 300, nullable: false),
                Description = table.Column<string>(nullable: false),
                Category = table.Column<string>(maxLength: 100, nullable: false),
                Dialect = table.Column<string>(nullable: false),
                QueryText = table.Column<string>(nullable: false),
                NaturalLanguagePrompt = table.Column<string>(nullable: false),
                Tags = table.Column<string>(nullable: false),
                UsageCount = table.Column<int>(nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Templates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TenantId = table.Column<Guid>(nullable: false),
                UserId = table.Column<string>(nullable: true),
                Action = table.Column<string>(maxLength: 200, nullable: false),
                EntityType = table.Column<string>(maxLength: 100, nullable: false),
                EntityId = table.Column<string>(nullable: true),
                Details = table.Column<string>(nullable: true),
                IpAddress = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

        migrationBuilder.CreateIndex("IX_Tenants_ApiKeyHash", "Tenants", "ApiKeyHash", unique: true);
        migrationBuilder.CreateIndex("IX_TenantUsers_TenantId", "TenantUsers", "TenantId");
        migrationBuilder.CreateIndex("IX_Schemas_TenantId", "Schemas", "TenantId");
        migrationBuilder.CreateIndex("IX_Sessions_TenantId", "Sessions", "TenantId");
        migrationBuilder.CreateIndex("IX_Messages_SessionId", "Messages", "SessionId");
        migrationBuilder.CreateIndex("IX_AuditLogs_TenantId", "AuditLogs", "TenantId");
        migrationBuilder.CreateIndex("IX_AuditLogs_CreatedAt", "AuditLogs", "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AuditLogs");
        migrationBuilder.DropTable("Feedback");
        migrationBuilder.DropTable("Templates");
        migrationBuilder.DropTable("Messages");
        migrationBuilder.DropTable("Sessions");
        migrationBuilder.DropTable("Schemas");
        migrationBuilder.DropTable("TenantUsers");
        migrationBuilder.DropTable("Tenants");
    }
}
