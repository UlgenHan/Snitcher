using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Snitcher.Core.Entities;

namespace Snitcher.Repository.Configurations;

/// <summary>
/// Entity Framework Core configuration for the ProjectNamespace entity.
/// Defines table schema, relationships, and constraints.
/// </summary>
public class ProjectNamespaceConfiguration : IEntityTypeConfiguration<ProjectNamespace>
{
    /// <summary>
    /// Configures the ProjectNamespace entity.
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<ProjectNamespace> builder)
    {
        // Table configuration
        builder.ToTable("ProjectNamespaces");
        
        // Primary key configuration
        builder.HasKey(n => n.Id);
        
        // Property configurations
        builder.Property(n => n.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(n => n.FullName)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(n => n.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(n => n.ProjectId)
            .IsRequired();
            
        builder.Property(n => n.ParentNamespaceId);
            
        builder.Property(n => n.Depth)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(n => n.CreatedAt)
            .IsRequired();
            
        builder.Property(n => n.UpdatedAt)
            .IsRequired();
            
        builder.Property(n => n.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(n => n.LastAnalyzedAt);
        
        // Relationship configurations
        builder.HasOne(n => n.Project)
            .WithMany(p => p.Namespaces)
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(n => n.ParentNamespace)
            .WithMany(n => n.ChildNamespaces)
            .HasForeignKey(n => n.ParentNamespaceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Unique constraints
        builder.HasIndex(n => new { n.ProjectId, n.FullName })
            .IsUnique()
            .HasDatabaseName("IX_ProjectNamespaces_ProjectId_FullName");
            
        builder.HasIndex(n => n.ParentNamespaceId)
            .HasDatabaseName("IX_ProjectNamespaces_ParentNamespaceId");
            
        builder.HasIndex(n => n.Depth)
            .HasDatabaseName("IX_ProjectNamespaces_Depth");
        
        // Query filter for soft delete
        builder.HasQueryFilter(n => !n.IsDeleted);
        
        // Check constraints
        builder.ToTable(table => table.HasCheckConstraint("CK_ProjectNamespaces_Depth_NonNegative", "Depth >= 0"));
    }
}
