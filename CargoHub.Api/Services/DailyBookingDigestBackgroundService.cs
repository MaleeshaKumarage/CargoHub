using CargoHub.Api.Options;
using CargoHub.Application.Bookings;
using Microsoft.Extensions.Options;

namespace CargoHub.Api.Services;

/// <summary>Waits until the configured local time, then runs the digest for the previous local calendar day.</summary>
public sealed class DailyBookingDigestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<DailyDigestOptions> _options;
    private readonly ILogger<DailyBookingDigestBackgroundService> _logger;

    public DailyBookingDigestBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<DailyDigestOptions> options,
        ILogger<DailyBookingDigestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opt = _options.CurrentValue;
            if (!opt.Enabled)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            var tzId = string.IsNullOrWhiteSpace(opt.TimeZoneId) ? "UTC" : opt.TimeZoneId.Trim();
            if (!DailyDigestTimeHelper.TryParseRunAtLocalTime(opt.RunAtLocalTime, out var runAt))
            {
                _logger.LogError("DailyDigest:RunAtLocalTime is invalid ({Value}). Expected HH:mm.", opt.RunAtLocalTime);
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                continue;
            }

            try
            {
                var utcNow = DateTime.UtcNow;
                var nextRunUtc = DailyDigestTimeHelper.GetNextRunUtc(tzId, runAt, utcNow);
                var delay = nextRunUtc - utcNow;
                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                opt = _options.CurrentValue;
                if (!opt.Enabled)
                    continue;

                tzId = string.IsNullOrWhiteSpace(opt.TimeZoneId) ? "UTC" : opt.TimeZoneId.Trim();
                var todayLocal = DailyDigestTimeHelper.GetLocalDateToday(tzId, DateTime.UtcNow);
                var yesterdayLocal = todayLocal.AddDays(-1);

                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IDailyBookingDigestOrchestrator>();
                await orchestrator.ProcessDigestForLocalDateAsync(yesterdayLocal, tzId, opt.SkipIfEmpty, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily digest background run failed.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
