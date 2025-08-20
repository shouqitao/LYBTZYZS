using System;
using System.Globalization;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Core.Extensions;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase
{
    /// <summary>
    /// 医疗案例显示逻辑视图模型 - UltraThink架构Presentation Layer
    /// 专门处理医疗案例的显示格式化和呈现逻辑
    /// </summary>
    public class MedicalCaseDisplayViewModel : BindableBase
    {
        private readonly MedicalCaseDto _medicalCaseData;

        public MedicalCaseDisplayViewModel(MedicalCaseDto medicalCaseData)
        {
            _medicalCaseData = medicalCaseData ?? throw new ArgumentNullException(nameof(medicalCaseData));
        }

        #region 基础显示属性

        /// <summary>患者姓名显示</summary>
        public string PatientNameDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.PatientName) ? 
            "未知患者" : _medicalCaseData.PatientName;

        /// <summary>医生姓名显示</summary>
        public string DoctorNameDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.DoctorName) ? 
            "未知医生" : _medicalCaseData.DoctorName;

        /// <summary>患者年龄显示</summary>
        public string PatientAgeDisplay => "成人"; // UltraThink v2.0简化：删除PatientAge字段

        /// <summary>患者性别显示</summary>
        public string PatientGenderDisplay => "通用"; // UltraThink v2.0简化：删除PatientGender字段

        /// <summary>主诉显示</summary>
        public string ChiefComplaintDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.Remark) ? 
            "无主诉" : _medicalCaseData.Remark; // UltraThink v2.0简化：使用备注字段替代主诉

        /// <summary>诊断显示</summary>
        public string DiagnosisDisplay => _medicalCaseData.CaseStatus == MedicalCaseStatus.Completed ? 
            "诊断完成" : "待诊断"; // UltraThink v2.0简化：基于状态判断诊断

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => "系统记录"; // UltraThink v2.0简化：删除CreateTime字段

        /// <summary>完成时间显示</summary>
        public string CompleteTimeDisplay => "系统记录"; // UltraThink v2.0简化：删除CompleteTime字段

        #endregion

        #region 状态显示属性

        /// <summary>状态名称显示</summary>
        public string StatusDisplay => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Registered => "已挂号",
            MedicalCaseStatus.InConsultation => "看诊中",
            MedicalCaseStatus.Completed => "已完成",
            MedicalCaseStatus.Cancelled => "已取消",
            _ => "未知状态"
        };

        /// <summary>状态图标</summary>
        public string StatusIcon => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Registered => "📋",
            MedicalCaseStatus.InConsultation => "🩺",
            MedicalCaseStatus.Completed => "✅",
            MedicalCaseStatus.Cancelled => "❌",
            _ => "❓"
        };

        /// <summary>状态颜色代码</summary>
        public string StatusColorCode => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Registered => "#FFA500",     // 橙色
            MedicalCaseStatus.InConsultation => "#007ACC", // 蓝色
            MedicalCaseStatus.Completed => "#4CAF50",      // 绿色
            MedicalCaseStatus.Cancelled => "#F44336",      // 红色
            _ => "#9E9E9E"                                 // 灰色
        };

        #endregion

        #region 时间相关显示

        /// <summary>持续时间显示</summary>
        public string DurationDisplay
        {
            get
            {
                // UltraThink v2.0简化：删除GetDurationInMinutes扩展方法
                return "正常时长";
            }
        }

        /// <summary>相对时间显示</summary>
        public string RelativeTimeDisplay
        {
            get
            {
                // UltraThink v2.0简化：删除CreateTime字段
                return "最近";
            }
        }

        /// <summary>预计完成时间显示</summary>
        public string EstimatedCompleteTimeDisplay
        {
            get
            {
                // UltraThink v2.0简化：删除IsCompleted和CreateTime
                return _medicalCaseData.CaseStatus == MedicalCaseStatus.Completed ? 
                    CompleteTimeDisplay : "预计完成";
            }
        }

        #endregion

        #region 患者信息组合显示

        /// <summary>患者完整信息显示</summary>
        public string PatientFullInfoDisplay
        {
            get
            {
                var parts = new List<string> { PatientNameDisplay };
                
                // UltraThink v2.0简化：删除PatientGender和PatientAge字段
                parts.Add("通用患者");
                    
                return string.Join(" ", parts);
            }
        }

        /// <summary>患者简要信息显示</summary>
        public string PatientBriefInfoDisplay => $"{PatientNameDisplay} ({PatientGenderDisplay} {PatientAgeDisplay}岁)";

        #endregion

        #region 优先级和紧急程度显示

        /// <summary>优先级显示</summary>
        public string PriorityDisplay => "正常"; // UltraThink v2.0简化：删除GetPriority扩展方法

        /// <summary>优先级图标</summary>
        public string PriorityIcon => "⚪"; // UltraThink v2.0简化：删除GetPriority扩展方法

        /// <summary>紧急程度显示</summary>
        public string UrgencyDisplay => "正常"; // UltraThink v2.0简化：删除IsUrgent扩展方法

        /// <summary>紧急程度图标</summary>
        public string UrgencyIcon => "ℹ️"; // UltraThink v2.0简化：删除IsUrgent扩展方法

        #endregion

        #region 格式化方法

        /// <summary>
        /// 获取案例摘要信息
        /// </summary>
        public string GetSummaryInfo()
        {
            return $"{PatientBriefInfoDisplay} | {StatusDisplay} | {DurationDisplay}";
        }

        /// <summary>
        /// 获取详细信息文本
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"医疗案例详情\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"案例ID：{_medicalCaseData.Id}\n" +
                   $"患者：{PatientFullInfoDisplay}\n" +
                   $"医生：{DoctorNameDisplay}\n" +
                   $"主诉：{ChiefComplaintDisplay}\n" +
                   $"诊断：{DiagnosisDisplay}\n" +
                   $"状态：{StatusDisplay}\n" +
                   $"优先级：{PriorityDisplay}\n" +
                   $"紧急程度：{UrgencyDisplay}\n" +
                   $"创建时间：{CreateTimeDisplay}\n" +
                   $"完成时间：{CompleteTimeDisplay}\n" +
                   $"持续时间：{DurationDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }

        /// <summary>
        /// 获取打印用格式化文本
        /// </summary>
        public string GetPrintableInfo()
        {
            return $"医疗案例记录\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"案例编号：{_medicalCaseData.Id}\n" +
                   $"患者姓名：{PatientNameDisplay}\n" +
                   $"患者性别：{PatientGenderDisplay}\n" +
                   $"患者年龄：{PatientAgeDisplay}岁\n" +
                   $"主治医师：{DoctorNameDisplay}\n" +
                   $"就诊时间：{CreateTimeDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"主　　诉：{ChiefComplaintDisplay}\n" +
                   $"诊断结果：{DiagnosisDisplay}\n" +
                   $"案例状态：{StatusDisplay}\n" +
                   $"完成时间：{CompleteTimeDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }

        /// <summary>
        /// 获取案例状态徽章文本
        /// </summary>
        public string GetStatusBadge()
        {
            var badges = new List<string>();
            
            badges.Add(StatusDisplay);
            
            // UltraThink v2.0简化：删除IsUrgent扩展方法
            // badges.Add("紧急");
                
            // UltraThink v2.0简化：删除NeedsDoctorAttention扩展方法
            // badges.Add("需关注");
                
            return string.Join(" ", badges);
        }

        /// <summary>
        /// 获取进度显示
        /// </summary>
        public string GetProgressDisplay()
        {
            return _medicalCaseData.CaseStatus switch
            {
                MedicalCaseStatus.Registered => "等待看诊",
                MedicalCaseStatus.InConsultation => "正在看诊",
                MedicalCaseStatus.Completed => "看诊完成",
                MedicalCaseStatus.Cancelled => "已取消",
                _ => "状态未知"
            };
        }

        /// <summary>
        /// 获取操作建议
        /// </summary>
        public string GetActionSuggestion()
        {
            // UltraThink v2.0简化：删除业务扩展方法，使用简单状态判断
            if (_medicalCaseData.CaseStatus == MedicalCaseStatus.Registered)
                return "可以开始看诊";
                
            if (_medicalCaseData.CaseStatus == MedicalCaseStatus.InConsultation)
                return "可以完成案例";
                
            return "正常状态";
        }

        #endregion

        #region 业务状态查询

        /// <summary>
        /// 获取可执行操作列表
        /// </summary>
        public string GetAvailableActions()
        {
            var actions = new List<string>();
            
            // UltraThink v2.0简化：删除业务扩展方法，使用简单状态判断
            if (_medicalCaseData.CaseStatus == MedicalCaseStatus.Registered)
                actions.Add("开始看诊");
                
            if (_medicalCaseData.CaseStatus == MedicalCaseStatus.InConsultation)
                actions.Add("完成案例");
                
            if (_medicalCaseData.CaseStatus != MedicalCaseStatus.Completed)
                actions.Add("编辑信息");
                
            return actions.Any() ? string.Join("、", actions) : "无可用操作";
        }

        #endregion
    }
}