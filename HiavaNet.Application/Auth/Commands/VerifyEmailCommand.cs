using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Commands;

public sealed record VerifyEmailCommand(VerifyRequest Request) : IRequest<AuthResult>;
