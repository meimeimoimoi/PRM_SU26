using System.Security.Cryptography;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Identity.API.HealthChecks;

public class RsaKeyHealthCheck : IHealthCheck
{
    private readonly IRsaKeyProvider _keyProvider;

    public RsaKeyHealthCheck(IRsaKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rsaParams = _keyProvider.PrivateKeyParameters;

            if (rsaParams.D == null)
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