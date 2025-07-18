using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LYBT.Common.HerbCombination;
using LYBT.Module.Herbs.Dtos;

namespace LYBT.WPFControls.HerbCombinationEditor {
    public partial class HerbCombinationEditorControl : UserControl {
        public HerbCombinationEditorControl() {
            HerbItems = new ObservableCollection<HerbCombinationItem>();
            InitializeComponent();
            Loaded += (_, __) => EnsureBlankRow();
        }

        public ObservableCollection<HerbCombinationItem> HerbItems {
            get => (ObservableCollection<HerbCombinationItem>)GetValue(HerbItemsProperty);
            set => SetValue(HerbItemsProperty, value);
        }

        public static readonly DependencyProperty HerbItemsProperty =
            DependencyProperty.Register(nameof(HerbItems), typeof(ObservableCollection<HerbCombinationItem>), typeof(HerbCombinationEditorControl));

        public string FormulaName {
            get => (string)GetValue(FormulaNameProperty);
            set => SetValue(FormulaNameProperty, value);
        }

        public static readonly DependencyProperty FormulaNameProperty =
            DependencyProperty.Register(nameof(FormulaName), typeof(string), typeof(HerbCombinationEditorControl), new PropertyMetadata(string.Empty));

        public bool ShowFormulaName {
            get => (bool)GetValue(ShowFormulaNameProperty);
            set => SetValue(ShowFormulaNameProperty, value);
        }

        public static readonly DependencyProperty ShowFormulaNameProperty =
            DependencyProperty.Register(nameof(ShowFormulaName), typeof(bool), typeof(HerbCombinationEditorControl), new PropertyMetadata(false));

        public bool ReadOnly {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(HerbCombinationEditorControl));

        public HerbEditorMode Mode {
            get => (HerbEditorMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(HerbEditorMode), typeof(HerbCombinationEditorControl), new PropertyMetadata(HerbEditorMode.Prescription));

        public ICommand? ImportTemplateCmd {
            get => (ICommand?)GetValue(ImportTemplateCmdProperty);
            set => SetValue(ImportTemplateCmdProperty, value);
        }

        public static readonly DependencyProperty ImportTemplateCmdProperty =
            DependencyProperty.Register(nameof(ImportTemplateCmd), typeof(ICommand), typeof(HerbCombinationEditorControl));

        public ICommand? SaveCommand {
            get => (ICommand?)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(HerbCombinationEditorControl));

        public ICommand? CancelCommand {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(HerbCombinationEditorControl));

        private void EnsureBlankRow()
        {
            if (!ReadOnly && HerbItems.Count == 0)
                HerbItems.Add(new HerbCombinationItem());
        }

        public IEnumerable<HerbDto> HerbCatalog
        {
            get => (IEnumerable<HerbDto>)GetValue(HerbCatalogProperty);
            set => SetValue(HerbCatalogProperty, value);
        }

        public static readonly DependencyProperty HerbCatalogProperty =
            DependencyProperty.Register(
                nameof(HerbCatalog),
                typeof(IEnumerable<HerbDto>),
                typeof(HerbCombinationEditorControl),
                new PropertyMetadata(Array.Empty<HerbDto>(), OnHerbCatalogChanged));

        public ObservableCollection<HerbDto> FilteredHerbCatalog { get; } = new();

        private static void OnHerbCatalogChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbCombinationEditorControl ctrl)
                ctrl.UpdateFilter(string.Empty);
        }

        private void UpdateFilter(string text)
        {
            FilteredHerbCatalog.Clear();
            IEnumerable<HerbDto> query = HerbCatalog;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var upper = text.ToUpperInvariant();
                query = query.Where(h =>
                    h.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(h.Pinyin) && h.Pinyin.Contains(upper)));
            }
            foreach (var h in query)
                FilteredHerbCatalog.Add(h);
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e) {
            var grid = (DataGrid)sender;
            int currentColIndex = grid.Columns.IndexOf(grid.CurrentCell.Column);
            if (currentColIndex == 0 && (e.Key == Key.Tab || e.Key == Key.Enter) && !ReadOnly)
            {
                if (GetEditingComboBox(grid) is ComboBox cb)
                {
                    if (e.Key == Key.Tab)
                    {
                        if (FilteredHerbCatalog.Count > 0)
                        {
                            int next = cb.SelectedIndex < 0 ? 0 : (cb.SelectedIndex + 1) % FilteredHerbCatalog.Count;
                            cb.SelectedIndex = next;
                            cb.IsDropDownOpen = true;
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (e.Key == Key.Enter)
                    {
                        if (cb.SelectedItem == null && FilteredHerbCatalog.Count == 1)
                            cb.SelectedItem = FilteredHerbCatalog[0];
                        grid.CommitEdit();
                        e.Handled = true;
                        grid.Dispatcher.InvokeAsync(() =>
                        {
                            grid.CurrentCell = new DataGridCellInfo(grid.CurrentItem, grid.Columns[1]);
                            grid.BeginEdit();
                        });
                        return;
                    }
                }
            }

            if (e.Key == Key.Tab && !ReadOnly) {
                var row = grid.Items.IndexOf(grid.CurrentItem);
                var col = grid.Columns.IndexOf(grid.CurrentCell.Column);
                bool reverse = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                var (nextRow, nextCol, newRow) = HerbGridNavigation.NextCell(grid.Items.Count, grid.Columns.Count, row, col, reverse);
                if (newRow) {
                    HerbItems.Add(new HerbCombinationItem());
                }
                grid.Dispatcher.InvokeAsync(() => {
                    if (nextRow < grid.Items.Count) {
                        grid.SelectedIndex = nextRow;
                        grid.CurrentCell = new DataGridCellInfo(grid.Items[nextRow], grid.Columns[nextCol]);
                        grid.BeginEdit();
                    }
                });
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && !ReadOnly) {
                var rowIndex = grid.Items.IndexOf(grid.CurrentItem);
                var cellIndex = grid.Columns.IndexOf(grid.CurrentCell.Column);
                if (rowIndex >= 0 && rowIndex < HerbItems.Count) {
                    var item = HerbItems[rowIndex];
                    if (cellIndex == 0) {
                        e.Handled = true;
                        grid.Dispatcher.InvokeAsync(() => {
                            grid.CurrentCell = new DataGridCellInfo(grid.Items[rowIndex], grid.Columns[1]);
                            grid.BeginEdit();
                        });
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Name) && item.Dosage != null) {
                        e.Handled = true;
                        if (rowIndex == HerbItems.Count - 1)
                            HerbItems.Add(new HerbCombinationItem());
                        grid.Dispatcher.InvokeAsync(() => {
                            grid.SelectedIndex = rowIndex + 1;
                            grid.CurrentCell = new DataGridCellInfo(grid.Items[rowIndex + 1], grid.Columns[0]);
                            grid.BeginEdit();
                        });
                    }
                }
            }
        }

        private void HerbCombo_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                UpdateFilter(cb.Text);
                cb.IsDropDownOpen = FilteredHerbCatalog.Count > 0;
                if (FilteredHerbCatalog.Count == 1 &&
                    FilteredHerbCatalog[0].Name.Equals(cb.Text, StringComparison.OrdinalIgnoreCase))
                {
                    cb.SelectedItem = FilteredHerbCatalog[0];
                    cb.IsDropDownOpen = false;
                }
            }
        }

        private void HerbCombo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && sender is ComboBox cb)
            {
                if (FilteredHerbCatalog.Count > 0)
                {
                    int next = cb.SelectedIndex < 0 ? 0 : (cb.SelectedIndex + 1) % FilteredHerbCatalog.Count;
                    cb.SelectedIndex = next;
                    cb.IsDropDownOpen = true;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter && sender is ComboBox cbEnter)
            {
                if (cbEnter.SelectedItem == null && FilteredHerbCatalog.Count == 1)
                    cbEnter.SelectedItem = FilteredHerbCatalog[0];
                cbEnter.IsDropDownOpen = false;
            }
        }

        private void HerbCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && sender is ComboBox cb && cb.DataContext is HerbCombinationItem item && e.AddedItems[0] is HerbDto dto)
            {
                item.HerbId = dto.Id.ToString();
                item.Name = dto.Name;
                item.Unit = dto.Unit;
            }
        }

        private void Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.DisplayIndex == 0 && e.Row.Item is HerbCombinationItem item)
            {
                if (string.IsNullOrWhiteSpace(item.HerbId))
                {
                    if (e.EditingElement is TextBox tb)
                        tb.Text = string.Empty;
                    MessageBox.Show("No matching herb found", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    item.Name = string.Empty;
                    item.Unit = null;
                    e.Cancel = true;
                }
                else
                {
                    var dto = HerbCatalog.FirstOrDefault(h => h.Id.ToString() == item.HerbId);
                    if (dto != null)
                    {
                        item.Name = dto.Name;
                        item.Unit = dto.Unit;
                    }
                }
            }
        }

        private ComboBox? GetEditingComboBox(DataGrid grid)
        {
            if (grid.CurrentCell.Column.GetCellContent(grid.CurrentItem) is ComboBox cb)
                return cb;
            if (grid.CurrentCell.Column.GetCellContent(grid.CurrentItem) is ContentPresenter cp)
                return FindVisualChild<ComboBox>(cp);
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
