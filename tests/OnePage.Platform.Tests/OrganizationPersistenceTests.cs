using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OnePage.Platform;

namespace OnePage.Platform.Tests;

public sealed class OrganizationPersistenceTests : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private OrganizationDbContext db = null!;
    private OrganizationRepository repository = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OrganizationDbContext>().UseSqlite(connection).Options;
        db = new OrganizationDbContext(options);
        await OrganizationPersistence.InitializeAsync(db);
        repository = new OrganizationRepository(db, new TenantContextAccessor { Current = TenantContext.Create("user-1", "tenant-1", "corr-1") });
    }

    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    [Fact]
    public async Task Tenant_can_be_created_and_retrieved_by_stable_identifier()
    {
        var tenant = new Tenant("tenant-1", "Acme");
        await new TenantRepository(db).CreateAsync(tenant);
        var found = await new TenantRepository(db).GetAsync("tenant-1");
        Assert.NotNull(found);
        Assert.Equal("Acme", found.Name);
    }

    [Fact]
    public async Task Tenant_owned_records_are_isolated_by_context()
    {
        await new TenantRepository(db).CreateAsync(new Tenant("tenant-1", "Acme"));
        await new TenantRepository(db).CreateAsync(new Tenant("tenant-2", "Other"));
        await repository.CreateAsync(new Branch("branch-1", "tenant-1", "HQ"));
        var otherContext = new TenantContextAccessor { Current = TenantContext.Create("user-2", "tenant-2", "corr-2") };
        var otherRepository = new OrganizationRepository(db, otherContext);
        Assert.Null(await otherRepository.GetAsync<Branch>("branch-1"));
        await Assert.ThrowsAsync<TenantContextValidationException>(() => otherRepository.CreateAsync(new Branch("branch-2", "tenant-1", "Cross-tenant")));
    }

    [Fact]
    public async Task Organization_records_support_update_within_the_current_tenant()
    {
        var branch = await repository.CreateAsync(new Branch("branch-1", "tenant-1", "HQ"));
        branch.Rename("Lagos HQ");
        await repository.UpdateAsync(branch);
        Assert.Equal("Lagos HQ", (await repository.GetAsync<Branch>("branch-1"))!.Name);
    }

    [Fact]
    public async Task Memberships_are_scoped_and_support_active_state()
    {
        var membership = await repository.CreateMembershipAsync(new UserMembership("membership-1", "tenant-1", "user-2"));
        membership.SetActive(false);
        await db.SaveChangesAsync();
        var found = await repository.GetMembershipAsync("membership-1");
        Assert.NotNull(found);
        Assert.False(found.IsActive);
    }

    [Theory]
    [InlineData(null, "name", "Tenant ID is required.")]
    [InlineData("id", "", "Tenant name is required.")]
    public void Required_tenant_data_fails_fast(string? id, string name, string message)
    {
        var exception = Assert.Throws<OrganizationValidationException>(() => new Tenant(id!, name));
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task Clean_schema_initialization_is_reproducible()
    {
        await db.Database.EnsureDeletedAsync();
        await OrganizationPersistence.InitializeAsync(db);
        await new TenantRepository(db).CreateAsync(new Tenant("tenant-clean", "Clean"));
        Assert.NotNull(await new TenantRepository(db).GetAsync("tenant-clean"));
    }
}
