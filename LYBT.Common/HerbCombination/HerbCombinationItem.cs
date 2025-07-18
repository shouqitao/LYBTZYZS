namespace LYBT.Common.HerbCombination;

/// <summary>
/// Represents a single herb entry within a combination.
/// </summary>
using System.ComponentModel;

public class HerbCombinationItem : INotifyPropertyChanged
{
    /// <summary>
    /// Linked herb identifier from the master Herbs table.
    /// </summary>
    private string? _herbId;
    public string? HerbId
    {
        get => _herbId;
        set
        {
            _herbId = value;
            OnPropertyChanged(nameof(HerbId));
        }
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private decimal? _dosage;
    public decimal? Dosage
    {
        get => _dosage;
        set
        {
            _dosage = value;
            OnPropertyChanged(nameof(Dosage));
        }
    }

    private string? _unit;
    public string? Unit
    {
        get => _unit;
        set
        {
            _unit = value;
            OnPropertyChanged(nameof(Unit));
        }
    }

    private string? _usage;
    public string? Usage
    {
        get => _usage;
        set
        {
            _usage = value;
            OnPropertyChanged(nameof(Usage));
        }
    }

    private string? _remark;
    public string? Remark
    {
        get => _remark;
        set
        {
            _remark = value;
            OnPropertyChanged(nameof(Remark));
        }
    }

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
