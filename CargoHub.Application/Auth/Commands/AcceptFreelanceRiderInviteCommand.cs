using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record AcceptFreelanceRiderInviteCommand(AcceptFreelanceRiderInviteRequest Request) : IRequest<RegisterResult>;
