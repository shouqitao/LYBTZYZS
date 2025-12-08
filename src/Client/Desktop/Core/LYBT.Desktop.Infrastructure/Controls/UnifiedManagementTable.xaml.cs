using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>统一管理表格组件 - 提供统一的DataGrid样式、全选功能和快捷键支持</summary>
    public partial class UnifiedManagementTable : UserControl
    {
        private CheckBox? _selectAllCheckBox;
        private bool _isUpdatingCheckBoxState = false;
        private bool _isSelectingFromCheckBox = false;

        public UnifiedManagementTable()
        {
            InitializeComponent();
            DataGrid.SelectionChanged += DataGrid_SelectionChanged;
            DataGrid.Loaded += DataGrid_Loaded;
            SelectAllCommand = new RelayCommand(ExecuteSelectAll);
        }

        public IEnumerable ItemsSource { get => (IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(UnifiedManagementTable), new PropertyMetadata(null));

        public object SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(UnifiedManagementTable), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool ShowEmptyState { get => (bool)GetValue(ShowEmptyStateProperty); set => SetValue(ShowEmptyStateProperty, value); }
        public static readonly DependencyProperty ShowEmptyStateProperty = DependencyProperty.Register(nameof(ShowEmptyState), typeof(bool), typeof(UnifiedManagementTable), new PropertyMetadata(true));

        public string EmptyStateText { get => (string)GetValue(EmptyStateTextProperty); set => SetValue(EmptyStateTextProperty, value); }
        public static readonly DependencyProperty EmptyStateTextProperty = DependencyProperty.Register(nameof(EmptyStateText), typeof(string), typeof(UnifiedManagementTable), new PropertyMetadata("暂无数据"));

        public IList SelectedItems { get => (IList)GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(UnifiedManagementTable), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

        public bool ShowCheckBoxColumn { get => (bool)GetValue(ShowCheckBoxColumnProperty); set => SetValue(ShowCheckBoxColumnProperty, value); }
        public static readonly DependencyProperty ShowCheckBoxColumnProperty = DependencyProperty.Register(nameof(ShowCheckBoxColumn), typeof(bool), typeof(UnifiedManagementTable), new PropertyMetadata(false, OnShowCheckBoxColumnChanged));

        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns => DataGrid == null ? new System.Collections.ObjectModel.ObservableCollection<DataGridColumn>() : DataGrid.Columns;

        public ICommand SelectAllCommand { get => (ICommand)GetValue(SelectAllCommandProperty); private set => SetValue(SelectAllCommandProperty, value); }
        public static readonly DependencyProperty SelectAllCommandProperty = DependencyProperty.Register(nameof(SelectAllCommand), typeof(ICommand), typeof(UnifiedManagementTable), new PropertyMetadata(null));

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (SelectedItems != null && SelectedItems.Count > 0)
            {
                DataGrid.SelectedItems.Clear();
                foreach (var item in SelectedItems) DataGrid.SelectedItems.Add(item);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 同步DataGrid.SelectedItems到绑定的SelectedItems集合
            SyncSelectedItems();
            UpdateSelectAllCheckBoxState();
        }

        private void SyncSelectedItems()
        {
            if (SelectedItems == null) return;

            // 使用增量更新避免Clear()触发CollectionChanged时HasSelection=false
            // 1. 移除不再选中的项
            var itemsToRemove = SelectedItems.Cast<object>().Where(item => !DataGrid.SelectedItems.Contains(item)).ToList();
            foreach (var item in itemsToRemove)
            {
                SelectedItems.Remove(item);
            }

            // 2. 添加新选中的项
            foreach (var item in DataGrid.SelectedItems)
            {
                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }
        }

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UnifiedManagementTable)d;
            if (control.DataGrid != null && e.NewValue is IList newList)
            {
                control.DataGrid.SelectionChanged -= control.DataGrid_SelectionChanged;
                control.DataGrid.SelectedItems.Clear();
                foreach (var item in newList) control.DataGrid.SelectedItems.Add(item);
                control.DataGrid.SelectionChanged += control.DataGrid_SelectionChanged;
            }
        }

        private static void OnShowCheckBoxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (UnifiedManagementTable)d;
            if ((bool)e.NewValue) control.AddCheckBoxColumn();
            else control.RemoveCheckBoxColumn();
        }

        private void AddCheckBoxColumn()
        {
            if (DataGrid == null) return;
            var existingColumn = DataGrid.Columns.FirstOrDefault(c => c.Header is CheckBox);
            if (existingColumn != null) return;

            // 创建标题行checkbox - 透明背景
            _selectAllCheckBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = true,
                ToolTip = "全选/取消全选",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Gray
            };
            _selectAllCheckBox.Checked += SelectAllCheckBox_Changed;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Changed;

            // 使用DataGridTemplateColumn替代DataGridCheckBoxColumn，解决点击checkbox不选中行的问题
            var checkBoxColumn = new DataGridTemplateColumn
            {
                Header = _selectAllCheckBox,
                Width = new DataGridLength(40),
                CanUserResize = false,
                CanUserSort = false
            };

            // 创建CellTemplate - 使用Border包装checkbox，让整个单元格区域都响应点击
            var cellTemplate = new DataTemplate();

            // 外层Border - 填满单元格，处理点击事件
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            borderFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            borderFactory.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            // 在Border上处理点击事件，这样点击单元格任意位置都能触发选择
            borderFactory.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(RowCheckBox_PreviewMouseDown));
            borderFactory.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(RowCheckBox_PreviewMouseUp));
            borderFactory.AddHandler(Control.PreviewMouseDoubleClickEvent, new MouseButtonEventHandler(RowCheckBox_PreviewMouseDoubleClick));

            // 内层CheckBox - 绑定行选中状态
            var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
                Mode = BindingMode.OneWay // 只读绑定，选择逻辑由事件处理
            });
            checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkBoxFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkBoxFactory.SetValue(CheckBox.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
            // 禁用Focusable和HitTest，让点击穿透到Border处理
            checkBoxFactory.SetValue(CheckBox.FocusableProperty, false);
            checkBoxFactory.SetValue(CheckBox.IsHitTestVisibleProperty, false);

            // 组装模板
            borderFactory.AppendChild(checkBoxFactory);
            cellTemplate.VisualTree = borderFactory;
            checkBoxColumn.CellTemplate = cellTemplate;

            // OpenSpec: optimize-module-list-ui - UI-020 CheckBox列标题和内容垂直水平居中对齐
            // 设置HeaderStyle - 只覆盖对齐和Padding，其他样式继承默认
            var baseHeaderStyle = Application.Current.TryFindResource("BaseDataGridColumnHeader") as Style;
            checkBoxColumn.HeaderStyle = new Style(typeof(DataGridColumnHeader), baseHeaderStyle)
            {
                Setters = {
                    new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                    new Setter(DataGridColumnHeader.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(0))
                }
            };

            // 设置CellStyle - 只设置对齐，不设置Background以保留选中状态的背景色
            checkBoxColumn.CellStyle = new Style(typeof(DataGridCell))
            {
                Setters = {
                    new Setter(DataGridCell.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                    new Setter(DataGridCell.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                    new Setter(DataGridCell.PaddingProperty, new Thickness(0)),
                    new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0))
                }
            };

            DataGrid.Columns.Insert(0, checkBoxColumn);
        }

        private void RemoveCheckBoxColumn()
        {
            if (DataGrid == null) return;
            var checkBoxColumn = DataGrid.Columns.FirstOrDefault(c => c.Header is CheckBox);
            if (checkBoxColumn != null) DataGrid.Columns.Remove(checkBoxColumn);
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_selectAllCheckBox == null || DataGrid == null || _isUpdatingCheckBoxState) return;
            _isSelectingFromCheckBox = true;
            if (_selectAllCheckBox.IsChecked == true) DataGrid.SelectAll();
            else if (_selectAllCheckBox.IsChecked == false) DataGrid.UnselectAll();
            _isSelectingFromCheckBox = false;
        }

        private void RowCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataGrid == null || sender is not FrameworkElement element) return;
            // 使用VisualTreeHelper查找父级DataGridRow
            var row = FindVisualParent<DataGridRow>(element);
            if (row == null || row.Item == null) return;

            // 记录当前状态和数据项
            var shouldSelect = !row.IsSelected;
            var dataItem = row.Item;

            // 使用Dispatcher延迟执行选择操作，避免与DataGrid内部选择逻辑冲突
            // 延迟到当前事件处理完成后执行，确保DataGrid状态稳定
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (DataGrid == null) return;
                try
                {
                    if (shouldSelect)
                    {
                        if (!DataGrid.SelectedItems.Contains(dataItem))
                            DataGrid.SelectedItems.Add(dataItem);
                    }
                    else
                    {
                        if (DataGrid.SelectedItems.Contains(dataItem))
                            DataGrid.SelectedItems.Remove(dataItem);
                    }
                    // 确保DataGrid获得焦点，显示活动选中状态（蓝色而非灰色）
                    DataGrid.Focus();
                }
                catch (InvalidOperationException)
                {
                    // 忽略集合修改异常，可能在虚拟化滚动时发生
                }
            }), System.Windows.Threading.DispatcherPriority.Input);

            // 标记事件已处理，阻止事件继续传播到DataGrid
            e.Handled = true;
        }

        private void RowCheckBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // 阻止MouseUp事件传播到DataGrid，防止DataGrid的选择逻辑干扰
            e.Handled = true;
        }

        private void RowCheckBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 阻止双击事件传播到DataGrid
            e.Handled = true;
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent) return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void UpdateSelectAllCheckBoxState()
        {
            if (_selectAllCheckBox == null || DataGrid == null || _isSelectingFromCheckBox) return;
            var totalCount = DataGrid.Items.Count;
            _isUpdatingCheckBoxState = true;
            if (totalCount == 0) _selectAllCheckBox.IsChecked = false;
            else
            {
                var selectedCount = DataGrid.SelectedItems.Count;
                _selectAllCheckBox.IsChecked = selectedCount == 0 ? false : selectedCount == totalCount ? true : null;
            }
            _isUpdatingCheckBoxState = false;
        }

        private void ExecuteSelectAll()
        {
            if (DataGrid == null || DataGrid.Items.Count == 0) return;
            if (DataGrid.SelectedItems.Count == DataGrid.Items.Count) DataGrid.UnselectAll();
            else DataGrid.SelectAll();
        }
    }

    /// <summary>简单的ICommand实现</summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null) { _execute = execute ?? throw new ArgumentNullException(nameof(execute)); _canExecute = canExecute; }
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }
}
