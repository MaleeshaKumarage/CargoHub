using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record UpdateVerificationStatusCommand(UpdateVerificationStatusRequest Request) : IRequest<AuthResult>;
