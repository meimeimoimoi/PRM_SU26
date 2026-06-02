using Microsoft.EntityFrameworkCore;
using SmartDine.Domain.Entities;

namespace SmartDine.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho SmartDine — kết nối PostgreSQL.
/// </summary>
public class SmartDineDbContext : DbContext
{
    public SmartDineDbContext(DbContextOptions<SmartDineDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply tất cả Fluent API configurations từ assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartDineDbContext).Assembly);

        // Global query filter cho soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MenuItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MenuCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Table>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DiningSession>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Review>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LoyaltyAccount>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LoyaltyTransaction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Promotion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerActivity>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Tự động set CreatedAt/UpdatedAt khi save.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
