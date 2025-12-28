using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Contexts;
using Snitcher.Repository.Extensions;
using Snitcher.Service.Extensions;

namespace Snitcher.Service.Configuration;

/// <summary>
/// Configuration class for setting up the complete Snitcher application stack.
/// Provides centralized configuration for dependency injection and database setup.
/// </summary>
public static class SnitcherConfiguration
{
    /// <summary>
    /// Configures the complete Snitcher application with default settings.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection ConfigureSnitcher(this IServiceCollection services)
    {
        return services.ConfigureSnitcher(options => { });
    }

    /// <summary>
    /// Configures the complete Snitcher application with custom options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure Snitcher options</param>
    /// <returns>The configured service collection</returns>
    public static IServiceCollection ConfigureSnitcher(
        this IServiceCollection services,
        Action<SnitcherOptions> configureOptions)
    {
        var options = new SnitcherOptions();
        configureOptions?.Invoke(options);

        // Configure database based on options
        switch (options.DatabaseProvider.ToLowerInvariant())
        {
            case "sqlite":
                services.ConfigureSqliteDatabase(options.DatabasePath);
                break;
            case "inmemory":
                services.ConfigureInMemoryDatabase(options.DatabaseName);
                break;
            case "custom":
                if (options.ConfigureDbContext == null)
                    throw new ArgumentException("ConfigureDbContext must be provided when using custom database provider.");
                services.AddDbContext<SnitcherDbContext>(options.ConfigureDbContext);
                break;
            default:
                throw new ArgumentException($"Unsupported database provider: {options.DatabaseProvider}");
        }

        // Register repositories
        services.AddScoped<IWorkspaceRepository, Repository.Repositories.WorkspaceRepository>();
        services.AddScoped<IProjectRepository, Repository.Repositories.ProjectRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository.Repositories.EfRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository.Repositories.EfRepository<,>));
        services.AddScoped<IUnitOfWork, Repository.Repositories.UnitOfWork>();

        // Register services
        services.AddScoped<Service.Interfaces.IWorkspaceService, Service.Services.WorkspaceService>();
        services.AddScoped<Service.Interfaces.IProjectService, Service.Services.ProjectService>();

        // Register options for dependency injection
        services.AddSingleton(options);

        return services;
    }

    /// <summary>
    /// Configures SQLite database with the specified path.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databasePath">The database file path</param>
    /// <returns>The configured service collection</returns>
    private static IServiceCollection ConfigureSqliteDatabase(
        this IServiceCollection services,
        string? databasePath)
    {
        var connectionString = GetSqliteConnectionString(databasePath);
        services.AddDbContext<SnitcherDbContext>(options =>
            options.UseSqlite(connectionString));
        return services;
    }

    /// <summary>
    /// Configures in-memory database for testing.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="databaseName">The database name</param>
    /// <returns>The configured service collection</returns>
    private static IServiceCollection ConfigureInMemoryDatabase(
        this IServiceCollection services,
        string databaseName)
    {
        services.AddDbContext<SnitcherDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        return services;
    }

    /// <summary>
    /// Gets the SQLite connection string with proper path handling.
    /// </summary>
    /// <param name="databasePath">The database file path</param>
    /// <returns>The SQLite connection string</returns>
    private static string GetSqliteConnectionString(string? databasePath)
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

    /// <summary>
    /// Creates the database and applies migrations if needed.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="ensureCreated">Whether to ensure the database is created</param>
    /// <param name="applyMigrations">Whether to apply pending migrations</param>
    public static async Task InitializeDatabaseAsync(
        IServiceProvider serviceProvider,
        bool ensureCreated = true,
        bool applyMigrations = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SnitcherDbContext>();

        try
        {
            // Check if database exists and has tables
            var canConnect = await context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                // Database doesn't exist, create it
                await context.Database.EnsureCreatedAsync();
            }
            else if (applyMigrations)
            {
                // Database exists, apply any pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync();
                }
            }
        }
        catch (Exception ex)
        {
            // If migration fails, try to ensure created as fallback
            System.Diagnostics.Debug.WriteLine($"Migration failed: {ex.Message}. Attempting to ensure database is created.");
            
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize database: {fallbackEx.Message}");
                throw;
            }
        }
    }
}

/// <summary>
/// Configuration options for the Snitcher application.
/// </summary>
public class SnitcherOptions
{
    /// <summary>
    /// Gets or sets the database provider.
    /// Supported values: "sqlite", "inmemory", "custom"
    /// </summary>
    public string DatabaseProvider { get; set; } = "sqlite";

    /// <summary>
    /// Gets or sets the database path (for SQLite).
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Gets or sets the database name (for in-memory).
    /// </summary>
    public string DatabaseName { get; set; } = "SnitcherDb";

    /// <summary>
    /// Gets or sets the custom DbContext configuration action.
    /// Required when using custom database provider.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable sensitive data logging.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed errors.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Gets or sets the command timeout for database operations.
    /// </summary>
    public int? CommandTimeout { get; set; }
}
