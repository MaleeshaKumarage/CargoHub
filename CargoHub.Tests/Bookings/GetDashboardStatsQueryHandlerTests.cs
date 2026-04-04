using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class GetDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var expected = new DashboardBookingStatsDto
        {
            CountToday = 5,
            CountMonth = 20,
            CountYear = 100,
            ByCourier = new List<CountByKeyDto> { new() { Key = "DHL", Count = 10 } },
            FromCities = new List<CountByKeyDto>(),
            ToCities = new List<CountByKeyDto>()
        };
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetDashboardStatsAsync("cust-1", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetDashboardStatsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDashboardStatsQuery("cust-1", null), default);

        Assert.Equal(5, result.CountToday);
        Assert.Equal(20, result.CountMonth);
        Assert.Equal(100, result.CountYear);
        Assert.Single(result.ByCourier);
        Assert.Equal("DHL", result.ByCourier[0].Key);
    }

    [Fact]
    public async Task Handle_WithNullCustomerId_DelegatesToRepository()
    {
        var expected = new DashboardBookingStatsDto { CountToday = 0, CountMonth = 0, CountYear = 0 };
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetDashboardStatsAsync(null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetDashboardStatsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDashboardStatsQuery(null, null), default);

        Assert.Equal(0, result.CountToday);
    }

    [Fact]
    public async Task Handle_WithScopeAndHeatmap_DelegatesToRepository()
    {
        var expected = new DashboardBookingStatsDto { Scope = "drafts", CountMonth = 2 };
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetDashboardStatsAsync("c1", "drafts", 2024, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetDashboardStatsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDashboardStatsQuery("c1", "drafts", 2024, 3), default);

        Assert.Equal("drafts", result.Scope);
        Assert.Equal(2, result.CountMonth);
        repo.Verify(r => r.GetDashboardStatsAsync("c1", "drafts", 2024, 3, It.IsAny<CancellationToken>()), Times.Once);
    }
}
