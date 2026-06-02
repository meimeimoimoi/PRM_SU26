using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDine.Domain.Entities;

namespace SmartDine.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasOne(u => u.Customer)
               .WithOne(c => c.User)
               .HasForeignKey<Customer>(c => c.UserId);
    }
}
