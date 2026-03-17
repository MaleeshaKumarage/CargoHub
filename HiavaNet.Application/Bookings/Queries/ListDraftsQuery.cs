using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), returns all companies' drafts.</param>
public sealed record ListDraftsQuery(string? CustomerId, int Skip = 0, int Take = 100) : IRequest<List<BookingListDto>>;
