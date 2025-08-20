using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Formulas
{
    /// <summary>
    /// 验方信息清洁数据模型 - UltraThink架构Data Layer
    /// 移除所有UI相关属性，专注于纯业务数据
    /// </summary>
    public class FormulaInfoClean : BaseFormula
    {
        /// <summary>分类</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>适应症</summary>
        public string? Indications { get; set; }

        /// <summary>用法用量说明</summary>
        public string? DosageInstruction { get; set; }

        /// <summary>禁忌</summary>
        public string? Contraindications { get; set; }

        /// <summary>来源</summary>
        public string? Source { get; set; }

        /// <summary>药材组成</summary>
        public List<FormulaHerbItem> Herbs { get; set; } = new();

        /// <summary>创建人</summary>
        public string? CreatedBy { get; set; }

        #region 业务计算属性

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>总价格</summary>
        public decimal TotalPrice => Herbs?.Sum(h => h.SubTotal) ?? 0;

        /// <summary>是否活跃（映射到Status == Enabled）</summary>
        public bool IsActive
        {
            get => Status == CommonStatus.Enabled;
            set => Status = value ? CommonStatus.Enabled : CommonStatus.Disabled;
        }

        #endregion

        #region 业务逻辑方法

        /// <summary>
        /// 检查验方是否包含指定药材
        /// </summary>
        public bool ContainsHerb(string herbName)
        {
            return Herbs.Any(h => h.HerbName.Contains(herbName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取指定药材的用量
        /// </summary>
        public decimal GetHerbQuantity(string herbName)
        {
            var herb = Herbs.FirstOrDefault(h => h.HerbName.Equals(herbName, StringComparison.OrdinalIgnoreCase));
            return herb?.Quantity ?? 0;
        }

        /// <summary>
        /// 检查验方是否为空
        /// </summary>
        public bool IsEmpty => HerbCount == 0;

        /// <summary>
        /// 检查验方价格是否超出预算
        /// </summary>
        public bool IsOverBudget(decimal budget) => TotalPrice > budget;

        /// <summary>
        /// 获取药材名称列表
        /// </summary>
        public string GetHerbNamesList(int maxCount = 3)
        {
            if (HerbCount == 0) return "无";
            
            var names = Herbs.Take(maxCount).Select(h => h.HerbName);
            var result = string.Join("、", names);
            
            if (HerbCount > maxCount)
                result += "...";
                
            return result;
        }

        #endregion
    }
}