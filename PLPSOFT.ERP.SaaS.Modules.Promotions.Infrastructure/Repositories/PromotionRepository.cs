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
    /// Toàn bộ câu truy vấn SQL thực tế nằm ở đây.
    /// </summary>
    public class PromotionRepository : IPromotionRepository
    {
        private readonly PromotionDbContext _db;

        public PromotionRepository(PromotionDbContext db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════════════
        // HELPER PRIVATE
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Query gốc: luôn Include Rules + Products, dùng AsNoTracking vì chỉ đọc.
        /// </summary>
        private IQueryable<Promotion> BaseQuery()
            => _db.Promotions
                  .Include(p => p.PromotionRules)
                  .Include(p => p.PromotionProducts)
                  .AsNoTracking();

        /// <summary>
        /// Tra bảng SystemTypeValues lấy ValueCode từ TypeValueID.
        /// Vì Entity lưu ID (PromotionTypeId, PromotionStatusId),
        /// cần tra ngược ra code "ACTIVE", "DISCOUNT"... để trả về DTO.
        /// </summary>
        private async Task<string> GetValueCodeAsync(long typeValueId)
        {
            var result = await _db.Database
                .SqlQuery<string>(
                    $"SELECT ValueCode FROM dbo.SystemTypeValues WHERE TypeValueID = {typeValueId}")
                .FirstOrDefaultAsync();
            return result ?? string.Empty;
        }

        /// <summary>
        /// Tra bảng SystemTypeValues lấy TypeValueID từ TypeCode + ValueCode.
        /// Dùng khi tạo mới: chuyển "ACTIVE" → ID số.
        /// </summary>
        private async Task<long> GetTypeValueIdAsync(string typeCode, string valueCode)
        {
            var result = await _db.Database
                .SqlQuery<long>($@"
                    SELECT stv.TypeValueID
                    FROM   dbo.SystemTypeValues stv
                    JOIN   dbo.SystemTypes      st  ON st.TypeID = stv.TypeID
                    WHERE  st.TypeCode   = {typeCode}
                    AND    stv.ValueCode = {valueCode}")
                .FirstOrDefaultAsync();
            return result;
        }

        /// <summary>
        /// Map Entity Promotion → PromotionDto để trả về cho Engine và Web.
        /// </summary>
        private static PromotionDto MapToDto(
            Promotion p,
            string promotionTypeCode,
            string statusCode)
        => new()
        {
            PromotionID = p.PromotionId,
            PromotionCode = p.PromotionCode,
            PromotionName = p.PromotionName,
            PromotionType = promotionTypeCode,   // "DISCOUNT" hoặc "BOGO"
            Status = statusCode,           // "ACTIVE", "DRAFT", "EXPIRED"
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            MaxUsage = p.MaxUsage,
            CurrentUsage = p.CurrentUsage,
            IsStackable = p.IsStackable,
            Priority = p.Priority,
            Note = p.Note,

            // Map từng Rule
            Rules = p.PromotionRules.Select(r => new PromotionRuleDto
            {
                RuleID = r.RuleId,
                PromotionID = r.PromotionId,
                // RuleType và DiscountType cần tra DB — xử lý riêng ở MapListAsync
                RuleType = r.RuleTypeId.ToString(),
                DiscountType = r.DiscountTypeId.ToString(),
                MinOrderAmount = r.MinOrderAmount,
                MinQuantity = r.MinQuantity,
                CustomerGroupID = r.CustomerGroupId,
                CategoryID = r.CategoryId,
                DiscountValue = r.DiscountValue,
                MaxDiscountAmount = r.MaxDiscountAmount
            }).ToList(),

            // Map từng Product
            Products = p.PromotionProducts.Select(pp => new PromotionProductDto
            {
                ProductID = pp.ProductId,
                RequiredQuantity = pp.RequiredQuantity,
                FreeQuantity = pp.FreeQuantity,
                IsGiftProduct = pp.IsGiftProduct
            }).ToList()
        };

        /// <summary>
        /// Map danh sách Entity → DTO, tra code cho từng promotion.
        /// </summary>
        private async Task<List<PromotionDto>> MapListAsync(List<Promotion> list)
        {
            var result = new List<PromotionDto>();
            foreach (var p in list)
            {
                var typeCode = await GetValueCodeAsync(p.PromotionTypeId);
                var statusCode = await GetValueCodeAsync(p.PromotionStatusId);
                var dto = MapToDto(p, typeCode, statusCode);

                // Tra thêm RuleType và DiscountType cho từng rule
                foreach (var rule in dto.Rules)
                {
                    var entity = p.PromotionRules
                        .First(r => r.RuleId == rule.RuleID);
                    rule.RuleType = await GetValueCodeAsync(entity.RuleTypeId);
                    rule.DiscountType = await GetValueCodeAsync(entity.DiscountTypeId);
                }

                result.Add(dto);
            }
            return result;
        }

        // ════════════════════════════════════════════════════════════
        // ĐỌC DỮ LIỆU
        // ════════════════════════════════════════════════════════════

        public async Task<List<PromotionDto>> GetActivePromotionsAsync(long companyID)
        {
            var now = DateTime.Now;

            // Lấy StatusID của "ACTIVE" để lọc
            var activeStatusId = await GetTypeValueIdAsync("PROMOTION_STATUS", "ACTIVE");

            var list = await BaseQuery()
                .Where(p => p.CompanyId == companyID
                         && p.IsActive
                         && p.PromotionStatusId == activeStatusId
                         && p.StartDate <= now
                         && (p.EndDate == null || p.EndDate >= now))
                .OrderByDescending(p => p.Priority)
                .ToListAsync();

            return await MapListAsync(list);
        }

        public async Task<List<PromotionDto>> GetAllAsync(long companyID)
        {
            var list = await BaseQuery()
                .Where(p => p.CompanyId == companyID)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return await MapListAsync(list);
        }

        public async Task<PromotionDto?> GetByIdAsync(long promotionID)
        {
            var p = await BaseQuery()
                .FirstOrDefaultAsync(x => x.PromotionId == promotionID
                                       && x.IsActive);
            if (p is null) return null;

            var typeCode = await GetValueCodeAsync(p.PromotionTypeId);
            var statusCode = await GetValueCodeAsync(p.PromotionStatusId);
            var dto = MapToDto(p, typeCode, statusCode);

            foreach (var rule in dto.Rules)
            {
                var entity = p.PromotionRules.First(r => r.RuleId == rule.RuleID);
                rule.RuleType = await GetValueCodeAsync(entity.RuleTypeId);
                rule.DiscountType = await GetValueCodeAsync(entity.DiscountTypeId);
            }

            return dto;
        }

        public async Task<PromotionDto?> GetByCodeAsync(long companyID, string promotionCode)
        {
            var p = await BaseQuery()
                .FirstOrDefaultAsync(x => x.CompanyId == companyID
                                       && x.PromotionCode == promotionCode
                                       && x.IsActive);
            if (p is null) return null;

            var typeCode = await GetValueCodeAsync(p.PromotionTypeId);
            var statusCode = await GetValueCodeAsync(p.PromotionStatusId);
            var dto = MapToDto(p, typeCode, statusCode);

            foreach (var rule in dto.Rules)
            {
                var entity = p.PromotionRules.First(r => r.RuleId == rule.RuleID);
                rule.RuleType = await GetValueCodeAsync(entity.RuleTypeId);
                rule.DiscountType = await GetValueCodeAsync(entity.DiscountTypeId);
            }

            return dto;
        }

        public async Task<bool> IsCodeExistsAsync(long companyID, string promotionCode)
        {
            return await _db.Promotions
                .AnyAsync(x => x.CompanyId == companyID
                            && x.PromotionCode == promotionCode
                            && x.IsActive);
        }

        // ════════════════════════════════════════════════════════════
        // GHI DỮ LIỆU
        // ════════════════════════════════════════════════════════════

        public async Task<long> CreateAsync(
            PromotionDto dto,
            long companyID,
            long createdByUserID)
        {
            // Chuyển code string → ID số để lưu vào DB
            var typeId = await GetTypeValueIdAsync("PROMOTION_TYPE", dto.PromotionType);
            var statusId = await GetTypeValueIdAsync("PROMOTION_STATUS", dto.Status);

            var entity = new Promotion
            {
                CompanyId = companyID,
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
                IsActive = true,
                CreatedByUserId = createdByUserID,
                CreatedAt = DateTime.Now,
                Note = dto.Note
            };

            _db.Promotions.Add(entity);
            await _db.SaveChangesAsync();
            return entity.PromotionId;
        }

        public async Task UpdateAsync(PromotionDto dto)
        {
            // Không dùng AsNoTracking ở đây vì cần track để update
            var entity = await _db.Promotions
                .FirstOrDefaultAsync(x => x.PromotionId == dto.PromotionID);
            if (entity is null) return;

            entity.PromotionName = dto.PromotionName;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.MaxUsage = dto.MaxUsage;
            entity.Priority = dto.Priority;
            entity.IsStackable = dto.IsStackable;
            entity.Note = dto.Note;

            await _db.SaveChangesAsync();
        }

        public async Task DeactivateAsync(long promotionID)
        {
            var entity = await _db.Promotions
                .FirstOrDefaultAsync(x => x.PromotionId == promotionID);
            if (entity is null) return;

            entity.IsActive = false;
            await _db.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════
        // DÙNG CHO ENGINE — Minh Nhựt gọi
        // ════════════════════════════════════════════════════════════

        public async Task DeductUsageAsync(long promotionID)
        {
            // ExecuteUpdateAsync: update thẳng DB, không cần load entity vào memory
            // Tránh race condition khi nhiều đơn hàng cùng dùng 1 KM lúc cao điểm
            await _db.Promotions
                .Where(x => x.PromotionId == promotionID)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.CurrentUsage, x => x.CurrentUsage + 1));
        }
    }
}