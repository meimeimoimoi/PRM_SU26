using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDine.Domain.Entities;

namespace SmartDine.Infrastructure.Persistence.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("tables");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.QrCode).HasMaxLength(500);
        builder.Property(t => t.Location).HasMaxLength(100);

        builder.HasIndex(t => t.TableNumber).IsUnique();
        builder.HasIndex(t => t.Status);
    }
}

public class DiningSessionConfiguration : IEntityTypeConfiguration<DiningSession>
{
    public void Configure(EntityTypeBuilder<DiningSession> builder)
    {
        builder.ToTable("dining_sessions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.TotalAmount).HasPrecision(18, 2);

        builder.HasIndex(d => d.IsActive);
        builder.HasIndex(d => d.TableId);

        builder.HasOne(d => d.Customer).WithMany(c => c.DiningSessions).HasForeignKey(d => d.CustomerId);
        builder.HasOne(d => d.Table).WithMany(t => t.DiningSessions).HasForeignKey(d => d.TableId);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        builder.Property(oi => oi.SpecialInstructions).HasMaxLength(500);

        builder.HasOne(oi => oi.MenuItem).WithMany(m => m.OrderItems).HasForeignKey(oi => oi.MenuItemId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TransactionRef).HasMaxLength(200);

        builder.HasIndex(p => p.OrderId);
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.ManagerReply).HasMaxLength(2000);

        builder.HasIndex(r => r.MenuItemId);

        builder.HasOne(r => r.Customer).WithMany(c => c.Reviews).HasForeignKey(r => r.CustomerId);
        builder.HasOne(r => r.MenuItem).WithMany(m => m.Reviews).HasForeignKey(r => r.MenuItemId);
    }
}

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("loyalty_accounts");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Tier).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => l.CustomerId).IsUnique();
        builder.HasOne(l => l.Customer).WithOne(c => c.LoyaltyAccount).HasForeignKey<LoyaltyAccount>(l => l.CustomerId);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.DiscountValue).HasPrecision(18, 2);
        builder.Property(p => p.MinOrderAmount).HasPrecision(18, 2);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Data).HasMaxLength(4000);

        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
    }
}

public class CustomerActivityConfiguration : IEntityTypeConfiguration<CustomerActivity>
{
    public void Configure(EntityTypeBuilder<CustomerActivity> builder)
    {
        builder.ToTable("customer_activities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActivityType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Metadata).HasMaxLength(4000);

        builder.HasIndex(a => a.CustomerId);
        builder.HasOne(a => a.Customer).WithMany(c => c.Activities).HasForeignKey(a => a.CustomerId);
        builder.HasOne(a => a.MenuItem).WithMany().HasForeignKey(a => a.MenuItemId);
    }
}

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("loyalty_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Type).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasOne(t => t.LoyaltyAccount).WithMany(l => l.Transactions).HasForeignKey(t => t.LoyaltyAccountId);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.PhoneNumber).HasMaxLength(20);
        builder.Property(c => c.DietaryPreferences).HasMaxLength(500);
        builder.Property(c => c.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(c => c.UserId).IsUnique();
    }
}
