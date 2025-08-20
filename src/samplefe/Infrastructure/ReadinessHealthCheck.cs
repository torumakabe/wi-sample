using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SampleFe.Infrastructure;

public sealed class ReadinessHealthCheck(ReadinessState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(state.IsReady
            ? HealthCheckResult.Healthy("ready")
            : HealthCheckResult.Unhealthy("not ready"));
    }
}

