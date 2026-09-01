using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IHRRepository
{
    Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByEmailAcrossAllTenantsAsync(string email, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> ListByDepartmentAsync(string tenantId, string departmentId, CancellationToken cancellationToken = default);
}

public sealed class HRRepository(OrganizationDbContext db) : IHRRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public Task<Employee?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Employee?> GetByEmailAsync(string tenantId, string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return _db.Employees.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetByEmailAcrossAllTenantsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return await _db.Employees.AsNoTracking()
            .Where(x => x.Email.ToLower() == normalized)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _db.Employees.Update(employee);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Employee>> ListAsync(string tenantId, CancellationToken cancellationToken = default) =>
        _db.Employees.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<Employee>)t.Result, cancellationToken);

    public Task<IReadOnlyList<Employee>> ListByDepartmentAsync(string tenantId, string departmentId, CancellationToken cancellationToken = default) =>
        _db.Employees.AsNoTracking().Where(x => x.TenantId == tenantId && x.DepartmentId == departmentId).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<Employee>)t.Result, cancellationToken);
}
