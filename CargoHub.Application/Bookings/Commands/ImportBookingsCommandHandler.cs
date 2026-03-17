using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed class ImportBookingsCommandHandler : IRequestHandler<ImportBookingsCommand, ImportBookingsResult>
{
    private readonly IMediator _mediator;

    public ImportBookingsCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ImportBookingsResult> Handle(ImportBookingsCommand request, CancellationToken cancellationToken)
    {
        var created = 0;
        var draft = 0;
        var errors = new List<string>();

        foreach (var row in request.Rows)
        {
            try
            {
                if (row.IsComplete)
                {
                    var result = await _mediator.Send(
                        new CreateBookingCommand(request.CustomerId, request.CustomerName, row.Request, request.CompanyId),
                        cancellationToken);
                    if (result != null)
                        created++;
                    else
                        errors.Add($"Row (ref: {row.Request.ReferenceNumber}): Create failed.");
                }
                else
                {
                    var result = await _mediator.Send(
                        new CreateDraftCommand(request.CustomerId, request.CustomerName, row.Request, request.CompanyId),
                        cancellationToken);
                    if (result != null)
                        draft++;
                    else
                        errors.Add($"Row (ref: {row.Request.ReferenceNumber}): Draft create failed.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row (ref: {row.Request.ReferenceNumber}): {ex.Message}");
            }
        }

        return new ImportBookingsResult(created, draft, errors);
    }
}
