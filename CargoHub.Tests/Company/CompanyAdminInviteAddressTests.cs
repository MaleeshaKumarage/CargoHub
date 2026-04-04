using CargoHub.Application.Company;
using Xunit;

namespace CargoHub.Tests.Company;

public class CompanyAdminInviteAddressTests
{
    [Fact]
    public void SanitizeLocalPart_Null_ReturnsCompany()
    {
        Assert.Equal("company", CompanyAdminInviteAddress.SanitizeLocalPart(null!));
    }

    [Theory]
    [InlineData("", "company")]
    [InlineData("   ", "company")]
    public void SanitizeLocalPart_EmptyOrWhitespace_ReturnsCompany(string input, string expected)
    {
        Assert.Equal(expected, CompanyAdminInviteAddress.SanitizeLocalPart(input));
    }

    [Fact]
    public void SanitizeLocalPart_KeepsAsciiLettersAndDigits()
    {
        Assert.Equal("ab12", CompanyAdminInviteAddress.SanitizeLocalPart("Ab12"));
    }

    [Fact]
    public void SanitizeLocalPart_AllowsSafePunctuation_TrimsTrailingHyphenFromResult()
    {
        Assert.Equal("a._+%", CompanyAdminInviteAddress.SanitizeLocalPart("a._+%-"));
    }

    [Fact]
    public void SanitizeLocalPart_ReplacesUnsafeCharsWithHyphen()
    {
        Assert.Equal("a-b", CompanyAdminInviteAddress.SanitizeLocalPart("a@b"));
    }

    [Fact]
    public void SanitizeLocalPart_CollapsesDoubleHyphens()
    {
        Assert.Equal("a-b", CompanyAdminInviteAddress.SanitizeLocalPart("a--b"));
    }

    [Fact]
    public void SanitizeLocalPart_TrimsLeadingTrailingHyphensFromSegment()
    {
        Assert.Equal("x", CompanyAdminInviteAddress.SanitizeLocalPart("-x-"));
    }

    [Fact]
    public void SanitizeLocalPart_AllNonSafeBecomesEmpty_UsesCompany()
    {
        Assert.Equal("company", CompanyAdminInviteAddress.SanitizeLocalPart("@@@"));
    }

    [Fact]
    public void SanitizeLocalPart_TruncatesToMaxLocalPartLength()
    {
        var longId = new string('a', CompanyAdminInviteAddress.MaxLocalPartLength + 20);
        var s = CompanyAdminInviteAddress.SanitizeLocalPart(longId);
        Assert.Equal(CompanyAdminInviteAddress.MaxLocalPartLength, s.Length);
    }

    [Fact]
    public void SanitizeLocalPart_TruncationTrimsTrailingHyphen()
    {
        var pad = new string('a', CompanyAdminInviteAddress.MaxLocalPartLength - 1);
        var input = pad + "---b";
        var s = CompanyAdminInviteAddress.SanitizeLocalPart(input);
        Assert.True(s.Length <= CompanyAdminInviteAddress.MaxLocalPartLength);
        Assert.False(s.EndsWith('-'));
    }

    [Theory]
    [InlineData("123", null, "123@example.com")]
    [InlineData("123", "", "123@example.com")]
    [InlineData("123", "  ", "123@example.com")]
    [InlineData("123", "mail.example.com", "123@mail.example.com")]
    public void BuildFallbackEmail_UsesSanitizedLocalAndDomain(string bid, string? domain, string expected)
    {
        Assert.Equal(expected, CompanyAdminInviteAddress.BuildFallbackEmail(bid, domain!));
    }
}
