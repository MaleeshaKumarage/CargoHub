using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Commands;

public sealed record UpdateVerificationStatusCommand(UpdateVerificationStatusRequest Request) : IRequest<AuthResult>;
