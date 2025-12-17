namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 处方设置服务接口 - OpenSpec: enhance-duplicate-herb-dialog
    /// </summary>
    /// <remarks>
    /// 提供处方相关的配置访问，配置存储在appsettings.json的Prescription节点中。
    /// 后期将开发到系统设置UI中进行动态配置。
    /// </remarks>
    public interface IPrescriptionSettingsService
    {
        /// <summary>
        /// 重复药材合并策略
        /// Max = 取最大值, Min = 取最小值, Sum = 累加, Import = 使用导入值, Keep = 保留原值
        /// </summary>
        string DuplicateHerbMergeStrategy { get; }

        /// <summary>
        /// 计算合并后的剂量
        /// </summary>
        /// <param name="currentDosage">当前剂量（整数克）</param>
        /// <param name="importedDosage">导入剂量（整数克）</param>
        /// <returns>根据合并策略计算的剂量（整数克）</returns>
        int CalculateMergedDosage(int currentDosage, int importedDosage);
    }
}
