using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record ListAdminSubscriptionPlansQuery : IRequest<IReadOnlyList<AdminSubscriptionPlanSummaryDto>>;

public sealed class ListAdminSubscriptionPlansQueryHandler : IRequestHandler<ListAdminSubscriptionPlansQuery, IReadOnlyList<AdminSubscriptionPlanSummaryDto>>
{
    private readonly IAdminBillingReader _reader;

    public ListAdminSubscriptionPlansQueryHandler(IAdminBillingReader reader) => _reader = reader;

    public Task<IReadOnlyList<AdminSubscriptionPlanSummaryDto>> Handle(ListAdminSubscriptionPlansQuery request, CancellationToken cancellationToken) =>
        _reader.ListSubscriptionPlansAsync(cancellationToken);
}
