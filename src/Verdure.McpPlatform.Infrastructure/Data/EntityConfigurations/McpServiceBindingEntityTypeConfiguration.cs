using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for McpServiceBinding
/// </summary>
public class McpServiceBindingEntityTypeConfiguration : IEntityTypeConfiguration<McpServiceBinding>
{
    public void Configure(EntityTypeBuilder<McpServiceBinding> builder)
    {
        builder.ToTable("mcp_service_bindings");

        builder.HasKey(b => b.Id);

        // Configure Id as string (Guid Version 7)
        builder.Property(b => b.Id)
            .HasMaxLength(36)
            .IsRequired();

        // Configure foreign key as string
        builder.Property(b => b.XiaozhiMcpEndpointId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(b => b.McpServiceConfigId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        // Configure SelectedToolNames as a JSON column or separate table
        builder.Property<string>("_selectedToolNamesJson")
            .HasColumnName("SelectedToolNames")
            .HasMaxLength(4000);

        // Use backing field for collection
        builder.Ignore(b => b.SelectedToolNames);

        builder.HasIndex(b => b.XiaozhiMcpEndpointId);
        builder.HasIndex(b => b.McpServiceConfigId);
        builder.HasIndex(b => b.IsActive);
    }
}
