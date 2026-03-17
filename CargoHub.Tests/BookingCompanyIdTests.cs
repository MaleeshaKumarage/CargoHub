using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests;

/// <summary>
/// Tests that Booking.CompanyId is set on create/draft as per docs/Scope-Booking-Forms-And-Relationships.md (§7).
/// </summary>
public class BookingCompanyIdTests
{
    private static CreateBookingRequest MinimalRequest() => new()
    {
        ReceiverName = "Test",
        ReceiverAddress1 = "Street 1",
        ReceiverPostalCode = "00100",
        ReceiverCity = "Helsinki",
        ReceiverCountry = "FI"
    };

    // --- Positive: CompanyId set correctly ---

    [Fact]
    public async Task CreateBooking_WhenCompanyIdProvided_SetsBookingCompanyId()
    {
        // When we create a completed booking and pass a company id, the saved booking has that company id.
        var companyId = Guid.NewGuid();
        Booking? captured = null;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand(
            "customer-1",
            "Customer One",
            MinimalRequest(),
            companyId), default);

        Assert.NotNull(captured);
        Assert.Equal(companyId, captured.CompanyId);
    }

    [Fact]
    public async Task CreateBooking_WhenCompanyIdNull_SetsBookingCompanyIdToNull()
    {
        // When we create a booking without a company id, the saved booking has null company id (e.g. legacy or no company).
        Booking? captured = null;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand(
            "customer-1",
            "Customer One",
            MinimalRequest(),
            null), default);

        Assert.NotNull(captured);
        Assert.Null(captured.CompanyId);
    }

    [Fact]
    public async Task CreateDraft_WhenCompanyIdProvided_SetsBookingCompanyId()
    {
        // When we create a draft and pass a company id, the saved draft has that company id and is marked as draft.
        var companyId = Guid.NewGuid();
        Booking? captured = null;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateDraftCommandHandler(repo.Object);
        await handler.Handle(new CreateDraftCommand(
            "customer-1",
            "Customer One",
            MinimalRequest(),
            companyId), default);

        Assert.NotNull(captured);
        Assert.Equal(companyId, captured.CompanyId);
        Assert.True(captured.IsDraft);
    }

    [Fact]
    public async Task CreateDraft_WhenCompanyIdNull_SetsBookingCompanyIdToNull()
    {
        // When we create a draft without a company id, the saved draft has null company id.
        Booking? captured = null;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateDraftCommandHandler(repo.Object);
        await handler.Handle(new CreateDraftCommand(
            "customer-1",
            "Customer One",
            MinimalRequest(),
            null), default);

        Assert.NotNull(captured);
        Assert.Null(captured.CompanyId);
    }

    // --- Edge: minimal request, custom fields ---

    [Fact]
    public async Task CreateBooking_WithMinimalRequest_StillSetsCustomerAndCompanyId()
    {
        // When we create with only the minimum required receiver fields, customer id and company id are still set.
        var companyId = Guid.NewGuid();
        Booking? captured = null;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("cust-min", null, MinimalRequest(), companyId), default);

        Assert.NotNull(captured);
        Assert.Equal("cust-min", captured.CustomerId);
        Assert.Equal(companyId, captured.CompanyId);
        Assert.Equal("cust-min", captured.CustomerName);
    }

}
