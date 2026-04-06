using CargoHub.Infrastructure.Company;
using CargoHub.Infrastructure.Options;
using Xunit;

namespace CargoHub.Tests.Company;

public class CompanyAdminInviteEmailHtmlTests
{
    [Fact]
    public void BuildContactFooter_EmptyOptions_ReturnsEmpty()
    {
        var html = CompanyAdminInviteEmailHtml.BuildContactFooter(new PortalPublicOptions());
        Assert.Equal("", html);
    }

    [Fact]
    public void BuildContactFooter_NameAndEmail_ProducesParagraphWithMailto()
    {
        var html = CompanyAdminInviteEmailHtml.BuildContactFooter(new PortalPublicOptions
        {
            AdminInviteContactName = "Maleesha Kumarage",
            AdminInviteContactEmail = "contactmaleesha93@gmail.com"
        });

        Assert.Contains("For more information, you can contact Maleesha Kumarage", html);
        Assert.Contains("mailto:contactmaleesha93@gmail.com", html);
        Assert.StartsWith("<p>", html);
        Assert.EndsWith("</p>", html);
    }

    [Fact]
    public void BuildContactFooter_EmailOnly_UsesShortWording()
    {
        var html = CompanyAdminInviteEmailHtml.BuildContactFooter(new PortalPublicOptions
        {
            AdminInviteContactName = "",
            AdminInviteContactEmail = "only@example.com"
        });

        Assert.Contains("For more information, contact", html);
        Assert.Contains("mailto:only@example.com", html);
    }

    [Fact]
    public void BuildContactFooter_NameOnly_OmitsMailto()
    {
        var html = CompanyAdminInviteEmailHtml.BuildContactFooter(new PortalPublicOptions
        {
            AdminInviteContactName = "Support",
            AdminInviteContactEmail = ""
        });

        Assert.Contains("you can contact Support", html);
        Assert.DoesNotContain("mailto:", html);
    }

    [Fact]
    public void BuildContactFooter_EncodesHtmlInName()
    {
        var html = CompanyAdminInviteEmailHtml.BuildContactFooter(new PortalPublicOptions
        {
            AdminInviteContactName = "A & B <script>",
            AdminInviteContactEmail = "x@y.com"
        });

        Assert.Contains("A &amp; B &lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
    }
}
