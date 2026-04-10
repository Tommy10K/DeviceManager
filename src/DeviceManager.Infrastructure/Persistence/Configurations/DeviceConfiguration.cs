using DeviceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeviceManager.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(device => device.Id);

        builder.Property(device => device.Tag)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(device => device.Tag)
            .IsUnique()
            .HasDatabaseName("UQ_Device_Tag");

        builder.Property(device => device.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(device => device.Manufacturer)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(device => device.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(device => device.OperatingSystem)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(device => device.OSVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(device => device.Processor)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(device => device.RamAmount)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(device => device.Description)
            .HasMaxLength(1000);

        builder.Property(device => device.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(device => device.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(device => device.AssignedUser)
            .WithMany(user => user.AssignedDevices)
            .HasForeignKey(device => device.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}