using Atad.Application.Interfaces;
using Atad.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Atad.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Migrator
        services.AddScoped<IMigratorService, MigratorService>();

        return services;
    }
}
