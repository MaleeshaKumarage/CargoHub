using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record RequestPasswordResetCommand(RequestPasswordResetRequest Request, string? Environment) : IRequest<AuthResult>;
