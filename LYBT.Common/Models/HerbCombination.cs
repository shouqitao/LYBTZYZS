using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Common.Models {
    public enum HerbEditorMode { Template, Prescription }

    public class HerbCombinationItem {
        public string Name { get; set; } = string.Empty;
        public decimal? Dosage { get; set; }
        public string? Unit { get; set; }
        public string? Usage { get; set; }
        public string? Remark { get; set; }
    }

    public static class HerbGridNavigation {
        public static (int row, int col, bool newRow) NextCell(int rowCount, int colCount, int row, int col, bool reverse) {
            if (!reverse) {
                if (col == colCount - 1) {
                    if (row == rowCount - 1)
                        return (rowCount, 0, true);
                    return (row + 1, 0, false);
                }
                return (row, col + 1, false);
            } else {
                if (col == 0) {
                    if (row == 0) return (0, 0, false);
                    return (row - 1, colCount - 1, false);
                }
                return (row, col - 1, false);
            }
        }
    }

    public class HerbCombinationEditorViewModel : INotifyPropertyChanged {
        public ObservableCollection<HerbCombinationItem> Items { get; } = new();
        private string _formulaName = string.Empty;
        public string FormulaName {
            get => _formulaName;
            set { _formulaName = value; OnPropertyChanged(nameof(FormulaName)); }
        }
        public HerbEditorMode Mode { get; set; }

        public bool Validate(out string? message) {
            if (Mode == HerbEditorMode.Template && string.IsNullOrWhiteSpace(FormulaName)) {
                message = "Formula name is required";
                return false;
            }
            for (int i = 0; i < Items.Count; i++) {
                var it = Items[i];
                if (string.IsNullOrWhiteSpace(it.Name) || it.Dosage == null) {
                    message = $"Row {i + 1} requires name and dosage";
                    return false;
                }
            }
            message = null;
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
