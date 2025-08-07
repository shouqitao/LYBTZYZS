using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Models.Consultation;

namespace LYBT.WPF.Client.Core.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class MedicalCaseInfo : BaseMedicalCaseModel
    {
        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>状态描述（前端显示字段）</summary>
        public string StatusText => GetStatusText();

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>看诊信息（前端关联对象）</summary>
        public ConsultationInfo? ConsultationInfo { get; set; }

        /// <summary>患者年龄（前端显示字段）</summary>
        public int? PatientAge { get; set; }

        /// <summary>患者性别（前端显示字段）</summary>
        public string? PatientGender { get; set; }

        /// <summary>创建时间格式化（前端显示）</summary>
        public string CreateTimeText => CreateTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>完成时间格式化（前端显示）</summary>
        public string? CompleteTimeText => CompleteTime?.ToString("yyyy-MM-dd HH:mm");

        private string GetStatusText()
        {
            return Status switch
            {
                MedicalCaseStatus.Registered => "已挂号",
                MedicalCaseStatus.InConsultation => "看诊中",
                MedicalCaseStatus.Completed => "已完成",
                MedicalCaseStatus.Cancelled => "已取消",
                _ => "未知状态"
            };
        }
    }

}