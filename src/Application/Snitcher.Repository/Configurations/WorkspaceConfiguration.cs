using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snitcher.Core.Entities;

namespace Snitcher.Repository.Configurations;

/// <summary>
/// Entity Framework configuration for the Workspace entity.
/// </summary>
public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    /// <summary>
    /// Configures the Workspace entity.
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        // Table configuration
        builder.ToTable("Workspaces");
        
        // Primary key
        builder.HasKey(w => w.Id);
        
        // Property configurations
        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(w => w.Description)
            .HasMaxLength(1000);
            
        builder.Property(w => w.Path)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(w => w.Version)
            .HasMaxLength(50);
            
        builder.Property(w => w.IsDefault)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(w => w.Name)
            .IsUnique()
            .HasDatabaseName("IX_Workspaces_Name");
            
        builder.HasIndex(w => w.IsDefault)
            .HasDatabaseName("IX_Workspaces_IsDefault");
        
        // Relationships - Fixed to use ProjectEntity
        builder.HasMany(w => w.Projects)
            .WithOne(p => p.Workspace)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Query filter for soft delete
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
