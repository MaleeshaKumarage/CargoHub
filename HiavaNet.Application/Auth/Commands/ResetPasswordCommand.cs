using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Commands;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest<AuthResult>;
