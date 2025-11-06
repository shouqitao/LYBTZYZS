using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 统一管理工具栏组件
    /// 提供搜索、筛选、操作按钮区域
    /// Issue #1840 - Desktop端管理界面UI统一化
    /// </summary>
    public partial class UnifiedManagementToolBar : UserControl
    {
        public UnifiedManagementToolBar()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(UnifiedManagementToolBar),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSearchTextChanged));

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 搜索文本变更时的处理逻辑（如果需要）
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(
                nameof(SearchCommand),
                typeof(ICommand),
                typeof(UnifiedManagementToolBar),
                new PropertyMetadata(null));

        /// <summary>
        /// 筛选内容区域
        /// 用于放置筛选控件（如ComboBox、DatePicker等）
        /// </summary>
        public object FilterContent
        {
            get => GetValue(FilterContentProperty);
            set => SetValue(FilterContentProperty, value);
        }

        public static readonly DependencyProperty FilterContentProperty =
            DependencyProperty.Register(
                nameof(FilterContent),
                typeof(object),
                typeof(UnifiedManagementToolBar),
                new PropertyMetadata(null));

        /// <summary>
        /// 操作按钮区域
        /// 用于放置操作按钮（如"新建"、"导入"、"导出"等）
        /// </summary>
        public object ActionButtons
        {
            get => GetValue(ActionButtonsProperty);
            set => SetValue(ActionButtonsProperty, value);
        }

        public static readonly DependencyProperty ActionButtonsProperty =
            DependencyProperty.Register(
                nameof(ActionButtons),
                typeof(object),
                typeof(UnifiedManagementToolBar),
                new PropertyMetadata(null));

        #endregion
    }
}
