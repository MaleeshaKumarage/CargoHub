using CargoHub.Application.Company;

namespace CargoHub.Api.Services;

public sealed record CourierValidationError(string ErrorCode, string Message);

/// <summary>Validates booking postal service against company courier contracts.</summary>
public static class BookingCourierValidation
{
    /// <summary>When companyId is null, returns null (validation skipped). Otherwise returns allowed courier ids.</summary>
    public static async Task<HashSet<string>?> LoadAllowedCourierIdsAsync(
        ICompanyRepository companyRepository,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (!companyId.HasValue) return null;
        return await companyRepository.GetEnabledCourierIdsForCompanyAsync(companyId.Value, cancellationToken);
    }

    /// <summary>allowedOrSkip null = skip check. Otherwise postalService must be non-empty and in the set; set must be non-empty for any booking.</summary>
    public static CourierValidationError? ValidatePostalServiceForCompany(
        HashSet<string>? allowedOrSkip,
        string? postalService)
    {
        if (allowedOrSkip == null) return null;
        var ps = (postalService ?? "").Trim();
        if (allowedOrSkip.Count == 0)
        {
            return new CourierValidationError(
                "NoCourierContracts",
                "Configure at least one courier contract before creating bookings.");
        }

        if (string.IsNullOrEmpty(ps))
        {
            return new CourierValidationError(
                "CourierRequired",
                "Courier (postal service) is required.");
        }

        if (!allowedOrSkip.Contains(ps))
        {
            return new CourierValidationError(
                "CourierNotAllowed",
                "Selected courier is not enabled for your company.");
        }

        return null;
    }
}
