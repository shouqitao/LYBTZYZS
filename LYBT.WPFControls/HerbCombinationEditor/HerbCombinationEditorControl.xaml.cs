using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Common.Models;

namespace LYBT.WPFControls.HerbCombinationEditor {
    public partial class HerbCombinationEditorControl : UserControl {
        public HerbCombinationEditorControl() {
            HerbItems = new ObservableCollection<HerbCombinationItem>();
            InitializeComponent();
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
            }
        }
    }
}
