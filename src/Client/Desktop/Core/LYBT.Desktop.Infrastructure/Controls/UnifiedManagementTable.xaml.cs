using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
            if (SelectedItems != null)
            {
                SelectedItems.Clear();
                foreach (var item in DataGrid.SelectedItems) SelectedItems.Add(item);
            }
            UpdateSelectAllCheckBoxState();
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
            var existingColumn = DataGrid.Columns.FirstOrDefault(c => c is DataGridCheckBoxColumn);
            if (existingColumn != null) return;

            _selectAllCheckBox = new CheckBox { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsThreeState = true, ToolTip = "全选/取消全选" };
            _selectAllCheckBox.Checked += SelectAllCheckBox_Changed;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Changed;

            var checkBoxColumn = new DataGridCheckBoxColumn { Header = _selectAllCheckBox, Width = new DataGridLength(40), CanUserResize = false, CanUserSort = false, DisplayIndex = 0 };
            checkBoxColumn.Binding = new Binding("IsSelected") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            DataGrid.Columns.Insert(0, checkBoxColumn);
        }

        private void RemoveCheckBoxColumn()
        {
            if (DataGrid == null) return;
            var checkBoxColumn = DataGrid.Columns.FirstOrDefault(c => c is DataGridCheckBoxColumn);
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
