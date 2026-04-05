using CargoHub.Application.Billing;
using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class CreateBookingCommandHandlerBillingTests
{
    private static CreateBookingRequest MinimalRequest() => new()
    {
        ReceiverName = "R",
        ReceiverAddress1 = "A1",
        ReceiverPostalCode = "00100",
        ReceiverCity = "Helsinki",
        ReceiverCountry = "FI"
    };

    [Fact]
    public async Task Handle_PropagatesSubscriptionBillingException_FromAssert()
    {
        var repo = new Mock<IBookingRepository>();
        var billing = new Mock<ISubscriptionBillingOrchestrator>();
        billing.Setup(b => b.AssertBillableBookingAllowedAsync(It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionBillingException("TrialBookingLimitExceeded", "Trial exhausted."));
        var handler = new CreateBookingCommandHandler(repo.Object, billing.Object);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            handler.Handle(new CreateBookingCommand("c1", "N", MinimalRequest(), Guid.NewGuid()), default));
        Assert.Equal("TrialBookingLimitExceeded", ex.ErrorCode);
        repo.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallsPostBilling_AfterAdd()
    {
        var repo = new Mock<IBookingRepository>();
        Guid capturedId = default;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => capturedId = b.Id)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var billing = new Mock<ISubscriptionBillingOrchestrator>();
        billing.Setup(b => b.AssertBillableBookingAllowedAsync(It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        billing.Setup(b => b.PostBillingForNewCompletedBookingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateBookingCommandHandler(repo.Object, billing.Object);
        await handler.Handle(new CreateBookingCommand("c1", "N", MinimalRequest(), Guid.NewGuid()), default);
        billing.Verify(b => b.PostBillingForNewCompletedBookingAsync(capturedId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
