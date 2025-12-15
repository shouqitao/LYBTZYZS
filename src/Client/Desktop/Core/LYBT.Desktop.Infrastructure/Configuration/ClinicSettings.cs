namespace LYBT.Desktop.Infrastructure.Configuration
{
    /// <summary>
    /// 诊所配置信息 - OpenSpec: print-prescription-slip
    /// </summary>
    /// <remarks>
    /// 存储在appsettings.json的ClinicSettings节点中，用于处方打印等场景。
    /// </remarks>
    public class ClinicSettings
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "ClinicSettings";

        /// <summary>
        /// 诊所名称
        /// </summary>
        public string Name { get; set; } = "中医诊所";

        /// <summary>
        /// 诊所地址
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// 诊所电话
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 科别
        /// </summary>
        public string Department { get; set; } = "中医科";
    }
}
