using LYBT.Desktop.Infrastructure.Configuration;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 诊所配置服务实现 - OpenSpec: print-prescription-slip
    /// </summary>
    /// <remarks>
    /// 从appsettings.json的ClinicSettings节点读取配置。
    /// 支持配置热更新（基于IConfiguration的reloadOnChange）。
    /// </remarks>
    public class ClinicSettingsService : IClinicSettingsService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 诊所名称
        /// </summary>
        public string ClinicName => GetSettings().Name;

        /// <summary>
        /// 诊所地址
        /// </summary>
        public string? ClinicAddress => GetSettings().Address;

        /// <summary>
        /// 诊所电话
        /// </summary>
        public string? ClinicPhone => GetSettings().Phone;

        /// <summary>
        /// 科别
        /// </summary>
        public string Department => GetSettings().Department;

        /// <summary>
        /// 构造函数 - 注入配置对象
        /// </summary>
        /// <param name="configuration">配置对象</param>
        public ClinicSettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 获取当前诊所配置
        /// </summary>
        /// <returns>诊所配置对象</returns>
        public ClinicSettings GetSettings()
        {
            // 每次都从配置中读取以支持热更新
            var section = _configuration.GetSection(ClinicSettings.SectionName);
            var settings = section.Get<ClinicSettings>() ?? new ClinicSettings();

            return settings;
        }
    }
}
