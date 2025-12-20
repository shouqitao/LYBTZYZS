using LYBT.Desktop.Infrastructure.Configuration;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 诊所配置服务接口 - OpenSpec: print-prescription-slip
    /// </summary>
    /// <remarks>
    /// 提供诊所相关的配置访问，配置存储在appsettings.json的ClinicSettings节点中。
    /// 支持配置热更新，修改配置文件后无需重启应用。
    ///
    /// 注意：此接口保留在Infrastructure中，因为依赖Infrastructure.Configuration.ClinicSettings
    /// </remarks>
    public interface IClinicSettingsService
    {
        /// <summary>
        /// 获取当前诊所配置
        /// </summary>
        /// <returns>诊所配置对象</returns>
        ClinicSettings GetSettings();

        /// <summary>
        /// 诊所名称
        /// </summary>
        string ClinicName { get; }

        /// <summary>
        /// 诊所地址
        /// </summary>
        string? ClinicAddress { get; }

        /// <summary>
        /// 诊所电话
        /// </summary>
        string? ClinicPhone { get; }

        /// <summary>
        /// 科别
        /// </summary>
        string Department { get; }
    }
}
