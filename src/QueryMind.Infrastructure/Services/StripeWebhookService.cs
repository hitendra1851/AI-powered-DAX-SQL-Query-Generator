using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QueryMind.Infrastructure.Services;

public class StripeWebhookService(IConfiguration configuration, ILogger<StripeWebhookService> logger)
{
    private readonly string _webhookUrl = configuration["Stripe:WebhookUrl"] ?? string.Empty;

    public async Task SendUsageEventAsync(Guid tenantId, string stripeCustomerId, int queryCount, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_webhookUrl) || string.IsNullOrEmpty(stripeCustomerId))
            return;

        try
        {
            using var http = new HttpClient();
            var payload = new
            {
                customerId = stripeCustomerId,
                tenantId = tenantId,
                queries = queryCount,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            await http.PostAsync(_webhookUrl, content, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send Stripe usage event for tenant {TenantId}", tenantId);
        }
    }
}
