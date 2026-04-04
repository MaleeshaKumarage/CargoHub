using CargoHub.Application.Couriers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Registers courier clients and factory. Call from Program.cs or host builder.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all courier integration services: factory, HTTP-based clients (DHL Express, Matkahuolto),
    /// email-based client (Hämeen Tavarataxi), and optional SMTP sender.
    /// </summary>
    public static IServiceCollection AddCourierClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DhlExpressOptions>(configuration.GetSection(DhlExpressOptions.SectionName));
        services.Configure<MatkahuoltoOptions>(configuration.GetSection(MatkahuoltoOptions.SectionName));
        services.Configure<HameenTavarataxiOptions>(configuration.GetSection(HameenTavarataxiOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IConfigureOptions<SmtpOptions>, SmtpOptionsLegacyEnvironmentConfigurer>();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.AddHttpClient(DhlExpressCourierClient.HttpClientName, (sp, client) =>
        {
            var opt = configuration.GetSection(DhlExpressOptions.SectionName).Get<DhlExpressOptions>();
            client.BaseAddress = opt?.BaseUrl != null ? new Uri(opt.BaseUrl) : new Uri("https://express.api.dhl.com");
        });
        services.AddSingleton<DhlExpressCourierClient>();
        services.AddSingleton<ICourierBookingClient>(sp => sp.GetRequiredService<DhlExpressCourierClient>());

        services.AddHttpClient(MatkahuoltoCourierClient.HttpClientName);
        services.AddSingleton<MatkahuoltoCourierClient>();
        services.AddSingleton<ICourierBookingClient>(sp => sp.GetRequiredService<MatkahuoltoCourierClient>());

        services.AddSingleton<HameenTavarataxiCourierClient>();
        services.AddSingleton<ICourierBookingClient>(sp => sp.GetRequiredService<HameenTavarataxiCourierClient>());

        services.AddSingleton<ICourierBookingClientFactory>(sp =>
        {
            var clients = sp.GetServices<ICourierBookingClient>().ToList();
            return new CourierBookingClientFactory(clients);
        });

        return services;
    }
}
