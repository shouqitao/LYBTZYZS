using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Controls;

/// <summary>
/// 医案编辑控件 - 编辑模式（已统一为Compact模式）
/// OpenSpec: refactor-medicalcase-workspace V2
/// OpenSpec: refactor-medicalcase-management
/// OpenSpec: unify-herb-controls-to-herbs-module
///
/// Compact模式: Workspace场景，显示患者信息+诊断+处方+工具栏
/// </summary>
public partial class MedicalCaseEditControl : UserControl
{
    public MedicalCaseEditControl()
    {
        InitializeComponent();
    }

    #region 患者信息（只读）

    public static readonly DependencyProperty PatientNameProperty =
        DependencyProperty.Register(nameof(PatientName), typeof(string), typeof(MedicalCaseEditControl));

    public string? PatientName
    {
        get => (string?)GetValue(PatientNameProperty);
        set => SetValue(PatientNameProperty, value);
    }

    public static readonly DependencyProperty ConsultationDateProperty =
        DependencyProperty.Register(nameof(ConsultationDate), typeof(DateTime), typeof(MedicalCaseEditControl));

    public DateTime ConsultationDate
    {
        get => (DateTime)GetValue(ConsultationDateProperty);
        set => SetValue(ConsultationDateProperty, value);
    }

    public static readonly DependencyProperty DoctorNameProperty =
        DependencyProperty.Register(nameof(DoctorName), typeof(string), typeof(MedicalCaseEditControl));

    public string? DoctorName
    {
        get => (string?)GetValue(DoctorNameProperty);
        set => SetValue(DoctorNameProperty, value);
    }

    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(nameof(Status), typeof(MedicalCaseStatus), typeof(MedicalCaseEditControl));

    public MedicalCaseStatus Status
    {
        get => (MedicalCaseStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    #endregion

    #region 诊断信息（可编辑）

    /// <summary>
    /// 诊断数据对象 - 强类型绑定
    /// OpenSpec: unify-control-data-binding
    /// OpenSpec: unify-medicalcase-item-editmodel - 统一使用 ConsultationItem
    /// 替代原有 PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis 四个分散属性
    /// </summary>
    public static readonly DependencyProperty ConsultationProperty =
        DependencyProperty.Register(nameof(Consultation), typeof(ConsultationItem), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public ConsultationItem? Consultation
    {
        get => (ConsultationItem?)GetValue(ConsultationProperty);
        set => SetValue(ConsultationProperty, value);
    }

    #endregion

    #region 处方信息（可编辑）

    /// <summary>
    /// 处方数据对象 - 强类型绑定
    /// OpenSpec: unify-control-data-binding
    /// OpenSpec: unify-medicalcase-item-editmodel - 统一使用 PrescriptionItem
    /// 替代原有 HerbCount, DoseCount, HerbItems, Usage, TotalPrice 等分散属性
    /// </summary>
    public static readonly DependencyProperty PrescriptionProperty =
        DependencyProperty.Register(nameof(Prescription), typeof(PrescriptionItem), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public PrescriptionItem? Prescription
    {
        get => (PrescriptionItem?)GetValue(PrescriptionProperty);
        set => SetValue(PrescriptionProperty, value);
    }

    /// <summary>
    /// 方源 - 处方来源描述
    /// </summary>
    public static readonly DependencyProperty FormulaSourceProperty =
        DependencyProperty.Register(nameof(FormulaSource), typeof(string), typeof(MedicalCaseEditControl));

    public string? FormulaSource
    {
        get => (string?)GetValue(FormulaSourceProperty);
        set => SetValue(FormulaSourceProperty, value);
    }

    /// <summary>
    /// 所有可用药材列表 - 用于HerbListControl药材选择
    /// </summary>
    public static readonly DependencyProperty AllHerbsProperty =
        DependencyProperty.Register(nameof(AllHerbs), typeof(IEnumerable), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public IEnumerable? AllHerbs
    {
        get => (IEnumerable?)GetValue(AllHerbsProperty);
        set => SetValue(AllHerbsProperty, value);
    }

    #endregion

    #region 处方区控制

    /// <summary>
    /// 是否启用处方区
    /// 诊断区不受此属性影响，始终可编辑
    /// </summary>
    public static readonly DependencyProperty IsPrescriptionEnabledProperty =
        DependencyProperty.Register(nameof(IsPrescriptionEnabled), typeof(bool), typeof(MedicalCaseEditControl),
            new PropertyMetadata(true));

    public bool IsPrescriptionEnabled
    {
        get => (bool)GetValue(IsPrescriptionEnabledProperty);
        set => SetValue(IsPrescriptionEnabledProperty, value);
    }

    /// <summary>
    /// 是否需要处方（处方决策）
    /// true=需要处方, false=不需要处方
    /// </summary>
    public static readonly DependencyProperty NeedsPrescriptionProperty =
        DependencyProperty.Register(nameof(NeedsPrescription), typeof(bool), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool NeedsPrescription
    {
        get => (bool)GetValue(NeedsPrescriptionProperty);
        set => SetValue(NeedsPrescriptionProperty, value);
    }

    #endregion

    #region 工具栏命令

    /// <summary>
    /// 导入经验方命令
    /// </summary>
    public static readonly DependencyProperty ImportFormulaCommandProperty =
        DependencyProperty.Register(nameof(ImportFormulaCommand), typeof(ICommand), typeof(MedicalCaseEditControl));

    public ICommand? ImportFormulaCommand
    {
        get => (ICommand?)GetValue(ImportFormulaCommandProperty);
        set => SetValue(ImportFormulaCommandProperty, value);
    }

    /// <summary>
    /// 导入历史处方命令
    /// </summary>
    public static readonly DependencyProperty ImportHistoryCommandProperty =
        DependencyProperty.Register(nameof(ImportHistoryCommand), typeof(ICommand), typeof(MedicalCaseEditControl));

    public ICommand? ImportHistoryCommand
    {
        get => (ICommand?)GetValue(ImportHistoryCommandProperty);
        set => SetValue(ImportHistoryCommandProperty, value);
    }

    /// <summary>
    /// 清空所有药材命令
    /// </summary>
    public static readonly DependencyProperty ClearAllCommandProperty =
        DependencyProperty.Register(nameof(ClearAllCommand), typeof(ICommand), typeof(MedicalCaseEditControl));

    public ICommand? ClearAllCommand
    {
        get => (ICommand?)GetValue(ClearAllCommandProperty);
        set => SetValue(ClearAllCommandProperty, value);
    }

    #endregion

    #region 系统信息（只读）

    public static readonly DependencyProperty CreatedAtProperty =
        DependencyProperty.Register(nameof(CreatedAt), typeof(DateTime), typeof(MedicalCaseEditControl));

    public DateTime CreatedAt
    {
        get => (DateTime)GetValue(CreatedAtProperty);
        set => SetValue(CreatedAtProperty, value);
    }

    public static readonly DependencyProperty UpdatedAtProperty =
        DependencyProperty.Register(nameof(UpdatedAt), typeof(DateTime?), typeof(MedicalCaseEditControl));

    public DateTime? UpdatedAt
    {
        get => (DateTime?)GetValue(UpdatedAtProperty);
        set => SetValue(UpdatedAtProperty, value);
    }

    #endregion

    #region 备注（可编辑）

    /// <summary>
    /// 备注 - 医案聚合根备注字段
    /// OpenSpec: unify-medicalcase-remark-source
    /// 绑定到 MedicalCaseDetailModel.Remark（聚合根），而非 PrescriptionItem.Remark
    /// </summary>
    public static readonly DependencyProperty RemarkProperty =
        DependencyProperty.Register(nameof(Remark), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? Remark
    {
        get => (string?)GetValue(RemarkProperty);
        set => SetValue(RemarkProperty, value);
    }

    #endregion

    #region 验证属性

    /// <summary>
    /// 验证错误源 - 用于显示验证错误消息
    /// OpenSpec: ui-validation-framework
    /// </summary>
    public static readonly DependencyProperty ErrorsSourceProperty =
        DependencyProperty.Register(
            nameof(ErrorsSource),
            typeof(ValidationErrorsAccessor),
            typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public ValidationErrorsAccessor? ErrorsSource
    {
        get => (ValidationErrorsAccessor?)GetValue(ErrorsSourceProperty);
        set => SetValue(ErrorsSourceProperty, value);
    }

    #endregion
}
