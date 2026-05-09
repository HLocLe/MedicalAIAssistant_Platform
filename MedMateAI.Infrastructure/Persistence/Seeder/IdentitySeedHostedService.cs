using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MedMateAI.Infrastructure.Persistence.Seeder;

public sealed class IdentitySeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IdentitySeedHostedService> _logger;

    public IdentitySeedHostedService(IServiceProvider services, ILogger<IdentitySeedHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await IdentitySeeder.SeedAsync(_services, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed Identity roles/admin.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

