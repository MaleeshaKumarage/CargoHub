using CargoHub.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CargoHub.Tests.PortalPublicUrl;

public class PortalPublicBaseUrlResolverTests
{
    private static IConfiguration Config(params (string Key, string Value)[] pairs)
    {
        var dict = pairs.ToDictionary(p => p.Key, p => p.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public void Resolve_UsesPublicBaseUrl_WhenSet()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "https://app.example.com/" };
        var url = PortalPublicBaseUrlResolver.Resolve(portal, Config());
        Assert.Equal("https://app.example.com", url);
    }

    [Fact]
    public void Resolve_FallsBackToCorsPortalOrigin_WhenPublicBaseUrlEmpty()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "" };
        var cfg = Config(("Cors:PortalOrigin", "https://portal.prod.example/"));
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("https://portal.prod.example", url);
    }

    [Fact]
    public void Resolve_FallsBackToFirstPortalOrigin_WhenOthersEmpty()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "  " };
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:PortalOrigins:0"] = "",
                ["Cors:PortalOrigins:1"] = "https://first-valid.example"
            })
            .Build();
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("https://first-valid.example", url);
    }

    [Fact]
    public void Resolve_DefaultsToLocalhost_WhenNothingConfigured()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "" };
        var url = PortalPublicBaseUrlResolver.Resolve(portal, Config());
        Assert.Equal("http://localhost:3000", url);
    }

    [Fact]
    public void Resolve_TrimsTrailingSlash_FromPublicBaseUrl()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "https://portal.example.com///" };
        var url = PortalPublicBaseUrlResolver.Resolve(portal, Config());
        Assert.Equal("https://portal.example.com", url);
    }

    [Fact]
    public void Resolve_TrimsTrailingSlash_FromCorsPortalOrigin()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "" };
        var cfg = Config(("Cors:PortalOrigin", "https://cors.example.com/path/"));
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("https://cors.example.com/path", url);
    }

    [Fact]
    public void Resolve_WhenPortalOriginWhitespace_SkipsToPortalOrigins()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "" };
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:PortalOrigin"] = "   ",
                ["Cors:PortalOrigins:0"] = "https://from-array.example/",
            })
            .Build();
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("https://from-array.example", url);
    }

    [Fact]
    public void Resolve_WhenPortalOriginsOnlyBlanks_DefaultsToLocalhost()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "" };
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:PortalOrigins:0"] = "",
                ["Cors:PortalOrigins:1"] = "  ",
            })
            .Build();
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("http://localhost:3000", url);
    }

    [Fact]
    public void Resolve_WhenPublicBaseUrlNull_FallsBackToConfiguration()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = null! };
        var cfg = Config(("Cors:PortalOrigin", "https://fallback.example"));
        var url = PortalPublicBaseUrlResolver.Resolve(portal, cfg);
        Assert.Equal("https://fallback.example", url);
    }

    [Fact]
    public void ResolveTourUrl_UsesTourUrl_WhenSet()
    {
        var portal = new PortalPublicOptions
        {
            PublicBaseUrl = "https://ignored.example",
            TourUrl = "https://tour.example.com/en/tour/",
        };
        var url = PortalPublicBaseUrlResolver.ResolveTourUrl(portal, Config());
        Assert.Equal("https://tour.example.com/en/tour", url);
    }

    [Fact]
    public void ResolveTourUrl_AppendsEnTour_WhenTourUrlEmpty()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "https://app.example.com", TourUrl = "" };
        var url = PortalPublicBaseUrlResolver.ResolveTourUrl(portal, Config());
        Assert.Equal("https://app.example.com/en/tour", url);
    }

    [Fact]
    public void ResolveTourUrl_UsesResolvedBase_WhenTourUrlEmptyAndPublicBaseEmpty()
    {
        var portal = new PortalPublicOptions { PublicBaseUrl = "", TourUrl = "" };
        var cfg = Config(("Cors:PortalOrigin", "https://portal.example"));
        var url = PortalPublicBaseUrlResolver.ResolveTourUrl(portal, cfg);
        Assert.Equal("https://portal.example/en/tour", url);
    }
}
