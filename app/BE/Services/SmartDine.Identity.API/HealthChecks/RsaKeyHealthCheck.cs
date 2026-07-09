using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Identity.API.HealthChecks;

public class RsaKeyHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RsaKeyHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rsaKeyService = new RsaKeyService(_configuration);
            var rsaKey = rsaKeyService.GetRsaKey();

            if (rsaKey == null || !rsaKey.HasPrivateKey)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("RSA key is not properly configured or missing private key"));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy("RSA key is properly configured"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy($"RSA key health check failed: {ex.Message}"));
        }
    }
}