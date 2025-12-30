using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 患者搜索控件 - 从PatientSelectionView提取
    /// OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public partial class PatientSearchControl : UserControl
    {
        public PatientSearchControl() => InitializeComponent();

        #region Title - 标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PatientSearchControl),
                new PropertyMetadata("全部患者"));

        #endregion

        #region SearchKeyword - 搜索关键词

        public string SearchKeyword
        {
            get => (string)GetValue(SearchKeywordProperty);
            set => SetValue(SearchKeywordProperty, value);
        }

        public static readonly DependencyProperty SearchKeywordProperty =
            DependencyProperty.Register(nameof(SearchKeyword), typeof(string), typeof(PatientSearchControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Patients - 患者列表

        public IEnumerable Patients
        {
            get => (IEnumerable)GetValue(PatientsProperty);
            set => SetValue(PatientsProperty, value);
        }

        public static readonly DependencyProperty PatientsProperty =
            DependencyProperty.Register(nameof(Patients), typeof(IEnumerable), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        #endregion

        #region SelectedPatient - 选中患者

        public object SelectedPatient
        {
            get => GetValue(SelectedPatientProperty);
            set => SetValue(SelectedPatientProperty, value);
        }

        public static readonly DependencyProperty SelectedPatientProperty =
            DependencyProperty.Register(nameof(SelectedPatient), typeof(object), typeof(PatientSearchControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Commands

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 患者选中命令(双击/回车)
        /// </summary>
        public ICommand PatientSelectedCommand
        {
            get => (ICommand)GetValue(PatientSelectedCommandProperty);
            set => SetValue(PatientSelectedCommandProperty, value);
        }

        public static readonly DependencyProperty PatientSelectedCommandProperty =
            DependencyProperty.Register(nameof(PatientSelectedCommand), typeof(ICommand), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 新建患者命令
        /// </summary>
        public ICommand NewPatientCommand
        {
            get => (ICommand)GetValue(NewPatientCommandProperty);
            set => SetValue(NewPatientCommandProperty, value);
        }

        public static readonly DependencyProperty NewPatientCommandProperty =
            DependencyProperty.Register(nameof(NewPatientCommand), typeof(ICommand), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand
        {
            get => (ICommand)GetValue(PreviousPageCommandProperty);
            set => SetValue(PreviousPageCommandProperty, value);
        }

        public static readonly DependencyProperty PreviousPageCommandProperty =
            DependencyProperty.Register(nameof(PreviousPageCommand), typeof(ICommand), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand
        {
            get => (ICommand)GetValue(NextPageCommandProperty);
            set => SetValue(NextPageCommandProperty, value);
        }

        public static readonly DependencyProperty NextPageCommandProperty =
            DependencyProperty.Register(nameof(NextPageCommand), typeof(ICommand), typeof(PatientSearchControl),
                new PropertyMetadata(null));

        #endregion

        #region Display Options

        /// <summary>
        /// 是否显示新建患者按钮
        /// </summary>
        public bool ShowCreateButton
        {
            get => (bool)GetValue(ShowCreateButtonProperty);
            set => SetValue(ShowCreateButtonProperty, value);
        }

        public static readonly DependencyProperty ShowCreateButtonProperty =
            DependencyProperty.Register(nameof(ShowCreateButton), typeof(bool), typeof(PatientSearchControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否显示分页控件
        /// </summary>
        public bool ShowPagination
        {
            get => (bool)GetValue(ShowPaginationProperty);
            set => SetValue(ShowPaginationProperty, value);
        }

        public static readonly DependencyProperty ShowPaginationProperty =
            DependencyProperty.Register(nameof(ShowPagination), typeof(bool), typeof(PatientSearchControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否正在搜索
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(PatientSearchControl),
                new PropertyMetadata(false));

        #endregion

        #region Pagination

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PatientSearchControl),
                new PropertyMetadata(1));

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, value);
        }

        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PatientSearchControl),
                new PropertyMetadata(1));

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => (int)GetValue(TotalCountProperty);
            set => SetValue(TotalCountProperty, value);
        }

        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register(nameof(TotalCount), typeof(int), typeof(PatientSearchControl),
                new PropertyMetadata(0));

        #endregion
    }
}
