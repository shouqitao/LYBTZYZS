using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Controls.Converters;

/// <summary>
/// bool -> int (false=0, true=1)，用于 Transitioner.SelectedIndex 绑定 IsEditMode
/// </summary>
public class BoolToIntConverter : IValueConverter
{
    public static readonly BoolToIntConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? (b ? 1 : 0) : 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 1;
}
