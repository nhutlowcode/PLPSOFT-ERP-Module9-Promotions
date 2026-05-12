using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.DTOs;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Application.Interfaces;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Domain.Entities;
using PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Persistence;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Infrastructure.Repositories
{
    /// <summary>
    /// Triển khai IPromotionRepository.
    /// Xử lý tất cả truy vấn database cho KM.
    /// </summary>
    public class PromotionRepository : IPromotionRepository
    {
        private readonly PromotionDbContext _db;

        public PromotionRepository(PromotionDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════
        // PRIVATE HELPER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Query gốc: Include Rules & Products, AsNoTracking (chỉ đọc).
        /// </summary>
        private IQueryable<Promotion> BaseQuery()
            => _db.Promotions
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionProducts)
                .AsNoTracking();

        /// <summary>
        /// Lấy tất cả TypeValueID cần tra (TypeValueID → ValueCode).
        /// Query 1 lần duy nhất, build Dictionary để tránh N+1.
        /// </summary>
        private async Task<Dictionary<long, string>> GetTypeValueCodeMapAsync(
            params long[] typeValueIds)
        {
            if (!typeValueIds.Any()) return new Dictionary<long, string>();

            var map = await _db.SystemTypeValues
                .Where(stv => typeValueIds.Contains(stv.TypeValueID))
                .ToDictionaryAsync(stv => stv.TypeValueID, stv => stv.ValueCode);

            return map;
        }

        /// <summary>
        /// Tra TypeValueID từ TypeCode + ValueCode.
        /// </summary>
        private async Task<long> GetTypeValueIdAsync(string typeCode, string valueCode)
        {
            var result = await _db.SystemTypeValues
                .FromSqlInterpolated($@"
                    SELECT stv.TypeValueID, stv.TypeID, stv.ValueCode, stv.ValueName
                    FROM   dbo.SystemTypeValues stv
                    JOIN   dbo.SystemTypes      st  ON st.TypeID = stv.TypeID
                    WHERE  st.TypeCode   = {typeCode}
                    AND    stv.ValueCode = {valueCode}")
                .FirstOrDefaultAsync();

            return result?.TypeValueID ?? 0;
        }

        /// <summary>
        /// Map Entity Promotion → PromotionDto.
        /// </summary>
        private PromotionDto MapToDto(
            Promotion entity,
            Dictionary<long, string> typeValueCodeMap)
        {
            // Tra ValueCode từ map (đã query 1 lần)
            var promotionTypeCode = typeValueCodeMap.TryGetValue(entity.PromotionTypeId, out var tc)
                ? tc : string.Empty;
            var statusCode = typeValueCodeMap.TryGetValue(entity.PromotionStatusId, out var sc)
                ? sc : string.Empty;

            var dto = new PromotionDto
            {
                PromotionID = entity.PromotionId,
                BranchId = entity.BranchId,
                PromotionCode = entity.PromotionCode,
                PromotionName = entity.PromotionName,
                PromotionType = promotionTypeCode,
                Status = statusCode,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                MaxUsage = entity.MaxUsage,
                CurrentUsage = entity.CurrentUsage,
                IsStackable = entity.IsStackable,
                Priority = entity.Priority,
                Note = entity.Note,

                // Map Rules
                Rules = entity.PromotionRules.Select(r =>
                {
                    var ruleType = typeValueCodeMap.TryGetValue(r.RuleTypeId, out var rt)
                        ? rt : string.Empty;
                    var discountType = typeValueCodeMap.TryGetValue(r.DiscountTypeId, out var dt)
                        ? dt : string.Empty;

                    return new PromotionRuleDto
                    {
                        RuleID = r.RuleId,
                        PromotionID = r.PromotionId,
                        RuleType = ruleType,
                        MinOrderAmount = r.MinOrderAmount,
                        MinQuantity = r.MinQuantity,
                        CustomerGroupID = r.CustomerGroupId,
                        CategoryID = r.CategoryId,
                        DiscountType = discountType,
                        DiscountValue = r.DiscountValue,
                        MaxDiscountAmount = r.MaxDiscountAmount
                    };
                }).ToList(),

                // Map Products
                Products = entity.PromotionProducts.Select(pp => new PromotionProductDto
                {
                    ProductID = pp.ProductId,
                    RequiredQuantity = pp.RequiredQuantity,
                    FreeQuantity = pp.FreeQuantity,
                    IsGiftProduct = pp.IsGiftProduct
                }).ToList()
            };

            return dto;
        }

        // ═══════════════════════════════════════════════════════════
        // READ
        // ═══════════════════════════════════════════════════════════

        public async Task<List<PromotionDto>> GetAllByCompanyAsync(long companyID)
        {
            var entities = await BaseQuery()
                .Where(p => p.CompanyId == companyID)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Gom tất cả TypeValueID cần tra
            var typeValueIds = new HashSet<long>();
            foreach (var p in entities)
            {
                typeValueIds.Add(p.PromotionTypeId);
                typeValueIds.Add(p.PromotionStatusId);
                foreach (var r in p.PromotionRules)
                {
                    typeValueIds.Add(r.RuleTypeId);
                    typeValueIds.Add(r.DiscountTypeId);
                }
            }

            var typeMap = await GetTypeValueCodeMapAsync(typeValueIds.ToArray());

            return entities.Select(e => MapToDto(e, typeMap)).ToList();
        }

        public async Task<List<PromotionDto>> GetActivePromotionsAsync(long companyID, long? branchID = null)
        {
            var now = DateTime.Now;

            var query = BaseQuery()
                .Where(p => p.CompanyId == companyID
                         && p.IsActive
                         && p.StartDate <= now
                         && (p.EndDate == null || p.EndDate >= now)
                         && (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));

            // Lọc theo BranchID nếu có
            if (branchID.HasValue)
            {
                query = query.Where(p => p.BranchId == null || p.BranchId == branchID.Value);
            }

            var entities = await query
                .OrderByDescending(p => p.Priority)
                .ToListAsync();

            // Map với 1 query duy nhất
            var typeValueIds = new HashSet<long>();
            foreach (var p in entities)
            {
                typeValueIds.Add(p.PromotionTypeId);
                typeValueIds.Add(p.PromotionStatusId);
                foreach (var r in p.PromotionRules)
                {
                    typeValueIds.Add(r.RuleTypeId);
                    typeValueIds.Add(r.DiscountTypeId);
                }
            }

            var typeMap = await GetTypeValueCodeMapAsync(typeValueIds.ToArray());

            return entities.Select(e => MapToDto(e, typeMap)).ToList();
        }

        public async Task<PromotionDto?> GetPromotionByIdAsync(long promotionID)
        {
            var entity = await BaseQuery()
                .FirstOrDefaultAsync(x => x.PromotionId == promotionID);

            if (entity is null) return null;

            // Gom TypeValueID
            var typeValueIds = new HashSet<long>
            {
                entity.PromotionTypeId,
                entity.PromotionStatusId
            };
            foreach (var r in entity.PromotionRules)
            {
                typeValueIds.Add(r.RuleTypeId);
                typeValueIds.Add(r.DiscountTypeId);
            }

            var typeMap = await GetTypeValueCodeMapAsync(typeValueIds.ToArray());

            return MapToDto(entity, typeMap);
        }

        // ═══════════════════════════════════════════════════════════
        // CREATE / UPDATE / DELETE
        // ═══════════════════════════════════════════════════════════

        public async Task<long> CreatePromotionAsync(PromotionDto dto)
        {
            // Chuyển ValueCode string → TypeValueID số
            var typeId = await GetTypeValueIdAsync("PROMOTION_TYPE", dto.PromotionType);
            var statusId = await GetTypeValueIdAsync("PROMOTION_STATUS", dto.Status);

            if (typeId == 0 || statusId == 0)
                throw new InvalidOperationException($"Invalid PromotionType or Status");

            var promotion = new Promotion
            {
                CompanyId = 1,  // TODO: Get từ user context
                BranchId = dto.BranchId,
                PromotionCode = dto.PromotionCode,
                PromotionName = dto.PromotionName,
                PromotionTypeId = typeId,
                PromotionStatusId = statusId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MaxUsage = dto.MaxUsage,
                CurrentUsage = 0,
                Priority = dto.Priority,
                IsStackable = dto.IsStackable,
                IsActive = false,
                CreatedByUserId = 1,  // TODO: Get từ user context
                CreatedAt = DateTime.Now,
                Note = dto.Note,
                PromotionRules = new List<PromotionRule>(),
                PromotionProducts = new List<PromotionProduct>()
            };

            // Insert Rules
            if (dto.Rules.Any())
            {
                foreach (var ruleDto in dto.Rules)
                {
                    var ruleTypeId = await GetTypeValueIdAsync("PROMO_RULE_TYPE", ruleDto.RuleType);
                    var discountTypeId = await GetTypeValueIdAsync("DISCOUNT_TYPE", ruleDto.DiscountType);

                    if (ruleTypeId == 0 || discountTypeId == 0)
                        throw new InvalidOperationException($"Invalid RuleType or DiscountType");

                    promotion.PromotionRules.Add(new PromotionRule
                    {
                        RuleTypeId = ruleTypeId,
                        MinOrderAmount = ruleDto.MinOrderAmount,
                        MinQuantity = ruleDto.MinQuantity,
                        CustomerGroupId = ruleDto.CustomerGroupID,
                        CategoryId = ruleDto.CategoryID,
                        DiscountTypeId = discountTypeId,
                        DiscountValue = ruleDto.DiscountValue,
                        MaxDiscountAmount = ruleDto.MaxDiscountAmount,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Insert Products
            if (dto.Products.Any())
            {
                foreach (var productDto in dto.Products)
                {
                    promotion.PromotionProducts.Add(new PromotionProduct
                    {
                        ProductId = productDto.ProductID,
                        RequiredQuantity = productDto.RequiredQuantity,
                        FreeQuantity = productDto.FreeQuantity,
                        IsGiftProduct = productDto.IsGiftProduct,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            _db.Promotions.Add(promotion);
            await _db.SaveChangesAsync();

            return promotion.PromotionId;
        }

        public async Task UpdatePromotionAsync(PromotionDto dto)
        {
            var entity = await _db.Promotions
                .Include(p => p.PromotionRules)
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(x => x.PromotionId == dto.PromotionID);

            if (entity is null)
                throw new InvalidOperationException($"Promotion {dto.PromotionID} not found");

            // Update main fields
            entity.PromotionName = dto.PromotionName;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.MaxUsage = dto.MaxUsage;
            entity.Priority = dto.Priority;
            entity.IsStackable = dto.IsStackable;
            entity.Note = dto.Note;

            // Xóa Rules & Products cũ
            _db.PromotionRules.RemoveRange(entity.PromotionRules);
            _db.PromotionProducts.RemoveRange(entity.PromotionProducts);

            // Insert Rules mới
            if (dto.Rules.Any())
            {
                foreach (var ruleDto in dto.Rules)
                {
                    var ruleTypeId = await GetTypeValueIdAsync("PROMO_RULE_TYPE", ruleDto.RuleType);
                    var discountTypeId = await GetTypeValueIdAsync("DISCOUNT_TYPE", ruleDto.DiscountType);

                    entity.PromotionRules.Add(new PromotionRule
                    {
                        PromotionId = entity.PromotionId,
                        RuleTypeId = ruleTypeId,
                        MinOrderAmount = ruleDto.MinOrderAmount,
                        MinQuantity = ruleDto.MinQuantity,
                        CustomerGroupId = ruleDto.CustomerGroupID,
                        CategoryId = ruleDto.CategoryID,
                        DiscountTypeId = discountTypeId,
                        DiscountValue = ruleDto.DiscountValue,
                        MaxDiscountAmount = ruleDto.MaxDiscountAmount,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // Insert Products mới
            if (dto.Products.Any())
            {
                foreach (var productDto in dto.Products)
                {
                    entity.PromotionProducts.Add(new PromotionProduct
                    {
                        PromotionId = entity.PromotionId,
                        ProductId = productDto.ProductID,
                        RequiredQuantity = productDto.RequiredQuantity,
                        FreeQuantity = productDto.FreeQuantity,
                        IsGiftProduct = productDto.IsGiftProduct,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeletePromotionAsync(long promotionID)
        {
            var entity = await _db.Promotions
                .FirstOrDefaultAsync(x => x.PromotionId == promotionID);

            if (entity is null)
                throw new InvalidOperationException($"Promotion {promotionID} not found");

            // Không cho phép xóa nếu đã được sử dụng
            if (entity.CurrentUsage > 0)
                throw new InvalidOperationException(
                    "KM đã được sử dụng, không thể xóa. Hãy chuyển sang EXPIRED.");

            _db.Promotions.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(long promotionID, string newStatus)
        {
            var entity = await _db.Promotions
                .FirstOrDefaultAsync(x => x.PromotionId == promotionID);

            if (entity is null)
                throw new InvalidOperationException($"Promotion {promotionID} not found");

            var statusId = await GetTypeValueIdAsync("PROMOTION_STATUS", newStatus);
            if (statusId == 0)
                throw new InvalidOperationException($"Invalid status: {newStatus}");

            entity.PromotionStatusId = statusId;

            // Set IsActive theo status
            entity.IsActive = newStatus == "ACTIVE";

            await _db.SaveChangesAsync();
        }

        public async Task DeductPromotionUsageAsync(long promotionID)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Dùng UPDLOCK để lock hàng, tránh race condition
                var entity = await _db.Promotions
                    .FromSqlInterpolated($"SELECT * FROM promotions.Promotions WITH (UPDLOCK) WHERE PromotionID = {promotionID}")
                    .FirstOrDefaultAsync();

                if (entity is null)
                    throw new InvalidOperationException($"Promotion {promotionID} not found");

                // Tăng CurrentUsage
                entity.CurrentUsage++;

                // Nếu hết usage → deactivate
                if (entity.MaxUsage.HasValue && entity.CurrentUsage >= entity.MaxUsage)
                {
                    entity.IsActive = false;
                    var expiredStatusId = await GetTypeValueIdAsync("PROMOTION_STATUS", "EXPIRED");
                    entity.PromotionStatusId = expiredStatusId;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Lỗi khi ghi nhận sử dụng KM", ex);
            }
        }

        public async Task<List<BranchDto>> GetBranchesAsync(long companyId)
        {
            return await _db.Set<Branch>()
                .Where(b => b.CompanyId == companyId)
                .Select(b => new BranchDto
                {
                    BranchID = b.BranchId,
                    CompanyID = b.CompanyId,
                    BranchName = b.BranchName,
                    BranchCode = b.BranchCode
                })
                .ToListAsync();
        }

        public async Task<List<CustomerGroupDto>> GetCustomerGroupsAsync(long companyId)
        {
            return await _db.Set<CustomerGroup>()
                .Where(cg => cg.CompanyId == companyId && cg.IsActive)
                .Select(cg => new CustomerGroupDto
                {
                    GroupID = cg.CustomerGroupId,
                    CompanyID = cg.CompanyId,
                    GroupName = cg.GroupName,
                    GroupCode = cg.GroupCode
                })
                .ToListAsync();
        }

        public async Task<List<ProductCategoryDto>> GetCategoriesAsync(long companyId)
        {
            return await _db.Set<ProductCategory>()
                .Where(pc => pc.CompanyId == companyId && pc.IsActive)
                .Select(pc => new ProductCategoryDto
                {
                    CategoryID = pc.CategoryId,
                    CompanyID = pc.CompanyId,
                    CategoryName = pc.CategoryName,
                    CategoryCode = pc.CategoryCode
                })
                .ToListAsync();
        }

        public async Task<List<ProductDto>> GetProductsAsync(long companyId)
        {
            return await _db.Set<Product>()
                .Where(p => p.CompanyId == companyId)
                .Select(p => new ProductDto
                {
                    ProductID = p.ProductId,
                    CompanyID = p.CompanyId,
                    ProductName = p.ProductName,
                    ProductCode = p.ProductCode,
                    SKU = string.Empty,
                    Price = p.StandardPrice
                })
                .ToListAsync();
        }

        public async Task<List<SystemTypeValueDto>> GetTypeValuesAsync(string typeCode)
        {
            return await _db.SystemTypeValues
                .FromSqlInterpolated($@"
            SELECT stv.TypeValueID, stv.TypeID, stv.ValueCode, stv.ValueName 
            FROM   dbo.SystemTypeValues stv
            JOIN   dbo.SystemTypes st ON st.TypeID = stv.TypeID
            WHERE  st.TypeCode = {typeCode}")
                .Select(stv => new SystemTypeValueDto
                {
                    ValueCode = stv.ValueCode,
                    ValueName = stv.ValueName
                })
                .ToListAsync();
        }
    }
}