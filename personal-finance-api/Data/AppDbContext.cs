using Microsoft.EntityFrameworkCore;
using PersonalFinanceApi.Models;

namespace PersonalFinanceApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置User實體
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.PasswordHash)
                      .IsRequired();
                entity.Property(e => e.CreatedAt)
                      .IsRequired();
                
                // 建立唯一索引
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // 配置Transaction實體
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description)
                      .HasMaxLength(200);
                entity.Property(e => e.Date)
                      .IsRequired();
                entity.Property(e => e.Type)
                      .IsRequired()
                      .HasConversion<string>();
                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                // 配置外鍵關聯
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Transactions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Transactions)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置Category實體
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);
                entity.Property(e => e.Color)
                      .IsRequired()
                      .HasMaxLength(7)
                      .HasDefaultValue("#000000");
                entity.Property(e => e.Type)
                      .IsRequired()
                      .HasConversion<string>();

                // 配置外鍵關聯
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Categories)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 建立複合唯一索引（使用者不能有重複的分類名稱）
                entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            });
        }
    }
}