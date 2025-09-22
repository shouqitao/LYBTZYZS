using System.Collections.Generic;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase
{
    /// <summary>
    /// 病例显示逻辑视图模型
    /// </summary>
    public class MedicalCaseDisplayViewModel : BindableBase
    {
        private readonly MedicalCaseDto _medicalCaseData;

        public MedicalCaseDisplayViewModel(MedicalCaseDto medicalCaseData)
        {
            _medicalCaseData = medicalCaseData ?? throw new ArgumentNullException(nameof(medicalCaseData));
        }

        // 基础显示
        public string PatientNameDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.PatientName) ? "未知患者" : _medicalCaseData.PatientName;
        public string DoctorNameDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.DoctorName) ? "未知医生" : _medicalCaseData.DoctorName;
        public string PatientAgeDisplay => "--";
        public string PatientGenderDisplay => "--";
        public string ChiefComplaintDisplay => string.IsNullOrWhiteSpace(_medicalCaseData.Remark) ? "暂无" : _medicalCaseData.Remark;
        public string DiagnosisDisplay => _medicalCaseData.CaseStatus == MedicalCaseStatus.Closed ? "已完成" : "进行中";
        public string CreateTimeDisplay => "系统记录";
        public string CompleteTimeDisplay => "系统记录";

        // 状态显示
        public string StatusDisplay => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => "进行中",
            MedicalCaseStatus.Closed => "已关闭",
            _ => "未知状态"
        };

        public string StatusIcon => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => "▶",
            MedicalCaseStatus.Closed => "✔",
            _ => "?"
        };

        public string StatusColorCode => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => "#007ACC",
            MedicalCaseStatus.Closed => "#4CAF50",
            _ => "#9E9E9E"
        };

        // 时间相关
        public string DurationDisplay => "--";
        public string RelativeTimeDisplay => "--";
        public string EstimatedCompleteTimeDisplay => _medicalCaseData.CaseStatus == MedicalCaseStatus.Closed ? CompleteTimeDisplay : "预计完成";

        // 聚合显示
        public string PatientFullInfoDisplay => string.Join(" ", new[] { PatientNameDisplay, "普通患者" });
        public string PatientBriefInfoDisplay => $"{PatientNameDisplay} ({PatientGenderDisplay} {PatientAgeDisplay}岁)";

        // 优先级/紧急度（占位）
        public string PriorityDisplay => "--";
        public string PriorityIcon => "?";
        public string UrgencyDisplay => "--";
        public string UrgencyIcon => "Info";

        // 格式化输出
        public string GetSummaryInfo() => $"{PatientBriefInfoDisplay} | {StatusDisplay} | {DurationDisplay}";

        public string GetDetailedInfo()
        {
            return $"病例详细信息\n" +
                   $"--------------------------------\n" +
                   $"病例ID：{_medicalCaseData.Id}\n" +
                   $"患者：{PatientFullInfoDisplay}\n" +
                   $"医生：{DoctorNameDisplay}\n" +
                   $"主诉：{ChiefComplaintDisplay}\n" +
                   $"诊断：{DiagnosisDisplay}\n" +
                   $"状态：{StatusDisplay}\n" +
                   $"优先级：{PriorityDisplay}\n" +
                   $"紧急度：{UrgencyDisplay}\n" +
                   $"创建时间：{CreateTimeDisplay}\n" +
                   $"完成时间：{CompleteTimeDisplay}\n" +
                   $"时长：{DurationDisplay}\n" +
                   $"--------------------------------";
        }

        public string GetPrintableInfo()
        {
            return $"病例记录\n" +
                   $"--------------------------------\n" +
                   $"编号：{_medicalCaseData.Id}\n" +
                   $"患者：{PatientNameDisplay}\n" +
                   $"性别：{PatientGenderDisplay}\n" +
                   $"年龄：{PatientAgeDisplay}岁\n" +
                   $"主治医生：{DoctorNameDisplay}\n" +
                   $"创建时间：{CreateTimeDisplay}\n" +
                   $"--------------------------------\n" +
                   $"主诉：{ChiefComplaintDisplay}\n" +
                   $"诊断：{DiagnosisDisplay}\n" +
                   $"状态：{StatusDisplay}\n" +
                   $"完成时间：{CompleteTimeDisplay}\n" +
                   $"--------------------------------";
        }

        public string GetStatusBadge() => StatusDisplay;

        public string GetProgressDisplay() => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => "进行中",
            MedicalCaseStatus.Closed => "已关闭",
            _ => "状态未知"
        };

        public string GetActionSuggestion() => _medicalCaseData.CaseStatus == MedicalCaseStatus.Active ? "结束就诊" : "查看详情";

        public string GetAvailableActions()
        {
            var actions = new List<string>();
            if (_medicalCaseData.CaseStatus == MedicalCaseStatus.Active)
            {
                actions.Add("结束就诊");
            }
            if (_medicalCaseData.CaseStatus != MedicalCaseStatus.Closed)
            {
                actions.Add("编辑信息");
            }
            return actions.Count > 0 ? string.Join("、", actions) : "无可用操作";
        }
    }
}
