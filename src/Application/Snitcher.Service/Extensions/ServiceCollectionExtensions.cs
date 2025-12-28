using Microsoft.Extensions.DependencyInjection;
using Snitcher.Core.Interfaces;
using Snitcher.Service.Interfaces;
using Snitcher.Service.Services;
using Snitcher.Repository.Extensions;

namespace Snitcher.Service.Extensions;

/// <summary>
/// Extension methods for registering service layer with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the service layer to the service collection.
    /// Registers all service interfaces and implementations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        // Register service interfaces and implementations
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IProjectService, ProjectService>();
        
        return services;
    }

    /// <summary>
    /// Adds the service layer with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddServiceLayerScoped(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        
        return services;
    }

    /// <summary>
    /// Adds the service layer with transient lifetime.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddServiceLayerTransient(this IServiceCollection services)
    {
        services.AddTransient<IProjectService, ProjectService>();

        return services;
    }

    /// <summary>
    /// Adds the service layer with singleton lifetime.
    /// Use with caution as services maintain state.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddServiceLayerSingleton(this IServiceCollection services)
    {
        services.AddSingleton<IProjectService, ProjectService>();

        return services;
    }

    /// <summary>
    /// Adds the complete application stack (Repository + Service layers).
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">Optional database connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddApplicationStack(
        this IServiceCollection services, 
        string? connectionString = null)
    {
        // Add repository layer
        services.AddRepositoryLayer(connectionString);

        // Add service layer
        services.AddServiceLayer();

        return services;
    }

    /// <summary>
    /// Adds the complete application stack with custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureDbContext">Action to configure the DbContext</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddApplicationStack(
        this IServiceCollection services,
        Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configureDbContext)
    {
        // Add repository layer
        services.AddRepositoryLayer(configureDbContext);

        // Add service layer
        services.AddServiceLayer();

        return services;
    }

    /// <summary>
    /// Adds the complete application stack with in-memory database for testing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The in-memory database name</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddApplicationStackInMemory(
        this IServiceCollection services,
        string databaseName = "SnitcherTestDb")
    {
        // Add repository layer
        services.AddRepositoryLayerInMemory(databaseName);

        // Add service layer
        services.AddServiceLayer();

        return services;
    }

    /// <summary>
    /// Adds the complete application stack with SQLite database.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databasePath">Optional custom database file path</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddApplicationStackWithSqlite(
        this IServiceCollection services,
        string? databasePath = null)
    {
        // Add repository layer with SQLite
        services.UseSqliteDatabase(databasePath);

        // Register repositories
        services.AddScoped<Core.Interfaces.IProjectRepository, Repository.Repositories.ProjectRepository>();
        services.AddScoped(typeof(Core.Interfaces.IRepository<>), typeof(Repository.Repositories.EfRepository<>));
        services.AddScoped(typeof(Core.Interfaces.IRepository<,>), typeof(Repository.Repositories.EfRepository<,>));
        services.AddScoped<Core.Interfaces.IUnitOfWork, Repository.Repositories.UnitOfWork>();

        // Add service layer
        services.AddServiceLayer();

        return services;
    }
}
