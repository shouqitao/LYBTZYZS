namespace LYBT.Desktop.Infrastructure.Models
{
    /// <summary>
    /// 重复药材剂量取值策略
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public enum DuplicateDosageStrategy
    {
        /// <summary>
        /// 取两个剂量中的较大值(默认)
        /// </summary>
        Max = 0,

        /// <summary>
        /// 取两个剂量中的较小值
        /// </summary>
        Min = 1,

        /// <summary>
        /// 两个剂量相加
        /// </summary>
        Sum = 2,

        /// <summary>
        /// 两个剂量的平均值
        /// </summary>
        Average = 3,

        /// <summary>
        /// 保留第一个添加的剂量
        /// </summary>
        First = 4
    }

    /// <summary>
    /// 剂量策略扩展方法
    /// </summary>
    public static class DuplicateDosageStrategyExtensions
    {
        /// <summary>
        /// 根据策略计算合并后的剂量
        /// </summary>
        /// <param name="strategy">策略</param>
        /// <param name="existingDosage">现有剂量</param>
        /// <param name="newDosage">新剂量</param>
        /// <returns>合并后的剂量</returns>
        public static int CalculateMergedDosage(this DuplicateDosageStrategy strategy, int existingDosage, int newDosage)
        {
            return strategy switch
            {
                DuplicateDosageStrategy.Max => Math.Max(existingDosage, newDosage),
                DuplicateDosageStrategy.Min => Math.Min(existingDosage, newDosage),
                DuplicateDosageStrategy.Sum => existingDosage + newDosage,
                DuplicateDosageStrategy.Average => (existingDosage + newDosage) / 2,
                DuplicateDosageStrategy.First => existingDosage,
                _ => Math.Max(existingDosage, newDosage)
            };
        }

        /// <summary>
        /// 获取策略的显示名称
        /// </summary>
        public static string GetDisplayName(this DuplicateDosageStrategy strategy)
        {
            return strategy switch
            {
                DuplicateDosageStrategy.Max => "取较大值",
                DuplicateDosageStrategy.Min => "取较小值",
                DuplicateDosageStrategy.Sum => "剂量相加",
                DuplicateDosageStrategy.Average => "取平均值",
                DuplicateDosageStrategy.First => "保留原值",
                _ => "取较大值"
            };
        }
    }
}
