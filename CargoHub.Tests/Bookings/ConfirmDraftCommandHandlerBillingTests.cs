using CargoHub.Application.Billing;
using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ConfirmDraftCommandHandlerBillingTests
{
    [Fact]
    public async Task Handle_PropagatesSubscriptionBillingException()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        var billing = new Mock<ISubscriptionBillingOrchestrator>();
        billing.Setup(b => b.ConfirmDraftWithBillingAsync(id, "cust", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionBillingException("TrialBookingLimitExceeded", "Trial exhausted."));
        var handler = new ConfirmDraftCommandHandler(repo.Object, billing.Object);
        await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            handler.Handle(new ConfirmDraftCommand(id, "cust"), default));
        repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
