using Microsoft.EntityFrameworkCore;
using QueryMind.Domain.Entities;
using QueryMind.Infrastructure.Persistence.Configurations;

namespace QueryMind.Infrastructure.Persistence;

public class QueryMindDbContext(DbContextOptions<QueryMindDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Schema> Schemas => Set<Schema>();
    public DbSet<QuerySession> Sessions => Set<QuerySession>();
    public DbSet<QueryMessage> Messages => Set<QueryMessage>();
    public DbSet<QueryFeedback> Feedback => Set<QueryFeedback>();
    public DbSet<QueryTemplate> Templates => Set<QueryTemplate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QueryMindDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
