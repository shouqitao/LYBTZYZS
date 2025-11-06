using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 统一管理表格组件
    /// 提供统一的DataGrid样式和行为
    /// Issue #1840 - Desktop端管理界面UI统一化
    /// </summary>
    public partial class UnifiedManagementTable : UserControl
    {
        public UnifiedManagementTable()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 数据源
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(UnifiedManagementTable),
                new PropertyMetadata(null));

        /// <summary>
        /// 选中项
        /// </summary>
        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(UnifiedManagementTable),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 是否显示空状态提示
        /// </summary>
        public bool ShowEmptyState
        {
            get => (bool)GetValue(ShowEmptyStateProperty);
            set => SetValue(ShowEmptyStateProperty, value);
        }

        public static readonly DependencyProperty ShowEmptyStateProperty =
            DependencyProperty.Register(
                nameof(ShowEmptyState),
                typeof(bool),
                typeof(UnifiedManagementTable),
                new PropertyMetadata(true));

        /// <summary>
        /// 空状态提示文本
        /// </summary>
        public string EmptyStateText
        {
            get => (string)GetValue(EmptyStateTextProperty);
            set => SetValue(EmptyStateTextProperty, value);
        }

        public static readonly DependencyProperty EmptyStateTextProperty =
            DependencyProperty.Register(
                nameof(EmptyStateText),
                typeof(string),
                typeof(UnifiedManagementTable),
                new PropertyMetadata("暂无数据"));

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取DataGrid的列集合
        /// 允许在XAML中定义DataGrid列
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns => DataGrid.Columns;

        #endregion
    }
}
