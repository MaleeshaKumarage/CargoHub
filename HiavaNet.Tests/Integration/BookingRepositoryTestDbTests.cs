using HiavaNet.Application.Bookings;
using HiavaNet.Application.Bookings.Commands;
using HiavaNet.Application.Bookings.Dtos;
using HiavaNet.Domain.Bookings;
using HiavaNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HiavaNet.Tests.Integration;

/// <summary>
/// Integration tests using the in-memory test database. Verify that bookings are stored with CompanyId and can be read back.
/// </summary>
public class BookingRepositoryTestDbTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public BookingRepositoryTestDbTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddBooking_WithCompanyId_PersistsAndCanBeReadBack()
    {
        // When we add a booking with a company id, it is saved and we can load it with the same company id.
        var companyId = Guid.NewGuid();
        var customerId = "cust-1";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "Receiver",
            ReceiverAddress1 = "Addr 1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var handler = new CreateBookingCommandHandler(repo);
        var result = await handler.Handle(new CreateBookingCommand(customerId, "Customer", request, companyId), default);

        Assert.NotNull(result);
        var loaded = await repo.GetByIdAsync(result.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal(companyId, loaded.CompanyId);
        Assert.False(loaded.IsDraft);
    }

    [Fact]
    public async Task AddDraft_WithCompanyId_PersistsAndAppearsInDraftList()
    {
        // When we add a draft with a company id, it is saved and appears when we list drafts for that customer.
        var companyId = Guid.NewGuid();
        var customerId = "cust-draft";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var handler = new CreateDraftCommandHandler(repo);
        var result = await handler.Handle(new CreateDraftCommand(customerId, "C", request, companyId), default);

        Assert.NotNull(result);
        var drafts = await repo.ListDraftsByCustomerIdAsync(customerId, 0, 10, default);
        Assert.Single(drafts);
        Assert.Equal(companyId, drafts[0].CompanyId);
        Assert.True(drafts[0].IsDraft);
    }

    [Fact]
    public async Task ListBookings_ReturnsOnlyCompleted_NotDrafts()
    {
        // When we list bookings by customer, only completed bookings are returned; drafts are separate.
        var customerId = "cust-list";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        var completed = await new CreateBookingCommandHandler(repo).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(completed);

        var list = await repo.ListByCustomerIdAsync(customerId, 0, 10, default);
        Assert.Single(list);
        Assert.Equal(completed.Id, list[0].Id);
        Assert.False(list[0].IsDraft);
    }

    [Fact]
    public async Task GetById_WhenBookingDoesNotExist_ReturnsNull()
    {
        // When we ask for a booking that does not exist, the repository returns null.
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var loaded = await repo.GetByIdAsync(Guid.NewGuid(), default);
        Assert.Null(loaded);
    }
}
