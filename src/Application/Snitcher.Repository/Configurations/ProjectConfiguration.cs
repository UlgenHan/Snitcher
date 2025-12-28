using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snitcher.Core.Entities;

namespace Snitcher.Repository.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Project entity.
/// Defines table schema, relationships, and constraints.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <summary>
    /// Configures the Project entity.
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // Table configuration
        builder.ToTable("Projects");
        
        // Primary key configuration
        builder.HasKey(p => p.Id);
        
        // Property configurations
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
            
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
            
        builder.Property(p => p.CreatedAt)
            .IsRequired();
            
        builder.Property(p => p.UpdatedAt)
            .IsRequired();
            
        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(p => p.LastAnalyzedAt);
        
        // Relationship configurations
        builder.HasMany(p => p.Namespaces)
            .WithOne(n => n.Project)
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Unique constraint
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Projects_Name");
            
        builder.HasIndex(p => p.Path)
            .HasDatabaseName("IX_Projects_Path");
        
        // Query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
