using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Common.HerbCombination;

/// <summary>
/// View model for editing herb combinations.
/// </summary>
public class HerbCombinationEditorViewModel : INotifyPropertyChanged
{
    public ObservableCollection<HerbCombinationItem> Items { get; } = new();

    private string _formulaName = string.Empty;
    public string FormulaName
    {
        get => _formulaName;
        set
        {
            _formulaName = value;
            OnPropertyChanged(nameof(FormulaName));
        }
    }

    public HerbEditorMode Mode { get; set; }

    /// <summary>
    /// Validate herb combination and formula name depending on mode.
    /// </summary>
    public bool Validate(out string? message)
    {
        if (Mode == HerbEditorMode.Template && string.IsNullOrWhiteSpace(FormulaName))
        {
            message = "Formula name is required";
            return false;
        }
        for (int i = 0; i < Items.Count; i++)
        {
            var it = Items[i];
            if (string.IsNullOrWhiteSpace(it.HerbId) || string.IsNullOrWhiteSpace(it.Name) || it.Dosage == null)
            {
                message = $"Row {i + 1} requires a valid herb and dosage";
                return false;
            }
        }
        message = null;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
