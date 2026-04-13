using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者编辑控件 - 对象 DP 绑定
    /// OpenSpec: frontend-architecture-unification
    ///
    /// 使用 PatientEditContext 对象 DP 替代扁平 DP
    /// 所有编辑字段通过 Patient 属性访问
    /// </summary>
    public partial class PatientEditControl : UserControl
    {
        public PatientEditControl()
        {
            InitializeComponent();
        }

        #region DependencyProperties

        /// <summary>
        /// 患者编辑上下文 (对象 DP — 唯一编辑真源)
        /// </summary>
        public static readonly DependencyProperty PatientProperty =
            DependencyProperty.Register(
                nameof(Patient),
                typeof(PatientEditContext),
                typeof(PatientEditControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public PatientEditContext? Patient
        {
            get => (PatientEditContext?)GetValue(PatientProperty);
            set => SetValue(PatientProperty, value);
        }

        /// <summary>
        /// 性别选项列表
        /// </summary>
        public static readonly DependencyProperty GenderOptionsProperty =
            DependencyProperty.Register(
                nameof(GenderOptions),
                typeof(ObservableCollection<Gender>),
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<Gender>? GenderOptions
        {
            get => (ObservableCollection<Gender>?)GetValue(GenderOptionsProperty);
            set => SetValue(GenderOptionsProperty, value);
        }

        /// <summary>
        /// 状态选项列表
        /// </summary>
        public static readonly DependencyProperty StatusOptionsProperty =
            DependencyProperty.Register(
                nameof(StatusOptions),
                typeof(ObservableCollection<CommonStatus>),
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public ObservableCollection<CommonStatus>? StatusOptions
        {
            get => (ObservableCollection<CommonStatus>?)GetValue(StatusOptionsProperty);
            set => SetValue(StatusOptionsProperty, value);
        }

        /// <summary>
        /// 是否显示状态字段
        /// </summary>
        public static readonly DependencyProperty ShowStatusProperty =
            DependencyProperty.Register(
                nameof(ShowStatus),
                typeof(bool),
                typeof(PatientEditControl),
                new PropertyMetadata(true));

        public bool ShowStatus
        {
            get => (bool)GetValue(ShowStatusProperty);
            set => SetValue(ShowStatusProperty, value);
        }

        /// <summary>
        /// 验证错误源 - 用于显示验证错误消息
        /// OpenSpec: ui-validation-framework
        /// </summary>
        public static readonly DependencyProperty ErrorsSourceProperty =
            DependencyProperty.Register(
                nameof(ErrorsSource),
                typeof(ValidationErrorsAccessor),
                typeof(PatientEditControl),
                new PropertyMetadata(null));

        public ValidationErrorsAccessor? ErrorsSource
        {
            get => (ValidationErrorsAccessor?)GetValue(ErrorsSourceProperty);
            set => SetValue(ErrorsSourceProperty, value);
        }

        #endregion
    }
}
