using MediatR;

namespace CargoHub.Application.Subscriptions.Queries;

public sealed record GetPortalCompanySubscriptionQuery(string BusinessId) : IRequest<PortalCompanySubscriptionDto?>;
