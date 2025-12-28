using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snitcher.Core.Entities;

namespace Snitcher.Repository.Configurations;

/// <summary>
/// Entity Framework configuration for the ProjectEntity entity.
/// </summary>
public class ProjectEntityConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    /// <summary>
    /// Configures the ProjectEntity entity.
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        // Table configuration
        builder.ToTable("Projects");
        
        // Primary key
        builder.HasKey(p => p.Id);
        
        // Property configurations
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(p => p.Description)
            .HasMaxLength(1000);
            
        builder.Property(p => p.Path)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(p => p.Version)
            .HasMaxLength(50);
            
        builder.Property(p => p.WorkspaceId)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(p => new { p.WorkspaceId, p.Name })
            .IsUnique()
            .HasDatabaseName("IX_Projects_WorkspaceId_Name");
            
        builder.HasIndex(p => p.Path)
            .HasDatabaseName("IX_Projects_Path");
        
        // Relationships
        builder.HasOne(p => p.Workspace)
            .WithMany(w => w.Projects)
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
