using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaWallet.Application.Interfaces;
using NovaWallet.Application.Options;
using NovaWallet.Application.Services;

namespace NovaWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WalletOptions>()
            .Bind(configuration.GetSection(WalletOptions.SectionName));
        services.AddScoped<IWalletService, WalletService>();

        return services;
    }
}
