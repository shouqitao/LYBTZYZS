using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Core.Extensions
{
    /// <summary>
    /// DTO到Info类型的转换扩展方法
    /// </summary>
    /// <summary>
    /// DTO到Info类型的转换扩展方法
    /// </summary>
    public static class DtoToInfoExtensions
    {
        /// <summary>
        /// FormulaDto转换为FormulaInfo
        /// </summary>
        public static FormulaInfo ToFormulaInfo(this FormulaDto dto)
        {
            if (dto == null) return new FormulaInfo();
            
            return new FormulaInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = "其他", // FormulaDto没有Category字段，使用默认值
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark,
                IsShared = dto.IsShared,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                CreatedBy = dto.CreatedByName, // 映射到CreatedBy
                Indications = dto.Indications
            };
        }

        /// <summary>
        /// HerbDto转换为HerbInfo
        /// </summary>
        public static HerbInfo ToHerbInfo(this HerbDto dto)
        {
            if (dto == null) return new HerbInfo();
            
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode,
                WuBiCode = dto.WuBiCode,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark,
                Status = dto.Status,
                Stock = dto.Stock
            };
        }

        /// <summary>
        /// 批量转换FormulaDto列表为FormulaInfo列表
        /// </summary>
        public static List<FormulaInfo> ToFormulaInfoList(this List<FormulaDto> dtos)
        {
            return dtos?.Select(dto => dto.ToFormulaInfo()).ToList() ?? new List<FormulaInfo>();
        }

        /// <summary>
        /// 批量转换HerbDto列表为HerbInfo列表
        /// </summary>
        public static List<HerbInfo> ToHerbInfoList(this List<HerbDto> dtos)
        {
            return dtos?.Select(dto => dto.ToHerbInfo()).ToList() ?? new List<HerbInfo>();
        }
    }
}