using Microsoft.EntityFrameworkCore;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext cho Module Promotions
    /// Database: PLPSOFT_ERP_SAAS_V2026
    /// Schema: promotions
    /// </summary>
    public class PromotionDbContext : DbContext
    {
        public PromotionDbContext(DbContextOptions<PromotionDbContext> options) : base(options)
        {
        }

        // ═══════════════════════════════════════════════════════════
        // DbSets
        // ═══════════════════════════════════════════════════════════
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<PromotionRule> PromotionRules { get; set; } = null!;
        public DbSet<PromotionProduct> PromotionProducts { get; set; } = null!;

        /// <summary>
        /// Để tra TypeValueID từ TypeCode + ValueCode (lấy ValueCode từ TypeValueID).
        /// Schema: dbo (ngoài promotions)
        /// </summary>
        public DbSet<SystemTypeValue> SystemTypeValues { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<CustomerGroup> CustomerGroups { get; set; } = null!;
        public DbSet<ProductCategory> ProductCategories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═══════════════════════════════════════════════════════════
            // DEFAULT SCHEMA
            // ═══════════════════════════════════════════════════════════
            modelBuilder.HasDefaultSchema("promotions");

            // ═══════════════════════════════════════════════════════════
            // PROMOTION
            // ═══════════════════════════════════════════════════════════
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");

                entity.HasKey(e => e.PromotionId);

                // Column mapping
                entity.Property(e => e.PromotionId)
                    .HasColumnName("PromotionID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CompanyId)
                    .HasColumnName("CompanyID")
                    .IsRequired();

                entity.Property(e => e.BranchId)
                    .HasColumnName("BranchID");

                entity.Property(e => e.PromotionCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PromotionName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.PromotionTypeId)
                    .HasColumnName("PromotionTypeID")
                    .IsRequired();

                entity.Property(e => e.PromotionStatusId)
                    .HasColumnName("PromotionStatusID")
                    .IsRequired();

                entity.Property(e => e.StartDate)
                    .HasColumnType("datetime2(0)")
                    .IsRequired();

                entity.Property(e => e.EndDate)
                    .HasColumnType("datetime2(0)");

                entity.Property(e => e.MaxUsage);

                entity.Property(e => e.CurrentUsage)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.Priority)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.IsStackable)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedByUserId)
                    .HasColumnName("CreatedByUserID")
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime2(0)")
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                entity.Property(e => e.Note)
                    .HasMaxLength(500);

                // Navigation
                entity.HasMany(e => e.PromotionRules)
                    .WithOne(r => r.Promotion)
                    .HasForeignKey(r => r.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.PromotionProducts)
                    .WithOne(pp => pp.Promotion)
                    .HasForeignKey(pp => pp.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: (CompanyID, PromotionCode)
                entity.HasIndex(e => new { e.CompanyId, e.PromotionCode })
                    .IsUnique()
                    .HasDatabaseName("UX_Promotions_CompanyID_PromotionCode");
            });

            // ═══════════════════════════════════════════════════════════
            // PROMOTION RULE
            // ═══════════════════════════════════════════════════════════
            modelBuilder.Entity<PromotionRule>(entity =>
            {
                entity.ToTable("PromotionRules");

                entity.HasKey(e => e.RuleId);

                entity.Property(e => e.RuleId)
                    .HasColumnName("RuleID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.PromotionId)
                    .HasColumnName("PromotionID")
                    .IsRequired();

                entity.Property(e => e.RuleTypeId)
                    .HasColumnName("RuleTypeID")
                    .IsRequired();

                entity.Property(e => e.MinOrderAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.MinQuantity)
                    .HasColumnType("decimal(18,3)");

                entity.Property(e => e.CustomerGroupId)
                    .HasColumnName("CustomerGroupID");

                entity.Property(e => e.CategoryId)
                    .HasColumnName("CategoryID");

                entity.Property(e => e.DiscountTypeId)
                    .HasColumnName("DiscountTypeID")
                    .IsRequired();

                entity.Property(e => e.DiscountValue)
                    .HasColumnType("decimal(18,4)")
                    .IsRequired();

                entity.Property(e => e.MaxDiscountAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime2(0)")
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                // Foreign key
                entity.HasOne(e => e.Promotion)
                    .WithMany(p => p.PromotionRules)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ═══════════════════════════════════════════════════════════
            // PROMOTION PRODUCT
            // ═══════════════════════════════════════════════════════════
            modelBuilder.Entity<PromotionProduct>(entity =>
            {
                entity.ToTable("PromotionProducts");

                entity.HasKey(e => e.PromotionProductId);

                entity.Property(e => e.PromotionProductId)
                    .HasColumnName("PromotionProductID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.PromotionId)
                    .HasColumnName("PromotionID")
                    .IsRequired();

                entity.Property(e => e.ProductId)
                    .HasColumnName("ProductID")
                    .IsRequired();

                entity.Property(e => e.RequiredQuantity)
                    .HasColumnType("decimal(18,3)");

                entity.Property(e => e.FreeQuantity)
                    .HasColumnType("decimal(18,3)");

                entity.Property(e => e.IsGiftProduct)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime2(0)")
                    .IsRequired()
                    .HasDefaultValueSql("SYSDATETIME()");

                // Foreign key
                entity.HasOne(e => e.Promotion)
                    .WithMany(p => p.PromotionProducts)
                    .HasForeignKey(e => e.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ═══════════════════════════════════════════════════════════
            // SYSTEM TYPE VALUE (dbo schema, chỉ read)
            // ═══════════════════════════════════════════════════════════
            modelBuilder.Entity<SystemTypeValue>(entity =>
            {
                entity.ToTable("SystemTypeValues", "dbo");
                entity.HasNoKey();
            });

            modelBuilder.Entity<Branch>(entity =>
            {
                entity.ToTable("Branches", "dbo");  
                entity.HasKey(e => e.BranchId);
                entity.Property(e => e.BranchId).HasColumnName("BranchID");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyID");
                entity.Property(e => e.BranchName).HasColumnName("BranchName");
                entity.Property(e => e.BranchCode).HasColumnName("BranchCode");
            });

            modelBuilder.Entity<CustomerGroup>(entity =>
            {
                entity.ToTable("CustomerGroups", "dbo");
                entity.HasKey(e => e.CustomerGroupId);
                entity.Property(e => e.CustomerGroupId).HasColumnName("CustomerGroupID");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyID");
                entity.Property(e => e.GroupCode).HasColumnName("GroupCode");
                entity.Property(e => e.GroupName).HasColumnName("GroupName");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.ToTable("ProductCategories", "dbo");
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyID");
                entity.Property(e => e.CategoryCode).HasColumnName("CategoryCode");
                entity.Property(e => e.CategoryName).HasColumnName("CategoryName");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products", "dbo");
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyID");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.ProductCode).HasColumnName("ProductCode");
                entity.Property(e => e.ProductName).HasColumnName("ProductName");
                entity.Property(e => e.StandardPrice).HasColumnName("StandardPrice");
            });
        }
    }

    /// <summary>
    /// Read-only entity để tra SystemTypeValues từ dbo schema
    /// </summary>
    public class SystemTypeValue
    {
        public long TypeValueID { get; set; }
        public long TypeID { get; set; }
        public string ValueCode { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
    }
}