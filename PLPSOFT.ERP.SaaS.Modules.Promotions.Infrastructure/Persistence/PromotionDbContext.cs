using Microsoft.EntityFrameworkCore;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext riêng của Module Promotions.
    /// Chỉ quản lý 3 bảng trong schema "promotions", không đụng module khác.
    /// </summary>
    public class PromotionDbContext : DbContext
    {
        public PromotionDbContext(DbContextOptions<PromotionDbContext> options)
            : base(options) { }

        // ─── 3 BẢNG CHÍNH ────────────────────────────────────────────
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<PromotionRule> PromotionRules { get; set; } = null!;
        public DbSet<PromotionProduct> PromotionProducts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Bảng Promotions ──────────────────────────────────────
            modelBuilder.Entity<Promotion>(e =>
            {
                // Entity đã có [Table] attribute nhưng khai báo lại để chắc chắn
                e.ToTable("Promotions", schema: "promotions");

                // Khóa chính — Entity dùng PromotionId (chữ thường d)
                e.HasKey(x => x.PromotionId);

                // Map tên property C# → tên cột SQL
                e.Property(x => x.PromotionId).HasColumnName("PromotionID");
                e.Property(x => x.CompanyId).HasColumnName("CompanyID");
                e.Property(x => x.BranchId).HasColumnName("BranchID");
                e.Property(x => x.PromotionTypeId).HasColumnName("PromotionTypeID");
                e.Property(x => x.PromotionStatusId).HasColumnName("PromotionStatusID");
                e.Property(x => x.CreatedByUserId).HasColumnName("CreatedByUserID");

                e.Property(x => x.PromotionCode)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);       // VARCHAR trong SQL Server

                e.Property(x => x.PromotionName)
                    .IsRequired()
                    .HasMaxLength(255);      // NVARCHAR

                e.Property(x => x.Note)
                    .HasMaxLength(500);

                e.Property(x => x.CurrentUsage)
                    .HasDefaultValue(0);

                e.Property(x => x.IsStackable)
                    .HasDefaultValue(false);

                e.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                e.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSDATETIME()");

                // Unique: mỗi công ty không được trùng mã KM
                e.HasIndex(x => new { x.CompanyId, x.PromotionCode })
                    .IsUnique()
                    .HasDatabaseName("UQ_Promotions_Code");

                // Index tăng tốc lọc theo ngày
                e.HasIndex(x => new { x.StartDate, x.EndDate })
                    .HasDatabaseName("IX_Promotions_Dates");

                // Quan hệ 1-N với PromotionRules
                e.HasMany(x => x.PromotionRules)
                    .WithOne(r => r.Promotion)
                    .HasForeignKey(r => r.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ 1-N với PromotionProducts
                e.HasMany(x => x.PromotionProducts)
                    .WithOne(p => p.Promotion)
                    .HasForeignKey(p => p.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Bảng PromotionRules ──────────────────────────────────
            modelBuilder.Entity<PromotionRule>(e =>
            {
                e.ToTable("PromotionRules", schema: "promotions");
                e.HasKey(x => x.RuleId);

                // Map tên cột
                e.Property(x => x.RuleId).HasColumnName("RuleID");
                e.Property(x => x.PromotionId).HasColumnName("PromotionID");
                e.Property(x => x.RuleTypeId).HasColumnName("RuleTypeID");
                e.Property(x => x.CustomerGroupId).HasColumnName("CustomerGroupID");
                e.Property(x => x.CategoryId).HasColumnName("CategoryID");
                e.Property(x => x.DiscountTypeId).HasColumnName("DiscountTypeID");

                e.Property(x => x.DiscountValue)
                    .HasColumnType("decimal(18,4)");

                e.Property(x => x.MinOrderAmount)
                    .HasColumnType("decimal(18,2)");

                e.Property(x => x.MinQuantity)
                    .HasColumnType("decimal(18,3)");

                e.Property(x => x.MaxDiscountAmount)
                    .HasColumnType("decimal(18,2)");

                e.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSDATETIME()");
            });

            // ── Bảng PromotionProducts ───────────────────────────────
            modelBuilder.Entity<PromotionProduct>(e =>
            {
                e.ToTable("PromotionProducts", schema: "promotions");
                e.HasKey(x => x.PromotionProductId);

                // Map tên cột
                e.Property(x => x.PromotionProductId).HasColumnName("PromotionProductID");
                e.Property(x => x.PromotionId).HasColumnName("PromotionID");
                e.Property(x => x.ProductId).HasColumnName("ProductID");

                e.Property(x => x.RequiredQuantity)
                    .HasColumnType("decimal(18,3)");

                e.Property(x => x.FreeQuantity)
                    .HasColumnType("decimal(18,3)");

                e.Property(x => x.IsGiftProduct)
                    .HasDefaultValue(false);

                e.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSDATETIME()");
            });
        }
    }
}