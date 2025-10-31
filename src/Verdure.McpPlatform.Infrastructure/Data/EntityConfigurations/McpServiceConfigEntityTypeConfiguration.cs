using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for McpServiceConfig
/// </summary>
public class McpServiceConfigEntityTypeConfiguration : IEntityTypeConfiguration<McpServiceConfig>
{
    public void Configure(EntityTypeBuilder<McpServiceConfig> builder)
    {
        builder.ToTable("mcp_service_configs");

        builder.HasKey(c => c.Id);

        // Configure Id as string (Guid Version 7)
        builder.Property(c => c.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Endpoint)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.AuthenticationType)
            .HasMaxLength(50);

        builder.Property(c => c.AuthenticationConfig)
            .HasMaxLength(2000);

        builder.Property(c => c.Protocol)
            .HasMaxLength(50)
            .HasDefaultValue("stdio");

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Configure relationship with Tools
        builder.HasMany(c => c.Tools)
            .WithOne()
            .HasForeignKey("McpServiceConfigId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IsPublic);
    }
}
