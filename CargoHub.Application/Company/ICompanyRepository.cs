using CargoHub.Domain.Companies;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using CompanyAddress = CargoHub.Domain.Companies.CompanyAddress;

namespace CargoHub.Application.Company;

/// <summary>
/// Company persistence. Implemented in Infrastructure.
/// </summary>
public interface ICompanyRepository
{
    Task<CompanyEntity> CreateAsync(CompanyEntity company, CancellationToken cancellationToken = default);
    Task<CompanyEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Tracked entity for updates.</summary>
    Task<CompanyEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyEntity> UpdateAsync(CompanyEntity company, CancellationToken cancellationToken = default);
    /// <summary>Get company by government/business ID (e.g. Y-tunnus). Used to validate registration.</summary>
    Task<CompanyEntity?> GetByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default);
    /// <summary>Get company by business ID with SenderAddressBook and AddressBook loaded (for Actions address book).</summary>
    Task<CompanyEntity?> GetByBusinessIdWithAddressBooksAsync(string businessId, CancellationToken cancellationToken = default);
    /// <summary>Get company by Id with SenderAddressBook and AddressBook loaded (for SuperAdmin address book by company).</summary>
    Task<CompanyEntity?> GetByIdWithAddressBooksAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Get all companies with address books (for SuperAdmin view-all).</summary>
    Task<List<CompanyEntity>> GetAllWithAddressBooksAsync(CancellationToken cancellationToken = default);
    /// <summary>Get all companies (for super admin company list).</summary>
    Task<List<CompanyEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>Add a sender to the company's SenderAddressBook. Company is loaded with tracking.</summary>
    Task AddSenderAsync(Guid companyId, CompanyAddress address, CancellationToken cancellationToken = default);
    /// <summary>Add a receiver to the company's AddressBook. Company is loaded with tracking.</summary>
    Task AddReceiverAsync(Guid companyId, CompanyAddress address, CancellationToken cancellationToken = default);

    /// <summary>Agreement numbers (courier contracts) for the company. No-tracking.</summary>
    Task<CompanyEntity?> GetByBusinessIdWithAgreementsAsync(string businessId, CancellationToken cancellationToken = default);

    /// <summary>Courier ids with a non-empty contract id (PostalService values). Empty if none.</summary>
    Task<HashSet<string>> GetEnabledCourierIdsForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Replace all agreement numbers for the company. Company must exist.</summary>
    Task ReplaceAgreementNumbersAsync(Guid companyId, IReadOnlyList<AgreementNumber> agreements, CancellationToken cancellationToken = default);
}
