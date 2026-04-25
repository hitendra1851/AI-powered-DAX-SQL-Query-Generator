using FluentAssertions;
using QueryMind.Domain.Entities;
using QueryMind.Domain.Enums;
using Xunit;

namespace QueryMind.UnitTests.Domain;

public class TenantTests
{
    [Theory]
    [InlineData(PlanType.Starter, 500)]
    [InlineData(PlanType.Pro, 5000)]
    [InlineData(PlanType.Enterprise, int.MaxValue)]
    public void GetQueryLimit_ReturnsPlanSpecificLimit(PlanType plan, int expected)
    {
        var tenant = new Tenant { Plan = plan };
        tenant.GetQueryLimit().Should().Be(expected);
    }

    [Theory]
    [InlineData(PlanType.Starter, 3)]
    [InlineData(PlanType.Pro, int.MaxValue)]
    [InlineData(PlanType.Enterprise, int.MaxValue)]
    public void GetSchemaLimit_ReturnsPlanSpecificLimit(PlanType plan, int expected)
    {
        var tenant = new Tenant { Plan = plan };
        tenant.GetSchemaLimit().Should().Be(expected);
    }

    [Fact]
    public void HasQueryCapacity_BelowLimit_ReturnsTrue()
    {
        var tenant = new Tenant { Plan = PlanType.Starter, MonthlyQueryCount = 100 };
        tenant.HasQueryCapacity().Should().BeTrue();
    }

    [Fact]
    public void HasQueryCapacity_AtLimit_ReturnsFalse()
    {
        var tenant = new Tenant { Plan = PlanType.Starter, MonthlyQueryCount = 500 };
        tenant.HasQueryCapacity().Should().BeFalse();
    }

    [Fact]
    public void HasQueryCapacity_Enterprise_AlwaysReturnsTrue()
    {
        var tenant = new Tenant { Plan = PlanType.Enterprise, MonthlyQueryCount = 999999 };
        tenant.HasQueryCapacity().Should().BeTrue();
    }
}
