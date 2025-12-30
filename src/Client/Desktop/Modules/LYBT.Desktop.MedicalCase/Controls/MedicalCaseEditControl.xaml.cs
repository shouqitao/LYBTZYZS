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
/// OpenSpec: refactor-medicalcase-management
/// OpenSpec: unify-herb-list-controls - 统一使用HerbListEditor编辑处方
///
/// 可编辑字段：诊断信息（现病史、舌诊、脉诊、中医诊断）、处方药材、备注
/// 只读显示：患者信息、系统信息
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
    // OpenSpec: unify-herb-list-controls - 统一使用HerbListEditor编辑处方

    public static readonly DependencyProperty HerbCountProperty =
        DependencyProperty.Register(nameof(HerbCount), typeof(int), typeof(MedicalCaseEditControl),
            new PropertyMetadata(0));

    public int HerbCount
    {
        get => (int)GetValue(HerbCountProperty);
        set => SetValue(HerbCountProperty, value);
    }

    public static readonly DependencyProperty DoseCountProperty =
        DependencyProperty.Register(nameof(DoseCount), typeof(int?), typeof(MedicalCaseEditControl));

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
    /// 药材列表 - 用于HerbListEditor编辑
    /// </summary>
    public static readonly DependencyProperty HerbItemsProperty =
        DependencyProperty.Register(nameof(HerbItems), typeof(IEnumerable), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public IEnumerable? HerbItems
    {
        get => (IEnumerable?)GetValue(HerbItemsProperty);
        set => SetValue(HerbItemsProperty, value);
    }

    #endregion

    #region 处方编辑命令

    /// <summary>
    /// 删除药材命令
    /// </summary>
    public static readonly DependencyProperty DeleteHerbCommandProperty =
        DependencyProperty.Register(nameof(DeleteHerbCommand), typeof(ICommand), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public ICommand? DeleteHerbCommand
    {
        get => (ICommand?)GetValue(DeleteHerbCommandProperty);
        set => SetValue(DeleteHerbCommandProperty, value);
    }

    /// <summary>
    /// 剂量输入完成命令
    /// </summary>
    public static readonly DependencyProperty DosageCompletedCommandProperty =
        DependencyProperty.Register(nameof(DosageCompletedCommand), typeof(ICommand), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public ICommand? DosageCompletedCommand
    {
        get => (ICommand?)GetValue(DosageCompletedCommandProperty);
        set => SetValue(DosageCompletedCommandProperty, value);
    }

    /// <summary>
    /// 添加新行命令
    /// </summary>
    public static readonly DependencyProperty AddNewRowCommandProperty =
        DependencyProperty.Register(nameof(AddNewRowCommand), typeof(ICommand), typeof(MedicalCaseEditControl),
            new PropertyMetadata(null));

    public ICommand? AddNewRowCommand
    {
        get => (ICommand?)GetValue(AddNewRowCommandProperty);
        set => SetValue(AddNewRowCommandProperty, value);
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

    #region 备注（可编辑）

    public static readonly DependencyProperty RemarkProperty =
        DependencyProperty.Register(nameof(Remark), typeof(string), typeof(MedicalCaseEditControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? Remark
    {
        get => (string?)GetValue(RemarkProperty);
        set => SetValue(RemarkProperty, value);
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
