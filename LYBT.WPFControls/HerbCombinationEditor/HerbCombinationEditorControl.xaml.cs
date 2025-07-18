using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            DependencyProperty.Register(nameof(HerbCatalog), typeof(IEnumerable<HerbDto>), typeof(HerbCombinationEditorControl), new PropertyMetadata(Array.Empty<HerbDto>()));

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Tab && !ReadOnly) {
                var grid = (DataGrid)sender;
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
                var grid = (DataGrid)sender;
                var rowIndex = grid.Items.IndexOf(grid.CurrentItem);
                var colIndex = grid.Columns.IndexOf(grid.CurrentCell.Column);
                if (colIndex == grid.Columns.Count - 1 && rowIndex >= 0 && rowIndex < HerbItems.Count) {
                    var item = HerbItems[rowIndex];
                    if (!string.IsNullOrWhiteSpace(item.Name) && item.Dosage != null) {
                        if (rowIndex == HerbItems.Count - 1)
                            HerbItems.Add(new HerbCombinationItem());
                        e.Handled = true;
                        grid.Dispatcher.InvokeAsync(() => {
                            grid.SelectedIndex = rowIndex + 1;
                            grid.CurrentCell = new DataGridCellInfo(grid.Items[rowIndex + 1], grid.Columns[0]);
                            grid.BeginEdit();
                        });
                    }
                }
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
                    MessageBox.Show("No such herb found in the database.", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
