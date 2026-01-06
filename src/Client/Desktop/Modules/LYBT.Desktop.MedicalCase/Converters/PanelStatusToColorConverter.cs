using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LYBT.Desktop.MedicalCase.Enums;

namespace LYBT.Desktop.MedicalCase.Converters;

/// <summary>
/// 面板状态转颜色转换器
/// OpenSpec: simplify-workspace-event-architecture
/// </summary>
public class PanelStatusToColorConverter : IValueConverter
{
    /// <summary>
    /// 未开始状态颜色 (灰色)
    /// </summary>
    public Brush NotStartedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(158, 158, 158));

    /// <summary>
    /// 进行中状态颜色 (蓝色)
    /// </summary>
    public Brush InProgressBrush { get; set; } = new SolidColorBrush(Color.FromRgb(33, 150, 243));

    /// <summary>
    /// 已完成状态颜色 (绿色)
    /// </summary>
    public Brush CompletedBrush { get; set; } = new SolidColorBrush(Color.FromRgb(76, 175, 80));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PanelStatus status)
        {
            return status switch
            {
                PanelStatus.NotStarted => NotStartedBrush,
                PanelStatus.InProgress => InProgressBrush,
                PanelStatus.Completed => CompletedBrush,
                _ => NotStartedBrush
            };
        }
        return NotStartedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
