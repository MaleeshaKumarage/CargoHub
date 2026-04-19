using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CargoHub.Infrastructure.FreelanceRiders;

/// <summary>Periodically clears rider assignments that were not accepted before the deadline.</summary>
public sealed class RiderAssignmentLapseHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(45);
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RiderAssignmentLapseHostedService> _logger;

    public RiderAssignmentLapseHostedService(IServiceProvider serviceProvider, ILogger<RiderAssignmentLapseHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.Database.IsRelational())
                {
                    await Task.Delay(Interval, stoppingToken);
                    continue;
                }

                var now = DateTime.UtcNow;
                var pending = await db.Bookings
                    .Where(b =>
                        !b.IsDraft &&
                        b.FreelanceRiderId != null &&
                        b.FreelanceRiderAcceptedAtUtc == null &&
                        b.FreelanceRiderAssignmentDeadlineUtc != null &&
                        b.FreelanceRiderAssignmentDeadlineUtc < now &&
                        !b.FreelanceRiderAssignmentLapsed)
                    .ToListAsync(stoppingToken);

                foreach (var b in pending)
                {
                    b.FreelanceRiderAssignmentLapsed = true;
                    b.FreelanceRiderId = null;
                    b.UpdatedAtUtc = now;
                }

                if (pending.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Lapsed {Count} rider assignments.", pending.Count);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Rider assignment lapse sweep failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
