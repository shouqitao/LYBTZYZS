using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Desktop.Core.Models.Consultation
{
    /// <summary>
    /// 开始看诊信息模型 - 前端专用
    /// UltraThink四层架构：Info层，用于前端创建开始看诊请求
    /// </summary>
    public class ConsultationStartInfo
    {
        /// <summary>医疗案例ID</summary>
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>用户ID（兼容旧属性）</summary>
        public Guid UserId => DoctorId;

        /// <summary>预计看诊时长（分钟）</summary>
        [Range(5, 480, ErrorMessage = "预计看诊时长必须在5-480分钟之间")]
        [DisplayName("预计时长")]
        public int EstimatedDuration { get; set; } = 30;

        /// <summary>看诊类型</summary>
        [DisplayName("看诊类型")]
        public string? ConsultationType { get; set; }

        /// <summary>初步主诉</summary>
        [StringLength(500, ErrorMessage = "初步主诉长度不能超过500个字符")]
        [DisplayName("初步主诉")]
        public string? InitialComplaint { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        #region UI状态属性

        /// <summary>是否正在提交</summary>
        public bool IsSubmitting { get; set; }

        /// <summary>是否可以开始看诊</summary>
        public bool CanStart => MedicalCaseId != Guid.Empty && PatientId != Guid.Empty && DoctorId != Guid.Empty;

        #endregion

        #region 显示逻辑属性

        /// <summary>预计时长显示文本</summary>
        public string EstimatedDurationText => $"{EstimatedDuration} 分钟";

        /// <summary>看诊类型显示文本</summary>
        public string ConsultationTypeText => ConsultationType ?? "常规看诊";

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证开始看诊信息的完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (MedicalCaseId == Guid.Empty)
                return (false, "医疗案例ID不能为空");

            if (PatientId == Guid.Empty)
                return (false, "患者ID不能为空");

            if (DoctorId == Guid.Empty)
                return (false, "医生ID不能为空");

            if (EstimatedDuration < 5 || EstimatedDuration > 480)
                return (false, "预计看诊时长必须在5-480分钟之间");

            return (true, null);
        }

        /// <summary>
        /// 重置到默认状态
        /// </summary>
        public void Reset()
        {
            MedicalCaseId = Guid.Empty;
            PatientId = Guid.Empty;
            DoctorId = Guid.Empty;
            EstimatedDuration = 30;
            ConsultationType = null;
            InitialComplaint = null;
            Remark = null;
            IsSubmitting = false;
        }

        #endregion
    }
}