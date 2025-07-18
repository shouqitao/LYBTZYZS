using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using LYBT.Common.HerbCombination;

namespace LYBT.WPFControls.Converters;

/// <summary>
/// Converts <see cref="HerbEditorMode"/> to <see cref="Visibility"/>.
/// Collapses element when mode is <see cref="HerbEditorMode.Template"/>.
/// </summary>
public class HideInTemplateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HerbEditorMode mode && mode == HerbEditorMode.Template)
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
