using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Core.Models.Consultation;

namespace LYBT.Desktop.Core.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例信息模型 - 前端专用，继承共享基础模型
    /// UltraThink四层架构：Info层，包含UI状态和显示逻辑
    /// </summary>
    public class MedicalCaseInfo : BaseMedicalCase
    {
        #region 基础属性（来自DTO映射）

        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>患者年龄（前端显示字段）</summary>
        public int? PatientAge { get; set; }

        /// <summary>患者性别（前端显示字段）</summary>
        public string? PatientGender { get; set; }

        /// <summary>诊断结果</summary>
        public string? Diagnosis { get; set; }
        
        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }
        
        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>完成时间</summary>
        public new DateTime? CompleteTime { get; set; }

        #endregion

        #region UI状态属性

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否展开详情</summary>
        public bool IsExpanded { get; set; }

        /// <summary>是否正在编辑</summary>
        public bool IsEditing { get; set; }

        /// <summary>是否正在加载</summary>
        public bool IsLoading { get; set; }

        /// <summary>是否可编辑</summary>
        public bool CanEdit { get; set; } = true;

        /// <summary>是否可删除</summary>
        public bool CanDelete { get; set; } = true;

        #endregion

        #region 显示逻辑属性

        /// <summary>状态描述（前端显示字段）</summary>
        public string StatusText => GetStatusText();

        /// <summary>状态颜色</summary>
        public string StatusColor => GetStatusColor();

        /// <summary>创建时间格式化（前端显示）</summary>
        public string CreateTimeText => CreateTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>完成时间格式化（前端显示）</summary>
        public string? CompleteTimeText => CompleteTime?.ToString("yyyy-MM-dd HH:mm");

        /// <summary>患者信息显示文本</summary>
        public string PatientDisplayText => $"{PatientName} {PatientGender} {PatientAge}岁";

        /// <summary>案例摘要显示文本</summary>
        public string SummaryText => $"{ChiefComplaint ?? "无主诉"} | {Diagnosis ?? "待诊断"}";

        /// <summary>持续时间显示文本</summary>
        public string DurationText => CompleteTime.HasValue 
            ? $"{(CompleteTime.Value - CreateTime).TotalMinutes:F0}分钟"
            : $"{(DateTime.Now - CreateTime).TotalMinutes:F0}分钟";

        /// <summary>是否已完成</summary>
        public bool IsCompleted => Status == MedicalCaseStatus.Completed;

        /// <summary>是否进行中</summary>
        public bool IsInProgress => Status == MedicalCaseStatus.InConsultation;

        /// <summary>是否已取消</summary>
        public bool IsCancelled => Status == MedicalCaseStatus.Cancelled;

        #endregion

        #region 关联对象

        /// <summary>看诊信息（前端关联对象）</summary>
        public ConsultationInfo? ConsultationInfo { get; set; }

        #endregion

        #region 私有方法

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

        private string GetStatusColor()
        {
            return Status switch
            {
                MedicalCaseStatus.Registered => "#FFA500",     // 橙色
                MedicalCaseStatus.InConsultation => "#007ACC", // 蓝色
                MedicalCaseStatus.Completed => "#4CAF50",      // 绿色
                MedicalCaseStatus.Cancelled => "#F44336",      // 红色
                _ => "#9E9E9E"                                 // 灰色
            };
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 重置编辑状态
        /// </summary>
        public void ResetEditingState()
        {
            IsEditing = false;
            IsExpanded = false;
        }

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (PatientId == Guid.Empty)
                return (false, "患者ID不能为空");

            if (DoctorId == Guid.Empty)
                return (false, "医生ID不能为空");

            if (string.IsNullOrWhiteSpace(PatientName))
                return (false, "患者姓名不能为空");

            if (string.IsNullOrWhiteSpace(DoctorName))
                return (false, "医生姓名不能为空");

            return (true, null);
        }

        #endregion
    }

}