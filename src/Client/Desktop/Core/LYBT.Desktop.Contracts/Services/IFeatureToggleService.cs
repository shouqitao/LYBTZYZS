namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 功能开关服务接口 - MVP阶段功能控制 (Issue #1477 #1479)
    /// </summary>
    /// <remarks>
    /// 用于控制Desktop端功能的启用/禁用状态，支持MVP阶段的功能简化。
    /// 配置存储在appsettings.json的FeatureToggles节点中。
    /// </remarks>
    public interface IFeatureToggleService
    {
        /// <summary>
        /// 检查指定功能是否启用
        /// </summary>
        /// <param name="featureKey">功能键名（如 "Consultation.Create"）</param>
        /// <returns>true表示功能启用，false表示功能禁用</returns>
        bool IsEnabled(string featureKey);
    }
}
