using CargoHub.Application.Billing;
using Xunit;

namespace CargoHub.Tests.Billing;

public class SubscriptionBillingExceptionTests
{
    [Fact]
    public void Ctor_SetsErrorCodeAndMessage()
    {
        var ex = new SubscriptionBillingException("CODE", "msg");
        Assert.Equal("CODE", ex.ErrorCode);
        Assert.Equal("msg", ex.Message);
    }
}
