using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 功能开关服务实现 - 简化的Dictionary版本 (Issue #1477 #1479)
    /// </summary>
    /// <remarks>
    /// MVP阶段使用简单的Dictionary&lt;string, bool&gt;存储功能开关配置。
    /// 配置从appsettings.json的FeatureToggles节点读取。
    /// Post-MVP阶段可扩展为支持远程配置、动态刷新、AB测试等高级功能。
    /// </remarks>
    public class FeatureToggleService : IFeatureToggleService
    {
        private readonly Dictionary<string, bool> _features;

        /// <summary>
        /// 构造函数 - 从配置文件加载功能开关
        /// </summary>
        /// <param name="configuration">配置对象（注入）</param>
        public FeatureToggleService(IConfiguration configuration)
        {
            // 从配置文件的FeatureToggles节点读取功能开关
            _features = configuration.GetSection("FeatureToggles")
                .Get<Dictionary<string, bool>>() ?? new Dictionary<string, bool>();

            // 记录加载的功能开关数量（便于调试）
            System.Diagnostics.Debug.WriteLine($"[FeatureToggleService] 已加载 {_features.Count} 个功能开关配置");
        }

        /// <summary>
        /// 检查指定功能是否启用
        /// </summary>
        /// <param name="featureKey">功能键名（如 "Consultation.Create"）</param>
        /// <returns>true表示功能启用，false表示功能禁用或未配置</returns>
        public bool IsEnabled(string featureKey)
        {
            // 如果配置中没有该功能键，默认返回false（保守策略）
            return _features.TryGetValue(featureKey, out var enabled) && enabled;
        }
    }
}
