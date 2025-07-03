using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Common.Extensions;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// Converts an enum value to its DescriptionAttribute text.
    /// </summary>
    public class EnumDescriptionConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Enum e ? e.GetDescription() : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
