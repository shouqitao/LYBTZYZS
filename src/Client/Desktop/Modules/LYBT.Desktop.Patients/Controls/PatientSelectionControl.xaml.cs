using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者选择控件
    /// OpenSpec: refactor-clinical-workflow
    ///
    /// 可复用的患者选择控件，使用Master-Detail布局
    /// - 左侧：患者列表（工具栏+搜索+列表）
    /// - 右侧：患者详情（使用PatientViewControl）
    ///
    /// 用于：Clinical患者选择界面、Reception挂号界面等
    /// </summary>
    public partial class PatientSelectionControl : UserControl
    {
        public PatientSelectionControl()
        {
            InitializeComponent();
        }

        #region 事件

        /// <summary>
        /// 患者双击事件（用于执行主操作，如开始看诊）
        /// </summary>
        public event EventHandler<PatientListDto>? PatientDoubleClicked;

        /// <summary>
        /// 双击处理
        /// </summary>
        private void PatientDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedPatient is PatientListDto patient)
            {
                PatientDoubleClicked?.Invoke(this, patient);

                // 同时执行SelectCommand（向后兼容）
                if (SelectCommand?.CanExecute(patient) == true)
                {
                    SelectCommand.Execute(patient);
                }
            }
        }

        #endregion

        #region Patients - 患者列表

        /// <summary>
        /// 患者列表数据源
        /// </summary>
        public IEnumerable Patients
        {
            get => (IEnumerable)GetValue(PatientsProperty);
            set => SetValue(PatientsProperty, value);
        }

        public static readonly DependencyProperty PatientsProperty =
            DependencyProperty.Register(nameof(Patients), typeof(IEnumerable), typeof(PatientSelectionControl),
                new PropertyMetadata(null));

        #endregion

        #region SelectedPatient - 选中的患者

        /// <summary>
        /// 当前选中的患者（列表项）
        /// </summary>
        public PatientListDto? SelectedPatient
        {
            get => (PatientListDto?)GetValue(SelectedPatientProperty);
            set => SetValue(SelectedPatientProperty, value);
        }

        public static readonly DependencyProperty SelectedPatientProperty =
            DependencyProperty.Register(nameof(SelectedPatient), typeof(PatientListDto), typeof(PatientSelectionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPatientChanged));

        private static void OnSelectedPatientChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PatientSelectionControl control)
            {
                control.HasSelection = e.NewValue != null;
            }
        }

        #endregion

        #region PatientDetail - 患者详情

        /// <summary>
        /// 患者详情数据（用于Detail区域显示）
        /// </summary>
        public PatientDetailDto? PatientDetail
        {
            get => (PatientDetailDto?)GetValue(PatientDetailProperty);
            set => SetValue(PatientDetailProperty, value);
        }

        public static readonly DependencyProperty PatientDetailProperty =
            DependencyProperty.Register(nameof(PatientDetail), typeof(PatientDetailDto), typeof(PatientSelectionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region HasSelection - 是否有选中项

        /// <summary>
        /// 是否有选中的患者
        /// </summary>
        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            private set => SetValue(HasSelectionPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HasSelectionPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasSelection), typeof(bool), typeof(PatientSelectionControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty HasSelectionProperty = HasSelectionPropertyKey.DependencyProperty;

        #endregion

        #region SearchText - 搜索文本

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(PatientSelectionControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Commands

        /// <summary>
        /// 新建患者命令
        /// </summary>
        public ICommand? CreateNewCommand
        {
            get => (ICommand?)GetValue(CreateNewCommandProperty);
            set => SetValue(CreateNewCommandProperty, value);
        }

        public static readonly DependencyProperty CreateNewCommandProperty =
            DependencyProperty.Register(nameof(CreateNewCommand), typeof(ICommand), typeof(PatientSelectionControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 刷新列表命令
        /// </summary>
        public ICommand? RefreshCommand
        {
            get => (ICommand?)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(PatientSelectionControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand? SearchCommand
        {
            get => (ICommand?)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(PatientSelectionControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 选择命令（双击时执行）
        /// </summary>
        public ICommand? SelectCommand
        {
            get => (ICommand?)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(PatientSelectionControl),
                new PropertyMetadata(null));

        #endregion

        #region State

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(PatientSelectionControl),
                new PropertyMetadata(false));

        #endregion
    }
}
