using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Commands;

/// <summary>
/// Command used to authenticate a portal user with email/account and password.
/// </summary>
public sealed record LoginUserCommand(PortalLoginRequest Request) : IRequest<LoginResult>;

