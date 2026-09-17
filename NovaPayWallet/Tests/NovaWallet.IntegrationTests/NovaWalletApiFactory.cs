using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaWallet.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace NovaWallet.IntegrationTests;

/// <summary>
/// Spins up a real Postgres container (via Testcontainers) and boots the API in-memory through
/// <see cref="WebApplicationFactory{TEntryPoint}"/>. A real Postgres instance is required because the
/// wallet/transfer concurrency guarantees rely on Postgres row locking (SELECT ... FOR UPDATE).
/// </summary>
public class NovaWalletApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("novawallet")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NovaWalletDb"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Force the host to build (and thus run the EnsureCreatedAsync schema bootstrap in Program.cs)
        // against the now-running container.
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NovaWalletDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
