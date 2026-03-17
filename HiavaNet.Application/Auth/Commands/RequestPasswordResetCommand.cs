using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Commands;

public sealed record RequestPasswordResetCommand(RequestPasswordResetRequest Request, string? Environment) : IRequest<AuthResult>;
