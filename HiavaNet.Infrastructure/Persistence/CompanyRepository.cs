using HiavaNet.Application.Company;
using HiavaNet.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace HiavaNet.Infrastructure.Persistence;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
        return company;
    }

    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<Company?> GetByBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return Task.FromResult<Company?>(null);
        return _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == businessId.Trim().ToLower(), cancellationToken);
    }

    public Task<Company?> GetByBusinessIdWithAddressBooksAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return Task.FromResult<Company?>(null);
        return _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .FirstOrDefaultAsync(c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == businessId.Trim().ToLower(), cancellationToken);
    }

    public Task<Company?> GetByIdWithAddressBooksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Company>> GetAllWithAddressBooksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Companies
            .AsNoTracking()
            .Include(c => c.SenderAddressBook)
            .Include(c => c.AddressBook)
            .OrderBy(c => c.Name ?? c.CompanyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Company>> GetAllAsync(CancellationToken cancellationToken = default)
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
