using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 处方设置服务实现 - OpenSpec: enhance-duplicate-herb-dialog
    /// </summary>
    /// <remarks>
    /// 从appsettings.json的Prescription节点读取配置。
    /// 同时提供静态访问器供POCO类使用（如DuplicateHerbInfo）。
    /// 后期将开发到系统设置UI中进行动态配置。
    /// </remarks>
    public class PrescriptionSettingsService : IPrescriptionSettingsService
    {
        private const string DefaultMergeStrategy = "Max";

        /// <summary>
        /// 静态实例（供POCO类访问）
        /// </summary>
        public static IPrescriptionSettingsService? Current { get; private set; }

        /// <summary>
        /// 重复药材合并策略
        /// </summary>
        public string DuplicateHerbMergeStrategy { get; }

        /// <summary>
        /// 构造函数 - 从配置文件加载处方设置
        /// </summary>
        /// <param name="configuration">配置对象（注入）</param>
        public PrescriptionSettingsService(IConfiguration configuration)
        {
            // 从配置文件的Prescription节点读取设置
            var section = configuration.GetSection("Prescription");
            DuplicateHerbMergeStrategy = section.GetValue<string>("DuplicateHerbMergeStrategy") ?? DefaultMergeStrategy;

            // 设置静态实例供POCO类访问
            Current = this;

            System.Diagnostics.Debug.WriteLine($"[PrescriptionSettingsService] 重复药材合并策略: {DuplicateHerbMergeStrategy}");
        }

        /// <summary>
        /// 计算合并后的剂量
        /// </summary>
        public decimal CalculateMergedDosage(decimal currentDosage, decimal importedDosage)
        {
            return DuplicateHerbMergeStrategy switch
            {
                "Max" => Math.Max(currentDosage, importedDosage),
                "Min" => Math.Min(currentDosage, importedDosage),
                "Sum" => currentDosage + importedDosage,
                "Import" => importedDosage,
                "Keep" => currentDosage,
                _ => Math.Max(currentDosage, importedDosage)
            };
        }

        /// <summary>
        /// 静态方法 - 计算合并后的剂量（供POCO类使用）
        /// </summary>
        public static decimal GetMergedDosage(decimal currentDosage, decimal importedDosage)
        {
            return Current?.CalculateMergedDosage(currentDosage, importedDosage)
                   ?? Math.Max(currentDosage, importedDosage); // 默认取最大值
        }
    }
}
