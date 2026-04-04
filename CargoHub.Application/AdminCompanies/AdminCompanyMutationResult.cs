namespace CargoHub.Application.AdminCompanies;

public sealed class AdminCompanyMutationResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public AdminCompanyDetailDto? Company { get; init; }
    public LimitReductionRequiredDetails? LimitReductionRequired { get; init; }
}

/// <summary>Returned when lowering caps requires choosing users to demote or deactivate.</summary>
public sealed class LimitReductionRequiredDetails
{
    public int ActiveUserCount { get; init; }
    public int? ProposedMaxUserAccounts { get; init; }
    public int AdminCount { get; init; }
    public int? ProposedMaxAdminAccounts { get; init; }
    public int MinimumUsersToDeactivate { get; init; }
    public int MinimumAdminsToDemote { get; init; }
    public string BusinessId { get; init; } = "";
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
    public IReadOnlyList<string>? InitialAdminInviteEmails { get; init; }
    public int ActiveUserCount { get; init; }
    public int AdminCount { get; init; }
}
