using Microsoft.EntityFrameworkCore;
using Snitcher.Core.Entities;
using Snitcher.Core.Interfaces;
using Snitcher.Repository.Configurations;

namespace Snitcher.Repository.Contexts;

/// <summary>
/// Entity Framework Core DbContext for the Snitcher application.
/// Manages the connection to SQLite database and entity configurations.
/// Simplified architecture with only Workspaces and Projects tables.
/// </summary>
public class SnitcherDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Gets or sets the Workspaces DbSet.
    /// </summary>
    public DbSet<Workspace> Workspaces { get; set; }
    
    /// <summary>
    /// Gets or sets the Projects DbSet.
    /// </summary>
    public DbSet<ProjectEntity> Projects { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the SnitcherDbContext class.
    /// </summary>
    /// <param name="options">The DbContext options</param>
    public SnitcherDbContext(DbContextOptions<SnitcherDbContext> options) : base(options)
    {
    }
    
    /// <summary>
    /// Configures the database context and applies entity configurations.
    /// </summary>
    /// <param name="modelBuilder">The model builder for configuring entities</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new WorkspaceConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectEntityConfiguration());
        
        // Configure query filters for soft delete
        ConfigureSoftDeleteFilters(modelBuilder);
        
        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);
    }
    
    /// <summary>
    /// Configures the database connection and SQLite-specific options.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for configuring the context</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Default configuration for development
            // In production, this should be configured through dependency injection
            var databasePath = GetDefaultDatabasePath();
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
            
            // Enable sensitive data logging in development
            #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
            #endif
        }
        
        base.OnConfiguring(optionsBuilder);
    }
    
    /// <summary>
    /// Saves changes to the database with automatic timestamp updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected entities</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction == null)
        {
            await Database.BeginTransactionAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction != null)
        {
            await Database.RollbackTransactionAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Configures global query filters for soft delete functionality.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workspace>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ProjectEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
    
    /// <summary>
    /// Configures database indexes for improved query performance.
    /// Note: Most indexes are now configured in individual entity configurations.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Additional global indexes can be configured here
        // Most indexes are now handled in individual entity configurations
    }
    
    /// <summary>
    /// Updates timestamps for modified entities before saving.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedAt).CurrentValue = DateTime.UtcNow;
                entry.Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedAt).CurrentValue = DateTime.UtcNow;
                
                // Don't update CreatedAt on modification
                entry.Property(e => e.CreatedAt).IsModified = false;
            }
        }
    }
    
    /// <summary>
    /// Gets the default database file path for SQLite.
    /// Places the database in the user's AppData directory.
    /// </summary>
    /// <returns>The full path to the SQLite database file</returns>
    private static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "Snitcher");
        var databasePath = Path.Combine(appFolder, "snitcher.db");
        
        // Ensure the directory exists
        Directory.CreateDirectory(appFolder);
        
        return databasePath;
    }
}
