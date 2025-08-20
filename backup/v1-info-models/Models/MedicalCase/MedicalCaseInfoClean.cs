using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例信息清洁数据模型 - UltraThink架构Data Layer
    /// 移除所有UI相关属性，专注于纯业务数据
    /// </summary>
    public class MedicalCaseInfoClean : BaseMedicalCase
    {
        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>患者年龄</summary>
        public int? PatientAge { get; set; }

        /// <summary>患者性别</summary>
        public string? PatientGender { get; set; }

        /// <summary>诊断结果</summary>
        public string? Diagnosis { get; set; }
        
        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }
        
        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>完成时间</summary>
        public new DateTime? CompleteTime { get; set; }

        /// <summary>是否可编辑</summary>
        public bool CanEdit { get; set; } = true;

        /// <summary>是否可删除</summary>
        public bool CanDelete { get; set; } = true;

        #region 业务计算属性

        /// <summary>是否已完成</summary>
        public bool IsCompleted => Status == MedicalCaseStatus.Completed;

        /// <summary>是否进行中</summary>
        public bool IsInProgress => Status == MedicalCaseStatus.InConsultation;

        /// <summary>是否已取消</summary>
        public bool IsCancelled => Status == MedicalCaseStatus.Cancelled;

        /// <summary>是否为新创建的案例</summary>
        public bool IsNew => Status == MedicalCaseStatus.Registered;

        #endregion

        #region 业务逻辑方法

        /// <summary>
        /// 获取案例持续时间（分钟）
        /// </summary>
        public double GetDurationInMinutes()
        {
            var endTime = CompleteTime ?? DateTime.Now;
            return (endTime - CreateTime).TotalMinutes;
        }

        /// <summary>
        /// 检查案例是否为当日案例
        /// </summary>
        public bool IsToday()
        {
            return CreateTime.Date == DateTime.Today;
        }

        /// <summary>
        /// 检查案例是否包含指定关键字
        /// </summary>
        public bool ContainsKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            var searchText = keyword.ToLowerInvariant();
            
            return PatientName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   DoctorName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                   (ChiefComplaint?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (Diagnosis?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
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

        /// <summary>
        /// 检查是否可以开始看诊
        /// </summary>
        public bool CanStartConsultation()
        {
            return Status == MedicalCaseStatus.Registered && CanEdit;
        }

        /// <summary>
        /// 检查是否可以完成案例
        /// </summary>
        public bool CanComplete()
        {
            return Status == MedicalCaseStatus.InConsultation && CanEdit;
        }

        /// <summary>
        /// 检查是否可以取消案例
        /// </summary>
        public bool CanCancel()
        {
            return Status != MedicalCaseStatus.Completed && 
                   Status != MedicalCaseStatus.Cancelled && 
                   CanEdit;
        }

        /// <summary>
        /// 获取案例优先级
        /// </summary>
        public string GetPriority()
        {
            var duration = GetDurationInMinutes();
            
            if (Status == MedicalCaseStatus.InConsultation)
            {
                return duration > 60 ? "高" : "中";
            }
            
            if (Status == MedicalCaseStatus.Registered)
            {
                return duration > 30 ? "中" : "低";
            }
            
            return "低";
        }

        /// <summary>
        /// 检查案例是否紧急
        /// </summary>
        public bool IsUrgent()
        {
            var duration = GetDurationInMinutes();
            
            // 挂号超过30分钟或看诊超过60分钟认为紧急
            return (Status == MedicalCaseStatus.Registered && duration > 30) ||
                   (Status == MedicalCaseStatus.InConsultation && duration > 60);
        }

        /// <summary>
        /// 获取患者基本信息摘要
        /// </summary>
        public string GetPatientSummary()
        {
            var parts = new List<string> { PatientName };
            
            if (!string.IsNullOrWhiteSpace(PatientGender))
                parts.Add(PatientGender);
                
            if (PatientAge.HasValue)
                parts.Add($"{PatientAge}岁");
                
            return string.Join(" ", parts);
        }

        /// <summary>
        /// 获取案例描述摘要
        /// </summary>
        public string GetCaseSummary()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(ChiefComplaint))
                parts.Add($"主诉: {ChiefComplaint}");
                
            if (!string.IsNullOrWhiteSpace(Diagnosis))
                parts.Add($"诊断: {Diagnosis}");
                
            return parts.Any() ? string.Join(" | ", parts) : "暂无详细信息";
        }

        /// <summary>
        /// 检查是否需要医生关注
        /// </summary>
        public bool NeedsDoctorAttention()
        {
            return IsUrgent() || 
                   (Status == MedicalCaseStatus.Registered && GetDurationInMinutes() > 15) ||
                   (Status == MedicalCaseStatus.InConsultation && GetDurationInMinutes() > 45);
        }

        #endregion
    }
}