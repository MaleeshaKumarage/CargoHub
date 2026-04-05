using CargoHub.Application.AdminEmail;
using Xunit;

namespace CargoHub.Tests.AdminEmail;

public sealed class ReleaseNotesEmailBodyFormatterTests
{
    [Fact]
    public void ToHtml_wraps_pre_wrap_and_encodes_html()
    {
        var html = ReleaseNotesEmailBodyFormatter.ToHtml("a <b> & c");
        Assert.Contains("white-space: pre-wrap", html);
        Assert.Contains("a &lt;b&gt; &amp; c", html);
        Assert.DoesNotContain("<b>", html);
    }

    [Fact]
    public void ToHtml_preserves_newlines_for_email_clients()
    {
        var html = ReleaseNotesEmailBodyFormatter.ToHtml("line1\n\n  spaced");
        Assert.Contains("line1", html);
        Assert.Contains("\n\n", html);
        Assert.Contains("  spaced", html);
    }

    [Fact]
    public void ToHtml_null_becomes_empty_encoded_block()
    {
        var html = ReleaseNotesEmailBodyFormatter.ToHtml(null!);
        Assert.Contains("white-space: pre-wrap", html);
        Assert.Contains("<div", html);
        Assert.DoesNotContain("null", html);
    }
}
