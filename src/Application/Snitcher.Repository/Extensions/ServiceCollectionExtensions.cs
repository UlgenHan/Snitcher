using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;
using Snitcher.Repository.Repositories;

namespace Snitcher.Repository.Extensions;

/// <summary>
/// Extension methods for registering repository services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the repository layer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">Optional database connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddRepositoryLayer(
        this IServiceCollection services, 
        string? connectionString = null)
    {
        // Register DbContext
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Use default SQLite configuration
            services.AddDbContext<SnitcherDbContext>();
        }
        else
        {
            // Use provided connection string
            services.AddDbContext<SnitcherDbContext>(options =>
                options.UseSqlite(connectionString));
        }

        // Register repositories
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
                
        // Register generic repository for any future entities
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds the repository layer with custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureDbContext">Action to configure the DbContext</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddRepositoryLayer(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Register DbContext with custom configuration
        services.AddDbContext<SnitcherDbContext>(configureDbContext);

        // Register repositories
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
                
        // Register generic repository for any future entities
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds the repository layer with in-memory database for testing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The in-memory database name</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddRepositoryLayerInMemory(
        this IServiceCollection services,
        string databaseName = "SnitcherTestDb")
    {
        // Register in-memory DbContext for testing
        services.AddDbContext<SnitcherDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // Register repositories
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
                
        // Register generic repository for any future entities
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Configures SQLite database with proper file path handling.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databasePath">Optional custom database file path</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection UseSqliteDatabase(
        this IServiceCollection services,
        string? databasePath = null)
    {
        var connectionString = GetSqliteConnectionString(databasePath);
        
        services.AddDbContext<SnitcherDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    /// <summary>
    /// Gets the SQLite connection string with proper file path handling.
    /// </summary>
    /// <param name="databasePath">Optional custom database file path</param>
    /// <returns>The SQLite connection string</returns>
    private static string GetSqliteConnectionString(string? databasePath = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            // Use default path in user's AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "Snitcher");
            var dbPath = Path.Combine(appFolder, "snitcher.db");
            
            // Ensure directory exists
            Directory.CreateDirectory(appFolder);
            
            return $"Data Source={dbPath}";
        }
        else
        {
            // Use custom path
            var fullPath = Path.GetFullPath(databasePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            return $"Data Source={fullPath}";
        }
    }
}
