using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 方剂DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class FormulaDtoExtensions
    {
        /// <summary>
        /// 将FormulaInputDto转换为FormulaDto
        /// Issue #1152: 替代AutoMapper
        /// 注意：Herbs集合需要在Service层单独处理
        /// </summary>
        public static FormulaDto ToDto(this FormulaInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new FormulaDto
            {
                Name = dto.Name,
                Effect = dto.Effect,
                Indications = dto.Indications,
                Description = dto.Description,
                Usage = dto.Usage,
                Property = dto.Property,
                IsShared = dto.IsShared,
                Contraindications = dto.Contraindications,
                Remark = dto.Remark,
                Source = null,  // CreateDto中不存在
                Herbs = new List<FormulaHerbItemDto>(),  // 需要在Service层处理
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
                // Category是只读计算属性，不能赋值
            };
        }

        /// <summary>
        /// 将FormulaInputDto应用到现有FormulaDto
        /// Issue #1152: 替代AutoMapper
        /// 注意：Herbs集合需要在Service层单独处理
        /// </summary>
        public static void ApplyUpdate(this FormulaDto existing, FormulaInputDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            existing.Name = dto.Name;
            existing.Effect = dto.Effect;
            existing.Indications = dto.Indications;
            existing.Description = dto.Description;
            existing.Usage = dto.Usage;
            existing.Property = dto.Property;
            existing.IsShared = dto.IsShared;
            existing.Contraindications = dto.Contraindications;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;
            // Herbs集合需要在Service层单独处理
            // Category是只读计算属性，不能赋值
        }
    }
}
