using Atad.Application.Interfaces;
using Atad.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Atad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string databaseName)
    {
        // MongoDB
        services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
        services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        // Repositories
        services.AddScoped<ITodoListRepository, TodoListRepository>();
        services.AddScoped<ITodoRepository, TodoRepository>();

        return services;
    }
}