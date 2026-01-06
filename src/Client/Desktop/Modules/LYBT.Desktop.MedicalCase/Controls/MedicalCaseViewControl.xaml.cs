using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Modules.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.Controls;

/// <summary>
/// 医案预览控件 - 查看模式
/// OpenSpec: refactor-medicalcase-workspace V2
/// OpenSpec: refactor-medicalcase-management
///
/// 支持两种显示模式:
/// - Full (默认): MasterDetail场景，完整的InfoCard布局
/// - Compact: Workspace场景，简化的只读预览
/// </summary>
public partial class MedicalCaseViewControl : UserControl
{
    public MedicalCaseViewControl()
    {
        InitializeComponent();
    }

    #region 显示模式

    /// <summary>
    /// 是否为紧凑模式 (Workspace场景)
    /// </summary>
    public static readonly DependencyProperty IsCompactModeProperty =
        DependencyProperty.Register(nameof(IsCompactMode), typeof(bool), typeof(MedicalCaseViewControl),
            new PropertyMetadata(false));

    public bool IsCompactMode
    {
        get => (bool)GetValue(IsCompactModeProperty);
        set => SetValue(IsCompactModeProperty, value);
    }

    #endregion

    #region Detail 依赖属性 - Full模式

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(
            nameof(Detail),
            typeof(MedicalCaseDetailModel),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(null));

    /// <summary>医案详情数据 - Full模式使用</summary>
    public MedicalCaseDetailModel? Detail
    {
        get => (MedicalCaseDetailModel?)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    #endregion

    #region 诊断信息 - Compact模式

    public static readonly DependencyProperty PresentIllnessProperty =
        DependencyProperty.Register(nameof(PresentIllness), typeof(string), typeof(MedicalCaseViewControl));

    public string? PresentIllness
    {
        get => (string?)GetValue(PresentIllnessProperty);
        set => SetValue(PresentIllnessProperty, value);
    }

    public static readonly DependencyProperty TongueDiagnosisProperty =
        DependencyProperty.Register(nameof(TongueDiagnosis), typeof(string), typeof(MedicalCaseViewControl));

    public string? TongueDiagnosis
    {
        get => (string?)GetValue(TongueDiagnosisProperty);
        set => SetValue(TongueDiagnosisProperty, value);
    }

    public static readonly DependencyProperty PulseDiagnosisProperty =
        DependencyProperty.Register(nameof(PulseDiagnosis), typeof(string), typeof(MedicalCaseViewControl));

    public string? PulseDiagnosis
    {
        get => (string?)GetValue(PulseDiagnosisProperty);
        set => SetValue(PulseDiagnosisProperty, value);
    }

    public static readonly DependencyProperty TcmDiagnosisProperty =
        DependencyProperty.Register(nameof(TcmDiagnosis), typeof(string), typeof(MedicalCaseViewControl));

    public string? TcmDiagnosis
    {
        get => (string?)GetValue(TcmDiagnosisProperty);
        set => SetValue(TcmDiagnosisProperty, value);
    }

    #endregion

    #region 处方信息 - Compact模式

    /// <summary>
    /// 药材列表 - Compact模式使用HerbListControl
    /// </summary>
    public static readonly DependencyProperty HerbItemsProperty =
        DependencyProperty.Register(nameof(HerbItems), typeof(IEnumerable), typeof(MedicalCaseViewControl));

    public IEnumerable? HerbItems
    {
        get => (IEnumerable?)GetValue(HerbItemsProperty);
        set => SetValue(HerbItemsProperty, value);
    }

    /// <summary>
    /// 所有可用药材列表
    /// </summary>
    public static readonly DependencyProperty AllHerbsProperty =
        DependencyProperty.Register(nameof(AllHerbs), typeof(IEnumerable), typeof(MedicalCaseViewControl));

    public IEnumerable? AllHerbs
    {
        get => (IEnumerable?)GetValue(AllHerbsProperty);
        set => SetValue(AllHerbsProperty, value);
    }

    /// <summary>
    /// 剂数
    /// </summary>
    public static readonly DependencyProperty DoseCountProperty =
        DependencyProperty.Register(nameof(DoseCount), typeof(int?), typeof(MedicalCaseViewControl));

    public int? DoseCount
    {
        get => (int?)GetValue(DoseCountProperty);
        set => SetValue(DoseCountProperty, value);
    }

    /// <summary>
    /// 用法
    /// </summary>
    public static readonly DependencyProperty UsageProperty =
        DependencyProperty.Register(nameof(Usage), typeof(string), typeof(MedicalCaseViewControl));

    public string? Usage
    {
        get => (string?)GetValue(UsageProperty);
        set => SetValue(UsageProperty, value);
    }

    /// <summary>
    /// 总价
    /// </summary>
    public static readonly DependencyProperty TotalPriceProperty =
        DependencyProperty.Register(nameof(TotalPrice), typeof(decimal), typeof(MedicalCaseViewControl),
            new PropertyMetadata(0m));

    public decimal TotalPrice
    {
        get => (decimal)GetValue(TotalPriceProperty);
        set => SetValue(TotalPriceProperty, value);
    }

    #endregion

    #region 打印按钮 - Compact模式

    /// <summary>
    /// 是否显示打印按钮
    /// </summary>
    public static readonly DependencyProperty ShowPrintButtonProperty =
        DependencyProperty.Register(nameof(ShowPrintButton), typeof(bool), typeof(MedicalCaseViewControl),
            new PropertyMetadata(false));

    public bool ShowPrintButton
    {
        get => (bool)GetValue(ShowPrintButtonProperty);
        set => SetValue(ShowPrintButtonProperty, value);
    }

    /// <summary>
    /// 打印命令
    /// </summary>
    public static readonly DependencyProperty PrintCommandProperty =
        DependencyProperty.Register(nameof(PrintCommand), typeof(ICommand), typeof(MedicalCaseViewControl));

    public ICommand? PrintCommand
    {
        get => (ICommand?)GetValue(PrintCommandProperty);
        set => SetValue(PrintCommandProperty, value);
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

    #region ShowAuditInfo 依赖属性 - Full模式

    public static readonly DependencyProperty ShowAuditInfoProperty =
        DependencyProperty.Register(
            nameof(ShowAuditInfo),
            typeof(bool),
            typeof(MedicalCaseViewControl),
            new PropertyMetadata(true));

    /// <summary>是否显示审计信息 - Full模式</summary>
    public bool ShowAuditInfo
    {
        get => (bool)GetValue(ShowAuditInfoProperty);
        set => SetValue(ShowAuditInfoProperty, value);
    }

    #endregion
}
