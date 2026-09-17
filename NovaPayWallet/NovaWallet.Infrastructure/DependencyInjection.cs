using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaWallet.Infrastructure.Persistence;
using NovaWallet.Infrastructure.Repositories;

namespace NovaWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NovaWalletDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("NovaWalletDb")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();
        services.AddScoped<IDailyTransferUsageRepository, DailyTransferUsageRepository>();

        return services;
    }
}
