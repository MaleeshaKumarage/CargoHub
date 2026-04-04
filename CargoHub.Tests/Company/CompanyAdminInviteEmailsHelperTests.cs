using System.Text.Json;
using CargoHub.Application.Company;
using Xunit;

namespace CargoHub.Tests.Company;

public class CompanyAdminInviteEmailsHelperTests
{
    [Fact]
    public void NormalizeList_Null_ReturnsEmpty()
    {
        Assert.Empty(CompanyAdminInviteEmailsHelper.NormalizeList(null));
    }

    [Fact]
    public void NormalizeList_TrimsDistinctCaseInsensitive()
    {
        var list = CompanyAdminInviteEmailsHelper.NormalizeList(new[] { " A@b.com ", "a@B.com", "", "  " });
        Assert.Single(list);
        Assert.Equal("A@b.com", list[0]);
    }

    [Fact]
    public void SerializeJson_EmptyAfterNormalize_ReturnsNull()
    {
        Assert.Null(CompanyAdminInviteEmailsHelper.SerializeJson(Array.Empty<string>()));
        Assert.Null(CompanyAdminInviteEmailsHelper.SerializeJson(new[] { "  ", "" }));
    }

    [Fact]
    public void SerializeJson_RoundTripsThroughDeserialize()
    {
        var emails = new[] { "one@test.com", "two@test.com" };
        var json = CompanyAdminInviteEmailsHelper.SerializeJson(emails);
        Assert.NotNull(json);
        var back = CompanyAdminInviteEmailsHelper.DeserializeJson(json);
        Assert.Equal(2, back.Count);
        Assert.Contains("one@test.com", back);
        Assert.Contains("two@test.com", back);
    }

    [Fact]
    public void DeserializeJson_NullOrWhiteSpace_ReturnsEmpty()
    {
        Assert.Empty(CompanyAdminInviteEmailsHelper.DeserializeJson(null));
        Assert.Empty(CompanyAdminInviteEmailsHelper.DeserializeJson("   "));
    }

    [Fact]
    public void DeserializeJson_InvalidJson_ReturnsEmpty()
    {
        Assert.Empty(CompanyAdminInviteEmailsHelper.DeserializeJson("{"));
    }

    [Fact]
    public void GetExplicitTargets_PrefersJsonOverLegacy()
    {
        var json = JsonSerializer.Serialize(new[] { "json@test.com" });
        var targets = CompanyAdminInviteEmailsHelper.GetExplicitTargets(json, "legacy@test.com");
        Assert.Single(targets);
        Assert.Equal("json@test.com", targets[0]);
    }

    [Fact]
    public void GetExplicitTargets_FallsBackToLegacy()
    {
        var targets = CompanyAdminInviteEmailsHelper.GetExplicitTargets(null, "  legacy@test.com  ");
        Assert.Single(targets);
        Assert.Equal("legacy@test.com", targets[0]);
    }

    [Fact]
    public void GetExplicitTargets_EmptyJsonUsesLegacy()
    {
        var targets = CompanyAdminInviteEmailsHelper.GetExplicitTargets("[]", "legacy@test.com");
        Assert.Single(targets);
        Assert.Equal("legacy@test.com", targets[0]);
    }
}
