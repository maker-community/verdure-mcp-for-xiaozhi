using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for McpTool
/// </summary>
public class McpToolEntityTypeConfiguration : IEntityTypeConfiguration<McpTool>
{
    public void Configure(EntityTypeBuilder<McpTool> builder)
    {
        builder.ToTable("mcp_tools");

        builder.HasKey(t => t.Id);

        // Configure Id as string (Guid Version 7)
        builder.Property(t => t.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.McpServiceConfigId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(t => t.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.InputSchema)
            .HasMaxLength(4000);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.McpServiceConfigId);
        builder.HasIndex(t => t.Name);
        
        // UserId indexes for data isolation and query performance
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.UserId, t.McpServiceConfigId });
    }
}
