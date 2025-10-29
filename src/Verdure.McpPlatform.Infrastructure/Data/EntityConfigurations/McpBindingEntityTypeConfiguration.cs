using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for McpBinding
/// </summary>
public class McpBindingEntityTypeConfiguration : IEntityTypeConfiguration<McpBinding>
{
    public void Configure(EntityTypeBuilder<McpBinding> builder)
    {
        builder.ToTable("mcp_bindings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ServiceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.NodeAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasIndex(b => b.McpServerId);
        builder.HasIndex(b => b.IsActive);
    }
}
