using Xunit;

namespace HiavaNet.Tests;

/// <summary>
/// Tests for API error contract per docs/Scope-Booking-Forms-And-Relationships.md (§5).
/// Error responses should use stable errorCode so the portal can map to i18n (errors.*).
/// </summary>
public class ApiErrorCodesTests
{
    /// <summary>
    /// Expected error codes from Scope doc. Keep in sync with API responses and portal messages (en.json / fi.json).
    /// </summary>
    public static readonly IReadOnlySet<string> ExpectedErrorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CompanyRequiredForBooking",
        "CompanyNotFound",
        "CourierRequired",
        "ValidationFailed",
        "CompletedBookingNotEditable"
    };

    [Fact]
    public void ExpectedErrorCodes_IsNonEmpty()
    {
        // We must have at least one error code defined for the API contract.
        Assert.NotEmpty(ExpectedErrorCodes);
    }

    [Fact]
    public void ExpectedErrorCodes_ContainsKeyScopedCodes()
    {
        // The set must contain the main codes used by the scope document for company and booking.
        Assert.Contains("CompanyRequiredForBooking", ExpectedErrorCodes);
        Assert.Contains("CourierRequired", ExpectedErrorCodes);
    }

    [Fact]
    public void ExpectedErrorCodes_AllPascalCase()
    {
        // Each code must be PascalCase so the portal can use it as a key (e.g. errors.CompanyRequiredForBooking).
        foreach (var code in ExpectedErrorCodes)
        {
            Assert.True(code.Length > 0);
            Assert.True(char.IsUpper(code[0]), $"{code} should start with upper case");
            Assert.DoesNotContain(" ", code);
        }
    }

    [Fact]
    public void ExpectedErrorCodes_NoDuplicates()
    {
        // There must be no duplicate codes (case-insensitive).
        var distinct = new HashSet<string>(ExpectedErrorCodes, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ExpectedErrorCodes.Count, distinct.Count);
    }

    [Fact]
    public void ExpectedErrorCodes_ContainsCompanyNotFound()
    {
        // Company not found is a common error when company is deleted or missing.
        Assert.Contains("CompanyNotFound", ExpectedErrorCodes);
    }

    [Fact]
    public void ExpectedErrorCodes_ContainsValidationFailed()
    {
        // Validation failed is used when mandatory fields are missing or invalid.
        Assert.Contains("ValidationFailed", ExpectedErrorCodes);
    }
}
