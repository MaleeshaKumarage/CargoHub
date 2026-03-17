using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

/// <summary>
/// Command used to register a new portal user. Company (government ID) must already exist.
/// </summary>
public sealed record RegisterUserCommand(PortalRegisterRequest Request) : IRequest<RegisterResult>;

