using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

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

        builder.Property(b => b.ServiceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.NodeAddress)
            .HasMaxLength(500)
            .IsRequired();

        // Configure foreign key as string
        builder.Property(b => b.XiaozhiConnectionId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasMaxLength(1000);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasIndex(b => b.XiaozhiConnectionId);
        builder.HasIndex(b => b.IsActive);
    }
}
