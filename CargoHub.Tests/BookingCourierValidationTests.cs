using CargoHub.Api.Services;
using Xunit;

namespace CargoHub.Tests;

public class BookingCourierValidationTests
{
    [Fact]
    public void ValidatePostalService_WhenAllowedNull_ReturnsNull()
    {
        Assert.Null(BookingCourierValidation.ValidatePostalServiceForCompany(null, "DHL"));
    }

    [Fact]
    public void ValidatePostalService_WhenAllowedEmpty_ReturnsNoCourierContracts()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var err = BookingCourierValidation.ValidatePostalServiceForCompany(allowed, "DHL");
        Assert.NotNull(err);
        Assert.Equal("NoCourierContracts", err.ErrorCode);
    }

    [Fact]
    public void ValidatePostalService_WhenPostalServiceEmpty_ReturnsCourierRequired()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHLExpress" };
        var err = BookingCourierValidation.ValidatePostalServiceForCompany(allowed, null);
        Assert.NotNull(err);
        Assert.Equal("CourierRequired", err.ErrorCode);
    }

    [Fact]
    public void ValidatePostalService_WhenNotInSet_ReturnsCourierNotAllowed()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHLExpress" };
        var err = BookingCourierValidation.ValidatePostalServiceForCompany(allowed, "Other");
        Assert.NotNull(err);
        Assert.Equal("CourierNotAllowed", err.ErrorCode);
    }

    [Fact]
    public void ValidatePostalService_WhenMatchCaseInsensitive_ReturnsNull()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DHLExpress" };
        Assert.Null(BookingCourierValidation.ValidatePostalServiceForCompany(allowed, "dhlexpress"));
    }
}
