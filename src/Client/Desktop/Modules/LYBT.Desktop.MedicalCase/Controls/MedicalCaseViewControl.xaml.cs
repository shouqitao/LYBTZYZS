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

    #region 诊断信息 - Compact模式 (对象绑定)

    /// <summary>
    /// 诊断数据对象 - 对象化绑定
    /// OpenSpec: unify-control-data-binding
    /// 替代原有 PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis 四个分散属性
    /// 类型为object以支持ConsultationItem等具有相同属性的类型
    /// </summary>
    public static readonly DependencyProperty ConsultationProperty =
        DependencyProperty.Register(nameof(Consultation), typeof(object), typeof(MedicalCaseViewControl),
            new PropertyMetadata(null));

    public object? Consultation
    {
        get => GetValue(ConsultationProperty);
        set => SetValue(ConsultationProperty, value);
    }

    #endregion

    #region 处方信息 - Compact模式 (对象绑定)

    /// <summary>
    /// 处方数据对象 - 对象化绑定
    /// OpenSpec: unify-control-data-binding
    /// 替代原有 HerbItems, DoseCount, Usage, TotalPrice 等分散属性
    /// 类型为object以支持PrescriptionItem等具有相同属性的类型
    /// </summary>
    public static readonly DependencyProperty PrescriptionProperty =
        DependencyProperty.Register(nameof(Prescription), typeof(object), typeof(MedicalCaseViewControl),
            new PropertyMetadata(null));

    public object? Prescription
    {
        get => GetValue(PrescriptionProperty);
        set => SetValue(PrescriptionProperty, value);
    }

    /// <summary>
    /// 所有可用药材列表 - HerbListControl需要
    /// </summary>
    public static readonly DependencyProperty AllHerbsProperty =
        DependencyProperty.Register(nameof(AllHerbs), typeof(IEnumerable), typeof(MedicalCaseViewControl));

    public IEnumerable? AllHerbs
    {
        get => (IEnumerable?)GetValue(AllHerbsProperty);
        set => SetValue(AllHerbsProperty, value);
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

    // OpenSpec: unify-control-data-binding - 已删除向后兼容属性 (MedicalCaseDetail, HasConsultation, HasPrescription)

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
