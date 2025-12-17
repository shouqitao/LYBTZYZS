using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Behaviors;

/// <summary>
/// DataGrid选择行为 - 提供checkbox列和SelectedItems同步功能
/// OpenSpec: refactor-master-detail-layout - 支持批量选择操作
///
/// 使用方式:
/// <DataGrid behaviors:DataGridSelectionBehavior.ShowCheckBoxColumn="True"
///           behaviors:DataGridSelectionBehavior.SelectedItems="{Binding SelectedItems, Mode=TwoWay}"/>
/// </summary>
public static class DataGridSelectionBehavior
{
    #region ShowCheckBoxColumn 附加属性

    public static readonly DependencyProperty ShowCheckBoxColumnProperty =
        DependencyProperty.RegisterAttached(
            "ShowCheckBoxColumn",
            typeof(bool),
            typeof(DataGridSelectionBehavior),
            new PropertyMetadata(false, OnShowCheckBoxColumnChanged));

    public static bool GetShowCheckBoxColumn(DependencyObject obj) =>
        (bool)obj.GetValue(ShowCheckBoxColumnProperty);

    public static void SetShowCheckBoxColumn(DependencyObject obj, bool value) =>
        obj.SetValue(ShowCheckBoxColumnProperty, value);

    #endregion

    #region SelectedItems 附加属性

    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(DataGridSelectionBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    public static IList? GetSelectedItems(DependencyObject obj) =>
        (IList?)obj.GetValue(SelectedItemsProperty);

    public static void SetSelectedItems(DependencyObject obj, IList? value) =>
        obj.SetValue(SelectedItemsProperty, value);

    #endregion

    #region 内部附加属性 - 存储状态

    private static readonly DependencyProperty SelectAllCheckBoxProperty =
        DependencyProperty.RegisterAttached("SelectAllCheckBox", typeof(CheckBox), typeof(DataGridSelectionBehavior));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(DataGridSelectionBehavior));

    private static readonly DependencyProperty IsSelectingFromCheckBoxProperty =
        DependencyProperty.RegisterAttached("IsSelectingFromCheckBox", typeof(bool), typeof(DataGridSelectionBehavior));

    #endregion

    #region ShowCheckBoxColumn 逻辑

    private static void OnShowCheckBoxColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid) return;

        if ((bool)e.NewValue)
        {
            dataGrid.SelectionMode = DataGridSelectionMode.Extended;
            dataGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            dataGrid.Loaded += DataGrid_Loaded;
            dataGrid.SelectionChanged += DataGrid_SelectionChanged;
            if (dataGrid.IsLoaded)
                AddCheckBoxColumn(dataGrid);
        }
        else
        {
            dataGrid.Loaded -= DataGrid_Loaded;
            dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            RemoveCheckBoxColumn(dataGrid);
        }
    }

    private static void DataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
            AddCheckBoxColumn(dataGrid);
    }

    private static void AddCheckBoxColumn(DataGrid dataGrid)
    {
        // 检查是否已存在checkbox列
        if (dataGrid.Columns.Any(c => c.Header is CheckBox)) return;

        // 创建全选checkbox
        // IsThreeState=true允许显示部分选择状态，但用户点击只在全选/全不选之间切换
        var selectAllCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsThreeState = true,
            ToolTip = "全选/取消全选",
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Gray
        };
        // 使用Click事件而非Checked/Unchecked，以便手动控制状态切换逻辑
        selectAllCheckBox.Click += (s, e) => OnSelectAllCheckBoxClick(dataGrid, selectAllCheckBox);

        dataGrid.SetValue(SelectAllCheckBoxProperty, selectAllCheckBox);

        // 创建DataGridTemplateColumn
        var checkBoxColumn = new DataGridTemplateColumn
        {
            Header = selectAllCheckBox,
            Width = new DataGridLength(40),
            CanUserResize = false,
            CanUserSort = false
        };

        // 创建CellTemplate
        var cellTemplate = new DataTemplate();
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        borderFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        borderFactory.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        borderFactory.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler((s, e) => OnRowCheckBoxClick(dataGrid, s, e)));
        borderFactory.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
            new MouseButtonEventHandler((s, e) => e.Handled = true));
        borderFactory.AddHandler(Control.PreviewMouseDoubleClickEvent,
            new MouseButtonEventHandler((s, e) => e.Handled = true));

        var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
        checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGridRow), 1),
            Mode = BindingMode.OneWay
        });
        checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        checkBoxFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        checkBoxFactory.SetValue(CheckBox.BackgroundProperty, Brushes.Transparent);
        checkBoxFactory.SetValue(CheckBox.FocusableProperty, false);
        checkBoxFactory.SetValue(CheckBox.IsHitTestVisibleProperty, false);

        borderFactory.AppendChild(checkBoxFactory);
        cellTemplate.VisualTree = borderFactory;
        checkBoxColumn.CellTemplate = cellTemplate;

        // 设置HeaderStyle - 确保与内容行checkbox对齐
        // 注意：内容行有3像素左边框(选中指示条)，Header需要相应的左padding补偿
        checkBoxColumn.HeaderStyle = new Style(typeof(DataGridColumnHeader))
        {
            Setters = {
                new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(DataGridColumnHeader.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(DataGridColumnHeader.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(3, 0, 0, 0)),
                new Setter(DataGridColumnHeader.BackgroundProperty, Application.Current.TryFindResource("SurfaceBrush") ?? Brushes.White),
                new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 1)),
                new Setter(DataGridColumnHeader.BorderBrushProperty, Application.Current.TryFindResource("DividerBrush") ?? Brushes.LightGray),
                new Setter(DataGridColumnHeader.MinHeightProperty, 40.0)
            }
        };

        // 设置CellStyle - 直接设置所有必要属性，不依赖外部样式资源
        checkBoxColumn.CellStyle = new Style(typeof(DataGridCell))
        {
            Setters = {
                new Setter(DataGridCell.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                new Setter(DataGridCell.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(DataGridCell.PaddingProperty, new Thickness(0)),
                new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)),
                new Setter(DataGridCell.FocusVisualStyleProperty, null),
                new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent)
            },
            Triggers = {
                new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true, Setters = {
                    new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent),
                    new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent)
                }},
                new Trigger { Property = DataGridCell.IsKeyboardFocusWithinProperty, Value = true, Setters = {
                    new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent)
                }}
            }
        };

        dataGrid.Columns.Insert(0, checkBoxColumn);
    }

    private static void RemoveCheckBoxColumn(DataGrid dataGrid)
    {
        var checkBoxColumn = dataGrid.Columns.FirstOrDefault(c => c.Header is CheckBox);
        if (checkBoxColumn != null)
            dataGrid.Columns.Remove(checkBoxColumn);
    }

    /// <summary>
    /// 处理全选checkbox点击事件
    /// 用户点击只在全选/全不选之间切换，部分选择状态只由行选择变化触发
    /// </summary>
    private static void OnSelectAllCheckBoxClick(DataGrid dataGrid, CheckBox selectAllCheckBox)
    {
        if ((bool)dataGrid.GetValue(IsUpdatingProperty)) return;

        dataGrid.SetValue(IsSelectingFromCheckBoxProperty, true);
        dataGrid.SetValue(IsUpdatingProperty, true);

        // WPF IsThreeState状态循环: false -> true -> null -> false
        // Click事件在状态改变后触发，所以我们检查改变后的状态:
        // - 现在是null: 之前是true(全选) -> 应该全不选
        // - 现在是true: 之前是false(无选择) -> 已经正确，执行全选
        // - 现在是false: 之前是null(部分选择) -> 应该全选

        if (selectAllCheckBox.IsChecked == null)
        {
            // 之前是全选，用户想取消全选
            dataGrid.UnselectAll();
            selectAllCheckBox.IsChecked = false;
        }
        else if (selectAllCheckBox.IsChecked == true)
        {
            // 之前是无选择，用户想全选
            dataGrid.SelectAll();
        }
        else // false
        {
            // 之前是部分选择，用户想全选
            dataGrid.SelectAll();
            selectAllCheckBox.IsChecked = true;
        }

        dataGrid.SetValue(IsUpdatingProperty, false);
        dataGrid.SetValue(IsSelectingFromCheckBoxProperty, false);
    }

    private static void OnRowCheckBoxClick(DataGrid dataGrid, object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        var row = FindVisualParent<DataGridRow>(element);
        if (row?.Item == null) return;

        var shouldSelect = !row.IsSelected;
        var dataItem = row.Item;

        dataGrid.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                if (shouldSelect)
                {
                    if (!dataGrid.SelectedItems.Contains(dataItem))
                        dataGrid.SelectedItems.Add(dataItem);
                }
                else
                {
                    if (dataGrid.SelectedItems.Contains(dataItem))
                        dataGrid.SelectedItems.Remove(dataItem);
                }
                dataGrid.Focus();
            }
            catch (InvalidOperationException) { }
        }), System.Windows.Threading.DispatcherPriority.Input);

        e.Handled = true;
    }

    #endregion

    #region SelectedItems 同步逻辑

    private static void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        SyncSelectedItems(dataGrid);
        UpdateSelectAllCheckBoxState(dataGrid);
    }

    private static void SyncSelectedItems(DataGrid dataGrid)
    {
        var selectedItems = GetSelectedItems(dataGrid);
        if (selectedItems == null) return;

        // 移除不再选中的项
        var itemsToRemove = selectedItems.Cast<object>()
            .Where(item => !dataGrid.SelectedItems.Contains(item))
            .ToList();
        foreach (var item in itemsToRemove)
            selectedItems.Remove(item);

        // 添加新选中的项
        foreach (var item in dataGrid.SelectedItems)
        {
            if (!selectedItems.Contains(item))
                selectedItems.Add(item);
        }
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid) return;

        if (e.NewValue is IList newList && dataGrid.IsLoaded)
        {
            dataGrid.SelectionChanged -= DataGrid_SelectionChanged;
            dataGrid.SelectedItems.Clear();
            foreach (var item in newList)
                dataGrid.SelectedItems.Add(item);
            dataGrid.SelectionChanged += DataGrid_SelectionChanged;
        }
    }

    private static void UpdateSelectAllCheckBoxState(DataGrid dataGrid)
    {
        var selectAllCheckBox = dataGrid.GetValue(SelectAllCheckBoxProperty) as CheckBox;
        if (selectAllCheckBox == null) return;
        if ((bool)dataGrid.GetValue(IsSelectingFromCheckBoxProperty)) return;

        var totalCount = dataGrid.Items.Count;
        dataGrid.SetValue(IsUpdatingProperty, true);

        if (totalCount == 0)
            selectAllCheckBox.IsChecked = false;
        else
        {
            var selectedCount = dataGrid.SelectedItems.Count;
            selectAllCheckBox.IsChecked = selectedCount == 0 ? false : selectedCount == totalCount ? true : null;
        }

        dataGrid.SetValue(IsUpdatingProperty, false);
    }

    #endregion

    #region 辅助方法

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

    #endregion
}
