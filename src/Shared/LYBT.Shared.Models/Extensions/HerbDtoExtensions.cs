using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 药材DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class HerbDtoExtensions
    {
        /// <summary>
        /// 将HerbCreateDto转换为HerbDto
        /// Issue #1152: 替代AutoMapper
        /// 字段映射: Origin→Category, Spec→Properties
        /// </summary>
        public static HerbDto ToDto(this HerbCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new HerbDto
            {
                Name = dto.Name,
                PinYinCode = dto.PinYinCode,
                Category = dto.Origin,      // Origin → Category 映射
                Properties = dto.Spec,       // Spec → Properties 映射
                Origin = dto.Origin,         // HerbDto也有Origin字段
                Spec = dto.Spec,             // HerbDto也有Spec字段
                Unit = dto.Unit,
                Price = dto.Price,
                CostPrice = dto.CostPrice,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = null,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将HerbUpdateDto应用到现有HerbDto
        /// Issue #1152: 替代AutoMapper
        /// 字段映射: Origin→Category, Spec→Properties
        /// </summary>
        public static void ApplyUpdate(this HerbDto existing, HerbUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            existing.Name = dto.Name;
            existing.PinYinCode = dto.PinYinCode;
            existing.Category = dto.Origin;      // Origin → Category 映射
            existing.Properties = dto.Spec;       // Spec → Properties 映射
            existing.Origin = dto.Origin;         // HerbDto也有Origin字段
            existing.Spec = dto.Spec;             // HerbDto也有Spec字段
            existing.Unit = dto.Unit;
            existing.Price = dto.Price;
            existing.CostPrice = dto.CostPrice;
            existing.Effect = dto.Effect;
            existing.Usage = dto.Usage;
            existing.UpdatedAt = DateTime.UtcNow;
            // Remark在UpdateDto中不存在，保持不变
        }
    }
}
