using Microsoft.EntityFrameworkCore;

namespace OnePage.Platform;

public interface IPayrollRepository
{
    Task<PayrollRecord> CreateAsync(PayrollRecord payroll, CancellationToken cancellationToken = default);
    Task<PayrollRecord?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateAsync(PayrollRecord payroll, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollRecord>> ListAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollRecord>> ListByEmployeeAsync(string tenantId, string employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollRecord>> ListByPeriodAsync(string tenantId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPayrollAsync(string tenantId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default);
}

public sealed class PayrollRepository(OrganizationDbContext db) : IPayrollRepository
{
    private readonly OrganizationDbContext _db = db;

    public async Task<PayrollRecord> CreateAsync(PayrollRecord payroll, CancellationToken cancellationToken = default)
    {
        _db.PayrollRecords.Add(payroll);
        await _db.SaveChangesAsync(cancellationToken);
        return payroll;
    }

    public Task<PayrollRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        _db.PayrollRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task UpdateAsync(PayrollRecord payroll, CancellationToken cancellationToken = default)
    {
        _db.PayrollRecords.Update(payroll);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollRecord>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var records = await _db.PayrollRecords.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        return records.OrderByDescending(x => x.PeriodStart).ToList();
    }

    public async Task<IReadOnlyList<PayrollRecord>> ListByEmployeeAsync(string tenantId, string employeeId, CancellationToken cancellationToken = default)
    {
        var records = await _db.PayrollRecords.AsNoTracking().Where(x => x.TenantId == tenantId && x.EmployeeId == employeeId).ToListAsync(cancellationToken);
        return records.OrderByDescending(x => x.PeriodStart).ToList();
    }

    public async Task<IReadOnlyList<PayrollRecord>> ListByPeriodAsync(string tenantId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default)
    {
        var records = await _db.PayrollRecords.AsNoTracking().Where(x => x.TenantId == tenantId && x.PeriodStart >= periodStart && x.PeriodEnd <= periodEnd).ToListAsync(cancellationToken);
        return records.OrderByDescending(x => x.PeriodStart).ToList();
    }

    public async Task<decimal> GetTotalPayrollAsync(string tenantId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken = default)
    {
        var result = await _db.PayrollRecords
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PeriodStart >= periodStart && x.PeriodEnd <= periodEnd && x.Status == "paid")
            .SumAsync(x => x.Amount, cancellationToken);
        return result;
    }
}
