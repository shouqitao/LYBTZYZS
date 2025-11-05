namespace LYBT.Desktop.Admin.Services
{
    /// <summary>
    /// 系统设置服务接口
    /// 负责系统配置的持久化管理（%LOCALAPPDATA%\LYBT\Desktop\system-settings.json）
    /// Epic #1832 Phase 2 - 系统设置完整实现
    /// </summary>
    public interface ISystemSettingsService
    {
        /// <summary>
        /// 系统名称
        /// </summary>
        string SystemName { get; set; }

        /// <summary>
        /// 医院名称
        /// </summary>
        string HospitalName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        string ContactPhone { get; set; }

        /// <summary>
        /// 是否启用自动备份
        /// </summary>
        bool AutoBackupEnabled { get; set; }

        /// <summary>
        /// 备份路径
        /// </summary>
        string BackupPath { get; set; }

        /// <summary>
        /// 保存系统设置到本地文件
        /// </summary>
        void Save();

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        void ResetToDefaults();
    }
}
