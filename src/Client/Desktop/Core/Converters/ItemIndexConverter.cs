using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 获取项目在集合中索引的转换器
    /// </summary>
    public class ItemIndexConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ContentPresenter contentPresenter)
            {
                var itemsControl = ItemsControl.ItemsControlFromItemContainer(contentPresenter);
                if (itemsControl != null)
                {
                    var index = itemsControl.ItemContainerGenerator.IndexFromContainer(contentPresenter);
                    return (index + 1).ToString();
                }
            }

            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
