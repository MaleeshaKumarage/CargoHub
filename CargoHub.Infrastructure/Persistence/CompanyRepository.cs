using CargoHub.Application.Company;
using CargoHub.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Infrastructure.Persistence;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CompanyEntity> CreateAsync(CompanyEntity company, CancellationToken cancellationToken = default)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
        return company;
    }

    public Task<CompanyEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<CompanyEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<CompanyEntity> UpdateAsync(CompanyEntity company, CancellationToken cancellationToken = default)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync(cancellationToken);
        return company;
    }

    public Task<CompanyEntity?> GetByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return Task.FromResult<CompanyEntity?>(null);
        return _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == businessId.Trim().ToLower(), cancellationToken);
    }

    public Task<CompanyEntity?> GetByBusinessIdWithAddressBooksAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return Task.FromResult<CompanyEntity?>(null);
        return _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .FirstOrDefaultAsync(c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == businessId.Trim().ToLower(), cancellationToken);
    }

    public Task<CompanyEntity?> GetByIdWithAddressBooksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<CompanyEntity>> GetAllWithAddressBooksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .OrderBy(c => c.Name ?? c.CompanyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CompanyEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Companies.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddSenderAsync(Guid companyId, CompanyAddress address, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .Include(c => c.SenderAddressBook)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null) return;
        var newEntry = CloneAddress(address);
        newEntry.Id = Guid.Empty;
        company.SenderAddressBook.Add(newEntry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddReceiverAsync(Guid companyId, CompanyAddress address, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .Include(c => c.AddressBook)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null) return;
        var newEntry = CloneAddress(address);
        newEntry.Id = Guid.Empty;
        company.AddressBook.Add(newEntry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<CompanyEntity?> GetByBusinessIdWithAgreementsAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return Task.FromResult<CompanyEntity?>(null);
        return _db.Companies
            .AsNoTracking()
            .Include(c => c.AgreementNumbers)
            .FirstOrDefaultAsync(c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == businessId.Trim().ToLower(), cancellationToken);
    }

    public async Task<HashSet<string>> GetEnabledCourierIdsForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .Include(c => c.AgreementNumbers)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company?.AgreementNumbers == null || company.AgreementNumbers.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in company.AgreementNumbers)
        {
            if (string.IsNullOrWhiteSpace(a.PostalService) || string.IsNullOrWhiteSpace(a.Number))
                continue;
            set.Add(a.PostalService.Trim());
        }

        return set;
    }

    public async Task ReplaceAgreementNumbersAsync(Guid companyId, IReadOnlyList<AgreementNumber> agreements, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .Include(c => c.AgreementNumbers)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null) return;
        company.AgreementNumbers.Clear();
        foreach (var a in agreements)
        {
            company.AgreementNumbers.Add(new AgreementNumber
            {
                PostalService = a.PostalService,
                Service = a.Service,
                Number = a.Number,
                Counter = a.Counter
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static CompanyAddress CloneAddress(CompanyAddress a)
    {
        return new CompanyAddress
        {
            Name = a.Name,
            Address1 = a.Address1,
            Address2 = a.Address2,
            PostalCode = a.PostalCode,
            City = a.City,
            Country = a.Country,
            PhoneNumber = a.PhoneNumber,
            PhoneNumberMobile = a.PhoneNumberMobile,
            ContactPersonName = a.ContactPersonName,
            Email = a.Email,
            County = a.County,
            VatNo = a.VatNo,
            CustomerNumber = a.CustomerNumber
        };
    }
}
