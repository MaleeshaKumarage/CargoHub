using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Commands;

public sealed record VerifyEmailCommand(VerifyRequest Request) : IRequest<AuthResult>;
