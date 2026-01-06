using System.Globalization;
using System.Windows.Data;
using LYBT.Desktop.MedicalCase.Enums;

namespace LYBT.Desktop.MedicalCase.Converters;

/// <summary>
/// 面板状态转文本转换器
/// OpenSpec: simplify-workspace-event-architecture
/// </summary>
public class PanelStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PanelStatus status)
        {
            return status switch
            {
                PanelStatus.NotStarted => "未开始",
                PanelStatus.InProgress => "进行中",
                PanelStatus.Completed => "已完成",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
