using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<AuthResult>;
