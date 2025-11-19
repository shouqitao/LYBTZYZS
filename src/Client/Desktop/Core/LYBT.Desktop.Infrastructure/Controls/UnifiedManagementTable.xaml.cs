using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 统一管理表格组件
    /// 提供统一的DataGrid样式和行为
    /// Issue #1840 - Desktop端管理界面UI统一化
    /// Issue #2160 - 添加全选功能和快捷键支持
    /// </summary>
    public partial class UnifiedManagementTable : UserControl
    {
        private CheckBox? _selectAllCheckBox; // Issue #2160: 表头全选CheckBox引用

        public UnifiedManagementTable()
        {
            InitializeComponent();
            DataGrid.SelectionChanged += DataGrid_SelectionChanged;
            DataGrid.Loaded += DataGrid_Loaded;

            // Issue #2160: 初始化全选命令
            SelectAllCommand = new RelayCommand(ExecuteSelectAll);
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

            // Issue #2160: 更新全选CheckBox的三态状态
            UpdateSelectAllCheckBoxState();
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

        /// <summary>
        /// 全选命令
        /// Issue #2160: 支持Ctrl+A快捷键全选
        /// </summary>
        public ICommand SelectAllCommand
        {
            get => (ICommand)GetValue(SelectAllCommandProperty);
            private set => SetValue(SelectAllCommandProperty, value);
        }

        public static readonly DependencyProperty SelectAllCommandProperty =
            DependencyProperty.Register(
                nameof(SelectAllCommand),
                typeof(ICommand),
                typeof(UnifiedManagementTable),
                new PropertyMetadata(null));

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
        /// Issue #2160 - Task 4.1: 添加表头全选CheckBox
        /// </summary>
        private void AddCheckBoxColumn()
        {
            if (DataGrid == null)
                return;

            // 检查是否已添加checkbox列
            var existingColumn = DataGrid.Columns.FirstOrDefault(c => c is DataGridCheckBoxColumn);
            if (existingColumn != null)
                return;

            // Issue #2160: 创建表头全选CheckBox
            _selectAllCheckBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = true, // 支持三态：全选/部分选中/未选中
                ToolTip = "全选/取消全选"
            };
            _selectAllCheckBox.Checked += SelectAllCheckBox_Changed;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Changed;

            // 创建CheckBox列
            var checkBoxColumn = new DataGridCheckBoxColumn
            {
                Header = _selectAllCheckBox,  // Issue #2160: 表头为全选CheckBox
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

        /// <summary>
        /// 表头全选CheckBox状态变更事件
        /// Issue #2160: 同步表头CheckBox状态到DataGrid选择
        /// </summary>
        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectAllCheckBox == null || DataGrid == null)
                return;

            // 避免循环触发: 如果是UpdateSelectAllCheckBoxState触发的变更,不处理
            if (_isUpdatingCheckBoxState)
                return;

            _isSelectingFromCheckBox = true;

            if (_selectAllCheckBox.IsChecked == true)
            {
                DataGrid.SelectAll();
            }
            else if (_selectAllCheckBox.IsChecked == false)
            {
                DataGrid.UnselectAll();
            }

            _isSelectingFromCheckBox = false;
        }

        /// <summary>
        /// 更新表头全选CheckBox的三态状态
        /// Issue #2160: 根据当前选择项更新CheckBox状态(全选/部分选中/未选中)
        /// </summary>
        private void UpdateSelectAllCheckBoxState()
        {
            if (_selectAllCheckBox == null || DataGrid == null)
                return;

            // 避免循环触发
            if (_isSelectingFromCheckBox)
                return;

            var totalCount = DataGrid.Items.Count;
            if (totalCount == 0)
            {
                _isUpdatingCheckBoxState = true;
                _selectAllCheckBox.IsChecked = false;
                _isUpdatingCheckBoxState = false;
                return;
            }

            var selectedCount = DataGrid.SelectedItems.Count;

            _isUpdatingCheckBoxState = true;

            if (selectedCount == 0)
            {
                _selectAllCheckBox.IsChecked = false; // 未选中
            }
            else if (selectedCount == totalCount)
            {
                _selectAllCheckBox.IsChecked = true; // 全选
            }
            else
            {
                _selectAllCheckBox.IsChecked = null; // 部分选中(Indeterminate)
            }

            _isUpdatingCheckBoxState = false;
        }

        /// <summary>
        /// 执行全选/取消全选命令
        /// Issue #2160: Ctrl+A快捷键触发
        /// </summary>
        private void ExecuteSelectAll()
        {
            if (DataGrid == null || DataGrid.Items.Count == 0)
                return;

            // 切换逻辑: 如果全选则取消全选,否则全选
            if (DataGrid.SelectedItems.Count == DataGrid.Items.Count)
            {
                DataGrid.UnselectAll();
            }
            else
            {
                DataGrid.SelectAll();
            }
        }

        #endregion

        #region 辅助字段

        // Issue #2160: 防止循环触发的标志
        private bool _isUpdatingCheckBoxState = false; // 正在更新CheckBox状态
        private bool _isSelectingFromCheckBox = false; // 正在从CheckBox选择

        #endregion
    }

    #region RelayCommand实现

    /// <summary>
    /// 简单的ICommand实现
    /// Issue #2160: 用于SelectAllCommand
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    #endregion
}
