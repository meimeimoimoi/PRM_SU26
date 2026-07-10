using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDine.Domain.Entities;

namespace SmartDine.Infrastructure.Persistence.Configurations;

public class RestaurantSettingsConfiguration : IEntityTypeConfiguration<RestaurantSettings>
{
    public void Configure(EntityTypeBuilder<RestaurantSettings> builder)
    {
        builder.ToTable("restaurant_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RestaurantName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.OpeningTime).HasColumnType("time");
        builder.Property(s => s.ClosingTime).HasColumnType("time");
        builder.Property(s => s.TaxRate).HasPrecision(5, 2);
        builder.Property(s => s.ServiceChargeRate).HasPrecision(5, 2);
    }
}
