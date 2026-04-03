namespace CargoHub.Application.AdminCompanies;

public sealed class AdminCompanyMutationResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public AdminCompanyDetailDto? Company { get; init; }
}

/// <summary>Super Admin company row + live counts.</summary>
public sealed class AdminCompanyDetailDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? BusinessId { get; init; }
    public string CompanyId { get; init; } = "";
    public int? MaxUserAccounts { get; init; }
    public int? MaxAdminAccounts { get; init; }
    public string? InitialAdminInviteEmail { get; init; }
    public int ActiveUserCount { get; init; }
    public int AdminCount { get; init; }
}
