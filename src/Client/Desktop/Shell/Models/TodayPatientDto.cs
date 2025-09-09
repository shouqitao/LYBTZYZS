using System;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Shell.Models
{
    /// <summary>
    /// 今日患者显示数据传输对象
    /// 用于工作台今日患者列表显示
    /// </summary>
    public class TodayPatientDto
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public Gender Gender { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 关联的医案ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor { get; set; } = "#000000";

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 医案状态枚举（用于排序和逻辑判断）
        /// </summary>
        public MedicalCaseStatus CaseStatus { get; set; }

        /// <summary>
        /// 性别显示文本
        /// </summary>
        public string GenderDisplay => Gender switch
        {
            Gender.Male => "男",
            Gender.Female => "女",
            _ => "未知"
        };

        /// <summary>
        /// 创建时间显示文本
        /// </summary>
        public string CreateTimeDisplay => CreateTime.ToString("HH:mm");

        /// <summary>
        /// 详细状态显示（包含医生信息）
        /// </summary>
        public string DetailedStatusDisplay => $"{Status} · {DoctorName}";

        /// <summary>
        /// 是否可以开始诊疗（已挂号状态）
        /// </summary>
        public bool CanStartConsultation => CaseStatus == MedicalCaseStatus.Registered;

        /// <summary>
        /// 是否正在诊疗中
        /// </summary>
        public bool IsInConsultation => CaseStatus == MedicalCaseStatus.InConsultation;
    }
}