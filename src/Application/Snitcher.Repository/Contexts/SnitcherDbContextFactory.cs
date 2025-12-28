using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Snitcher.Repository.Contexts;

/// <summary>
/// Factory for creating SnitcherDbContext at design time for migrations.
/// </summary>
public class SnitcherDbContextFactory : IDesignTimeDbContextFactory<SnitcherDbContext>
{
    /// <summary>
    /// Creates a SnitcherDbContext instance for design-time operations.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>A configured SnitcherDbContext instance</returns>
    public SnitcherDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SnitcherDbContext>();
        
        // Use SQLite for design-time operations
        var connectionString = "Data Source=snitcher_design.db";
        optionsBuilder.UseSqlite(connectionString);

        return new SnitcherDbContext(optionsBuilder.Options);
    }
}
