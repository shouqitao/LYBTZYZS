using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 工作区状态显示组件
/// OpenSpec: slim-medicalcase-workspace-viewmodel
/// 遵循SRP原则，专注于状态计算和显示
/// </summary>
public partial class WorkspaceStatusDisplay : ObservableObject
{
    #region 诊断状态

    [ObservableProperty]
    private string _consultationStatusText = string.Empty;

    [ObservableProperty]
    private Brush _consultationStatusColor = Brushes.Gray;

    #endregion

    #region 处方状态

    [ObservableProperty]
    private string _prescriptionStatusText = string.Empty;

    [ObservableProperty]
    private string _prescriptionStatusSummary = string.Empty;

    [ObservableProperty]
    private Brush _prescriptionStatusSummaryColor = Brushes.Gray;

    [ObservableProperty]
    private Brush _prescriptionStatusBackground = Brushes.Transparent;

    [ObservableProperty]
    private bool _showPrescriptionStatus;

    #endregion

    /// <summary>
    /// 更新诊断状态显示
    /// </summary>
    /// <param name="state">当前编辑状态</param>
    /// <param name="hasValidDiagnosis">是否有有效诊断</param>
    public void UpdateConsultationStatus(EditState state, bool hasValidDiagnosis)
    {
        (ConsultationStatusText, ConsultationStatusColor) = state switch
        {
            EditState.Editing when hasValidDiagnosis => ("已填写", new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))),  // Green
            EditState.Editing => ("编辑中", new SolidColorBrush(Color.FromRgb(0x38, 0x8B, 0xFD))),  // Blue
            EditState.ReadOnly => ("已完成", new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))), // Green
            _ => ("未知", Brushes.Gray)
        };
    }

    /// <summary>
    /// 更新诊断状态显示 (简化版)
    /// </summary>
    /// <param name="isCompleted">是否已完成</param>
    public void UpdateConsultationStatus(bool isCompleted)
    {
        ConsultationStatusText = isCompleted ? "已完成" : "未完成";
        ConsultationStatusColor = new SolidColorBrush(isCompleted
            ? Color.FromRgb(76, 175, 80)   // Green
            : Color.FromRgb(255, 152, 0)); // Orange
    }

    /// <summary>
    /// 更新处方状态显示
    /// </summary>
    /// <param name="itemCount">处方项数量</param>
    /// <param name="needsPrescription">是否需要处方</param>
    /// <param name="isCompleted">是否已完成</param>
    public void UpdatePrescriptionStatus(int itemCount, bool needsPrescription, bool isCompleted)
    {
        ShowPrescriptionStatus = needsPrescription || itemCount > 0;

        if (!needsPrescription)
        {
            PrescriptionStatusText = "不开处方";
            PrescriptionStatusSummary = string.Empty;
            PrescriptionStatusBackground = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)); // LightGray
            PrescriptionStatusSummaryColor = Brushes.Gray;
            return;
        }

        PrescriptionStatusText = itemCount > 0 ? $"{itemCount}味药材" : "未开方";
        PrescriptionStatusSummary = isCompleted ? "已完成" : "编辑中";
        PrescriptionStatusSummaryColor = isCompleted
            ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))  // Green
            : new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)); // Orange
        PrescriptionStatusBackground = itemCount > 0
            ? new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7))  // LightGreen
            : new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)); // LightYellow
    }


    /// <summary>
    /// 更新处方状态显示 (简化版 - 兼容现有调用)
    /// </summary>
    /// <param name="isCompleted">是否已完成</param>
    /// <param name="customText">自定义状态文本</param>
    public void UpdatePrescriptionStatus(bool isCompleted, string? customText = null)
    {
        ShowPrescriptionStatus = true;
        var color = isCompleted ? Color.FromRgb(76, 175, 80) : Color.FromRgb(158, 158, 158);
        PrescriptionStatusText = isCompleted ? "已完成" : (customText ?? "待开方");
        PrescriptionStatusBackground = new SolidColorBrush(color);
        PrescriptionStatusSummary = isCompleted ? "已开方" : (customText ?? "待开方");
        PrescriptionStatusSummaryColor = new SolidColorBrush(color);
    }

    /// <summary>
    /// 重置所有状态为默认值
    /// </summary>
    public void Reset()
    {
        ConsultationStatusText = string.Empty;
        ConsultationStatusColor = Brushes.Gray;
        PrescriptionStatusText = string.Empty;
        PrescriptionStatusSummary = string.Empty;
        PrescriptionStatusSummaryColor = Brushes.Gray;
        PrescriptionStatusBackground = Brushes.Transparent;
        ShowPrescriptionStatus = false;
    }
}
