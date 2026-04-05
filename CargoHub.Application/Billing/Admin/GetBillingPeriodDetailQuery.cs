using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetBillingPeriodDetailQuery(Guid PeriodId) : IRequest<BillingPeriodDetailDto?>;

public sealed class GetBillingPeriodDetailQueryHandler : IRequestHandler<GetBillingPeriodDetailQuery, BillingPeriodDetailDto?>
{
    private readonly IAdminBillingReader _reader;

    public GetBillingPeriodDetailQueryHandler(IAdminBillingReader reader) => _reader = reader;

    public Task<BillingPeriodDetailDto?> Handle(GetBillingPeriodDetailQuery request, CancellationToken cancellationToken) =>
        _reader.GetBillingPeriodDetailAsync(request.PeriodId, cancellationToken);
}
