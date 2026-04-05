using CargoHub.Application.Subscriptions;
using CargoHub.Application.Subscriptions.Queries;
using Moq;
using Xunit;

namespace CargoHub.Tests.Subscriptions;

public class GetPortalCompanySubscriptionQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToReader()
    {
        var expected = new PortalCompanySubscriptionDto { PlanName = "Trial", PlanKind = "Trial", Currency = "EUR" };
        var reader = new Mock<IPortalCompanySubscriptionReader>();
        reader.Setup(r => r.GetForBusinessIdAsync("B1", It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var h = new GetPortalCompanySubscriptionQueryHandler(reader.Object);
        var r = await h.Handle(new GetPortalCompanySubscriptionQuery("B1"), default);
        Assert.Same(expected, r);
    }
}
