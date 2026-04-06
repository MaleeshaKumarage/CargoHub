using CargoHub.Infrastructure.Options;

namespace CargoHub.Infrastructure.Company;

/// <summary>HTML fragments for company admin invite emails.</summary>
public static class CompanyAdminInviteEmailHtml
{
    /// <summary>
    /// Optional footer when <see cref="PortalPublicOptions.AdminInviteContactName"/> /
    /// <see cref="PortalPublicOptions.AdminInviteContactEmail"/> are set in configuration.
    /// </summary>
    public static string BuildContactFooter(PortalPublicOptions portal)
    {
        var name = portal.AdminInviteContactName?.Trim();
        var email = portal.AdminInviteContactEmail?.Trim();
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(email))
            return "";

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email))
        {
            return
                "<p>For more information, you can contact " +
                System.Net.WebUtility.HtmlEncode(name) +
                " (<a href=\"mailto:" + System.Net.WebUtility.HtmlEncode(email) + "\">" +
                System.Net.WebUtility.HtmlEncode(email) + "</a>).</p>";
        }

        if (!string.IsNullOrEmpty(email))
        {
            return "<p>For more information, contact <a href=\"mailto:" +
                   System.Net.WebUtility.HtmlEncode(email) + "\">" +
                   System.Net.WebUtility.HtmlEncode(email) + "</a>.</p>";
        }

        return "<p>For more information, you can contact " + System.Net.WebUtility.HtmlEncode(name) + ".</p>";
    }
}
