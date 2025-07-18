using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LYBT.Common.HerbCombination;

/// <summary>
/// View model for editing herb combinations.
/// </summary>
public class HerbCombinationEditorViewModel : INotifyPropertyChanged
{
    public ObservableCollection<HerbCombinationItem> Items { get; } = new();

    /// <summary>
    /// Remove rows where both name and dosage are empty.
    /// </summary>
    public void CleanEmptyRows()
    {
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            var it = Items[i];
            if (string.IsNullOrWhiteSpace(it.Name) && it.Dosage == null)
                Items.RemoveAt(i);
        }
    }

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
            bool error = string.IsNullOrWhiteSpace(it.HerbId) || string.IsNullOrWhiteSpace(it.Name) || it.Dosage == null;
            it.HasError = error;
            if (error)
            {
                message = "Incomplete herb information";
                return false;
            }
        }
        message = null;
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
