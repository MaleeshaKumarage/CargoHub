using MediatR;

namespace CargoHub.Application.Subscriptions.Queries;

public sealed class GetPortalCompanySubscriptionQueryHandler : IRequestHandler<GetPortalCompanySubscriptionQuery, PortalCompanySubscriptionDto?>
{
    private readonly IPortalCompanySubscriptionReader _reader;

    public GetPortalCompanySubscriptionQueryHandler(IPortalCompanySubscriptionReader reader)
    {
        _reader = reader;
    }

    public Task<PortalCompanySubscriptionDto?> Handle(GetPortalCompanySubscriptionQuery request, CancellationToken cancellationToken) =>
        _reader.GetForBusinessIdAsync(request.BusinessId, cancellationToken);
}
