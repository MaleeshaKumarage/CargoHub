using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record AcceptCompanyAdminInviteCommand(AcceptCompanyAdminInviteRequest Request) : IRequest<RegisterResult>;
