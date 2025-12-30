using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using LYBT.Desktop.Infrastructure.Controls;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>
/// 患者卡片显示模式到可见性转换器
/// OpenSpec: refactor-medicalcase-workspace, standardize-converter-organization
/// </summary>
public class PatientCardDisplayModeToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 转换显示模式到可见性
    /// </summary>
    /// <param name="value">当前显示模式</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">期望的显示模式(字符串或PatientCardDisplayMode)</param>
    /// <param name="culture">区域信息</param>
    /// <returns>如果当前模式匹配参数模式则返回Visible，否则Collapsed</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PatientCardDisplayMode currentMode)
            return Visibility.Collapsed;

        PatientCardDisplayMode targetMode;

        if (parameter is PatientCardDisplayMode mode)
        {
            targetMode = mode;
        }
        else if (parameter is string modeString && Enum.TryParse<PatientCardDisplayMode>(modeString, out var parsed))
        {
            targetMode = parsed;
        }
        else
        {
            return Visibility.Visible;
        }

        return currentMode == targetMode ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
