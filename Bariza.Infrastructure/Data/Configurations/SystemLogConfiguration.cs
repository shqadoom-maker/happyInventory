using Bariza.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bariza.Infrastructure.Data.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Level)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(log => log.MessageTemplate)
            .HasMaxLength(4000);

        builder.Property(log => log.Exception)
            .HasMaxLength(8000);

        builder.Property(log => log.SourceContext)
            .HasMaxLength(256);

        builder.Property(log => log.UserId)
            .HasMaxLength(64);

        builder.Property(log => log.UserName)
            .HasMaxLength(120);

        builder.Property(log => log.RequestPath)
            .HasMaxLength(512);

        builder.Property(log => log.RequestMethod)
            .HasMaxLength(16);

        builder.Property(log => log.TraceId)
            .HasMaxLength(64);

        builder.Property(log => log.Properties)
            .HasMaxLength(8000);

        // The log viewer always orders by time and commonly filters by level.
        builder.HasIndex(log => log.TimestampUtc);
        builder.HasIndex(log => new { log.Level, log.TimestampUtc });
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.SourceContext);
    }
}
