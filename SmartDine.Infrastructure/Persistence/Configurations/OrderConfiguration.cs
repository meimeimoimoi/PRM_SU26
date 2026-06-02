using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDine.Domain.Entities;

namespace SmartDine.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.SpecialInstructions).HasMaxLength(500);

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.TableId);
        builder.HasIndex(o => o.CreatedAt);

        builder.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId);
        builder.HasOne(o => o.Table).WithMany(t => t.Orders).HasForeignKey(o => o.TableId);
        builder.HasOne(o => o.DiningSession).WithMany(d => d.Orders).HasForeignKey(o => o.DiningSessionId);
        builder.HasOne(o => o.Promotion).WithMany(p => p.Orders).HasForeignKey(o => o.PromotionId);
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        builder.HasOne(o => o.Payment).WithOne(p => p.Order).HasForeignKey<Payment>(p => p.OrderId);
    }
}
