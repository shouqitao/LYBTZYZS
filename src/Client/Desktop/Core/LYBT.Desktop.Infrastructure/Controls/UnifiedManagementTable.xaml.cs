using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            DataGrid.SelectionChanged += DataGrid_SelectionChanged;
            DataGrid.Loaded += DataGrid_Loaded;
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // 同步SelectedItems初始状态
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                DataGrid.SelectedItems.Clear();
                foreach (var item in SelectedItems)
                {
                    DataGrid.SelectedItems.Add(item);
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 同步DataGrid的选中项到ViewModel
            if (SelectedItems != null)
            {
                SelectedItems.Clear();
                foreach (var item in DataGrid.SelectedItems)
                {
                    SelectedItems.Add(item);
                }
            }
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

        /// <summary>
        /// 选中项集合（批量选择）
        /// </summary>
        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(
                nameof(SelectedItems),
                typeof(IList),
                typeof(UnifiedManagementTable),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemsChanged));

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UnifiedManagementTable)d;
            if (control.DataGrid != null && e.NewValue is IList newList)
            {
                control.DataGrid.SelectionChanged -= control.DataGrid_SelectionChanged;
                control.DataGrid.SelectedItems.Clear();
                foreach (var item in newList)
                {
                    control.DataGrid.SelectedItems.Add(item);
                }
                control.DataGrid.SelectionChanged += control.DataGrid_SelectionChanged;
            }
        }

        /// <summary>
        /// 是否显示CheckBox选择列
        /// </summary>
        public bool ShowCheckBoxColumn
        {
            get => (bool)GetValue(ShowCheckBoxColumnProperty);
            set => SetValue(ShowCheckBoxColumnProperty, value);
        }

        public static readonly DependencyProperty ShowCheckBoxColumnProperty =
            DependencyProperty.Register(
                nameof(ShowCheckBoxColumn),
                typeof(bool),
                typeof(UnifiedManagementTable),
                new PropertyMetadata(false, OnShowCheckBoxColumnChanged));

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取DataGrid的列集合
        /// 允许在XAML中定义DataGrid列
        /// Issue #2011: 添加 null 检查，防止在视觉树构建期间访问未初始化的 DataGrid
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns
        {
            get
            {
                // 确保 DataGrid 已经在视觉树中初始化
                if (DataGrid == null)
                {
                    return new System.Collections.ObjectModel.ObservableCollection<DataGridColumn>();
                }
                return DataGrid.Columns;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// ShowCheckBoxColumn属性变更时触发
        /// Issue #2150 - Task 1.1: 批量删除功能 - checkbox列动态添加
        /// </summary>
        private static void OnShowCheckBoxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UnifiedManagementTable)d;
            var showCheckBox = (bool)e.NewValue;

            if (showCheckBox)
            {
                control.AddCheckBoxColumn();
            }
            else
            {
                control.RemoveCheckBoxColumn();
            }
        }

        /// <summary>
        /// 添加CheckBox选择列到DataGrid第一列
        /// Issue #2150 - Task 1.1: 批量删除功能
        /// </summary>
        private void AddCheckBoxColumn()
        {
            if (DataGrid == null)
                return;

            // 检查是否已添加checkbox列
            var existingColumn = DataGrid.Columns.FirstOrDefault(c => c is DataGridCheckBoxColumn);
            if (existingColumn != null)
                return;

            // 创建CheckBox列
            var checkBoxColumn = new DataGridCheckBoxColumn
            {
                Header = "",  // 表头空白，后续可添加全选checkbox
                Width = new DataGridLength(40),
                CanUserResize = false,
                CanUserSort = false,
                DisplayIndex = 0  // 确保在第一列
            };

            // 绑定到DataGridRow.IsSelected（WPF内置属性）
            var binding = new Binding("IsSelected")
            {
                RelativeSource = new RelativeSource(
                    RelativeSourceMode.FindAncestor,
                    typeof(DataGridRow),
                    1),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            checkBoxColumn.Binding = binding;

            // 插入到第一列
            DataGrid.Columns.Insert(0, checkBoxColumn);
        }

        /// <summary>
        /// 移除CheckBox选择列
        /// Issue #2150 - Task 1.1: 批量删除功能
        /// </summary>
        private void RemoveCheckBoxColumn()
        {
            if (DataGrid == null)
                return;

            var checkBoxColumn = DataGrid.Columns.FirstOrDefault(c => c is DataGridCheckBoxColumn);
            if (checkBoxColumn != null)
            {
                DataGrid.Columns.Remove(checkBoxColumn);
            }
        }

        #endregion
    }
}
