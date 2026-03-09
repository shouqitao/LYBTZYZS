using LYBT.Shared.Configuration.Options.Client;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 诊所配置服务接口
    /// D2: 诊所信息配置化，支持运行时热更新和持久化
    /// </summary>
    public interface IClinicSettingsService
    {
        /// <summary>
        /// 获取当前诊所配置（只读快照）
        /// </summary>
        ClinicSettingsOptions GetSettings();

        /// <summary>
        /// 诊所名称
        /// </summary>
        string ClinicName { get; }

        /// <summary>
        /// 诊所地址
        /// </summary>
        string ClinicAddress { get; }

        /// <summary>
        /// 诊所电话
        /// </summary>
        string ClinicPhone { get; }

        /// <summary>
        /// 科别
        /// </summary>
        string Department { get; }

        /// <summary>
        /// 执业许可证号
        /// </summary>
        string LicenseNumber { get; }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        string Email { get; }

        /// <summary>
        /// 保存诊所配置到 clinic-settings.json
        /// </summary>
        /// <param name="settings">要保存的配置</param>
        /// <returns>是否保存成功</returns>
        Task<bool> SaveSettingsAsync(ClinicSettingsOptions settings);
    }
}
