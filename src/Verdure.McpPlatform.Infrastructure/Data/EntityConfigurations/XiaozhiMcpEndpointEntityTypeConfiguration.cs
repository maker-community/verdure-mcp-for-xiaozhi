using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for XiaozhiMcpEndpoint
/// </summary>
public class XiaozhiMcpEndpointEntityTypeConfiguration : IEntityTypeConfiguration<XiaozhiMcpEndpoint>
{
    public void Configure(EntityTypeBuilder<XiaozhiMcpEndpoint> builder)
    {
        builder.ToTable("xiaozhi_mcp_endpoints");

        builder.HasKey(s => s.Id);

        // Configure Id as string (Guid Version 7)
        builder.Property(s => s.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.IsEnabled)
            .IsRequired();

        builder.Property(s => s.IsConnected)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.IsEnabled);

        builder.HasMany(s => s.ServiceBindings)
            .WithOne()
            .HasForeignKey(b => b.XiaozhiMcpEndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
