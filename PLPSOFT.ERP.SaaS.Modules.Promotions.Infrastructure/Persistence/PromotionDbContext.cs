using Microsoft.EntityFrameworkCore;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence;

public class PromotionDbContext : DbContext
{
    public PromotionDbContext(DbContextOptions<PromotionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Promotion> Promotions { get; set; }

    public DbSet<PromotionRule> PromotionRules { get; set; }

    public DbSet<PromotionProduct> PromotionProducts { get; set; }
}