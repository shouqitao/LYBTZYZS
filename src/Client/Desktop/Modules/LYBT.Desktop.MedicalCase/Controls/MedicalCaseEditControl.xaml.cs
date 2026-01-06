using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Controls;

/// <summary>
/// 医案编辑控件 - 编辑模式
/// OpenSpec: refactor-medicalcase-workspace V2
/// OpenSpec: refactor-medicalcase-management
/// OpenSpec: unify-herb-controls-to-herbs-module
///
/// 支持两种显示模式:
/// - Full (默认): MasterDetail场景，显示患者信息、诊断、处方、备注、系统信息
/// - Compact: Workspace场景，仅显示诊断+处方+工具栏
/// </summary>
public partial class MedicalCaseEditControl : UserControl
{
    public MedicalCaseEditControl()
    {
        InitializeComponent();
    }

    #region 显示模式

    /// <summary>
    /// 是否为紧凑模式 (Workspace场景)
    /// True: 显示简化布局 (工具栏+诊断+处方)
    /// False: 显示完整布局 (患者信息+诊断+处方+备注+系统信息)
    /// </summary>
    public static readonly DependencyProperty IsCompactModeProperty =
        DependencyProperty.Register(nameof(IsCompactMode), typeof(bool), typeof(MedicalCaseEditControl),
            new PropertyMetadata(false));

    public bool IsCompactMode
    {
        get => (bool)GetValue(IsCompactModeProperty);
        set => SetValue(IsCompactModeProperty, value);
    }

    #endregion

    #region 患者信息（只读）- Full模式

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

    public static readonly DependencyProperty PresentIllnessProperty =
        DependencyProperty.Register(nameof(PresentIllness), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? PresentIllness
    {
        get => (string?)GetValue(PresentIllnessProperty);
        set => SetValue(PresentIllnessProperty, value);
    }

    public static readonly DependencyProperty TongueDiagnosisProperty =
        DependencyProperty.Register(nameof(TongueDiagnosis), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? TongueDiagnosis
    {
        get => (string?)GetValue(TongueDiagnosisProperty);
        set => SetValue(TongueDiagnosisProperty, value);
    }

    public static readonly DependencyProperty PulseDiagnosisProperty =
        DependencyProperty.Register(nameof(PulseDiagnosis), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? PulseDiagnosis
    {
        get => (string?)GetValue(PulseDiagnosisProperty);
        set => SetValue(PulseDiagnosisProperty, value);
    }

    public static readonly DependencyProperty TcmDiagnosisProperty =
        DependencyProperty.Register(nameof(TcmDiagnosis), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? TcmDiagnosis
    {
        get => (string?)GetValue(TcmDiagnosisProperty);
        set => SetValue(TcmDiagnosisProperty, value);
    }

    #endregion

    #region 处方信息（可编辑）

    public static readonly DependencyProperty HerbCountProperty =
        DependencyProperty.Register(nameof(HerbCount), typeof(int), typeof(MedicalCaseEditControl),
            new PropertyMetadata(0));

    public int HerbCount
    {
        get => (int)GetValue(HerbCountProperty);
        set => SetValue(HerbCountProperty, value);
    }

    public static readonly DependencyProperty DoseCountProperty =
        DependencyProperty.Register(nameof(DoseCount), typeof(int?), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public int? DoseCount
    {
        get => (int?)GetValue(DoseCountProperty);
        set => SetValue(DoseCountProperty, value);
    }

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

    /// <summary>
    /// 药材列表 - 用于HerbListControl编辑(双向绑定)
    /// </summary>
    public static readonly DependencyProperty HerbItemsProperty =
        DependencyProperty.Register(nameof(HerbItems), typeof(IEnumerable), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public IEnumerable? HerbItems
    {
        get => (IEnumerable?)GetValue(HerbItemsProperty);
        set => SetValue(HerbItemsProperty, value);
    }

    #endregion

    #region Compact模式专用属性

    /// <summary>
    /// 用法 - Compact模式编辑
    /// </summary>
    public static readonly DependencyProperty UsageProperty =
        DependencyProperty.Register(nameof(Usage), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? Usage
    {
        get => (string?)GetValue(UsageProperty);
        set => SetValue(UsageProperty, value);
    }

    /// <summary>
    /// 用法选项列表
    /// </summary>
    public static readonly DependencyProperty UsageOptionsProperty =
        DependencyProperty.Register(nameof(UsageOptions), typeof(IEnumerable), typeof(MedicalCaseEditControl));

    public IEnumerable? UsageOptions
    {
        get => (IEnumerable?)GetValue(UsageOptionsProperty);
        set => SetValue(UsageOptionsProperty, value);
    }

    /// <summary>
    /// 总价 - Compact模式显示
    /// </summary>
    public static readonly DependencyProperty TotalPriceProperty =
        DependencyProperty.Register(nameof(TotalPrice), typeof(decimal), typeof(MedicalCaseEditControl),
            new PropertyMetadata(0m));

    public decimal TotalPrice
    {
        get => (decimal)GetValue(TotalPriceProperty);
        set => SetValue(TotalPriceProperty, value);
    }

    #endregion

    #region 工具栏命令 - V2 Compact模式

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

    #region 兼容性属性（保留用于View模式）

    public static readonly DependencyProperty PrescriptionItemsProperty =
        DependencyProperty.Register(nameof(PrescriptionItems), typeof(ObservableCollection<PrescriptionItemDto>), typeof(MedicalCaseEditControl));

    public ObservableCollection<PrescriptionItemDto>? PrescriptionItems
    {
        get => (ObservableCollection<PrescriptionItemDto>?)GetValue(PrescriptionItemsProperty);
        set => SetValue(PrescriptionItemsProperty, value);
    }

    public static readonly DependencyProperty HasPrescriptionItemsProperty =
        DependencyProperty.Register(nameof(HasPrescriptionItems), typeof(bool), typeof(MedicalCaseEditControl));

    public bool HasPrescriptionItems
    {
        get => (bool)GetValue(HasPrescriptionItemsProperty);
        set => SetValue(HasPrescriptionItemsProperty, value);
    }

    #endregion

    #region 备注（可编辑）- Full模式

    public static readonly DependencyProperty RemarkProperty =
        DependencyProperty.Register(nameof(Remark), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? Remark
    {
        get => (string?)GetValue(RemarkProperty);
        set => SetValue(RemarkProperty, value);
    }

    #endregion

    #region 系统信息（只读）- Full模式

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
