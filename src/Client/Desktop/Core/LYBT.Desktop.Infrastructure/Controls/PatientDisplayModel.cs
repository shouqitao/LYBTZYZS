namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 患者信息展示模型 - 用于PatientInfoCardControl数据绑定
    /// OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public class PatientDisplayModel
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 就诊次数
        /// </summary>
        public int VisitCount { get; set; }

        /// <summary>
        /// 挂号时间
        /// </summary>
        public DateTime? RegistrationTime { get; set; }

        /// <summary>
        /// 格式化的年龄显示
        /// </summary>
        public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";

        /// <summary>
        /// 格式化的挂号时间显示
        /// </summary>
        public string RegistrationTimeDisplay => RegistrationTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";

        /// <summary>
        /// 基本信息摘要 (姓名 性别 年龄)
        /// </summary>
        public string BasicInfoSummary => $"{Name} {Gender} {AgeDisplay}";
    }
}
