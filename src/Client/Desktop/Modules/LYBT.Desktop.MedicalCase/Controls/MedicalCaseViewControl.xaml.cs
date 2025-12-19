using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Models.Items.MedicalCases;
using LYBT.Desktop.Modules.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.Controls;

/// <summary>
/// 医案预览控件 - 查看模式
/// OpenSpec: refactor-medicalcase-management
/// </summary>
public partial class MedicalCaseViewControl : UserControl
{
    public MedicalCaseViewControl()
    {
        InitializeComponent();
    }

    #region Detail 依赖属性

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(
            nameof(Detail),
            typeof(MedicalCaseDetailModel),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(null));

    /// <summary>医案详情数据</summary>
    public MedicalCaseDetailModel? Detail
    {
        get => (MedicalCaseDetailModel?)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    #endregion

    #region MedicalCaseDetail 依赖属性（向后兼容）

    public static readonly DependencyProperty MedicalCaseDetailProperty =
        DependencyProperty.Register(
            nameof(MedicalCaseDetail),
            typeof(MedicalCaseDetailModel),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(null, OnMedicalCaseDetailChanged));

    /// <summary>医案详情数据（向后兼容，等同于Detail）</summary>
    public MedicalCaseDetailModel? MedicalCaseDetail
    {
        get => (MedicalCaseDetailModel?)GetValue(MedicalCaseDetailProperty);
        set => SetValue(MedicalCaseDetailProperty, value);
    }

    private static void OnMedicalCaseDetailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MedicalCaseViewControl control)
        {
            // 同步到Detail属性
            control.Detail = e.NewValue as MedicalCaseDetailModel;
        }
    }

    #endregion

    #region HasConsultation 依赖属性（向后兼容）

    public static readonly DependencyProperty HasConsultationProperty =
        DependencyProperty.Register(
            nameof(HasConsultation),
            typeof(bool),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(false));

    /// <summary>是否有诊断信息（向后兼容，供外部绑定使用）</summary>
    public bool HasConsultation
    {
        get => (bool)GetValue(HasConsultationProperty);
        set => SetValue(HasConsultationProperty, value);
    }

    #endregion

    #region HasPrescription 依赖属性（向后兼容）

    public static readonly DependencyProperty HasPrescriptionProperty =
        DependencyProperty.Register(
            nameof(HasPrescription),
            typeof(bool),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(false));

    /// <summary>是否有处方信息（向后兼容，供外部绑定使用）</summary>
    public bool HasPrescription
    {
        get => (bool)GetValue(HasPrescriptionProperty);
        set => SetValue(HasPrescriptionProperty, value);
    }

    #endregion

    #region ShowAuditInfo 依赖属性

    public static readonly DependencyProperty ShowAuditInfoProperty =
        DependencyProperty.Register(
            nameof(ShowAuditInfo),
            typeof(bool),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(true));

    /// <summary>是否显示审计信息</summary>
    public bool ShowAuditInfo
    {
        get => (bool)GetValue(ShowAuditInfoProperty);
        set => SetValue(ShowAuditInfoProperty, value);
    }

    #endregion
}
