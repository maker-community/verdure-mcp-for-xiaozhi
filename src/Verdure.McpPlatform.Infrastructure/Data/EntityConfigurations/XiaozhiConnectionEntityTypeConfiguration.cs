using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for XiaozhiConnection
/// </summary>
public class XiaozhiConnectionEntityTypeConfiguration : IEntityTypeConfiguration<XiaozhiConnection>
{
    public void Configure(EntityTypeBuilder<XiaozhiConnection> builder)
    {
        builder.ToTable("xiaozhi_connections");

        builder.HasKey(s => s.Id);

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
            .HasForeignKey(b => b.XiaozhiConnectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
