using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 通用枚举转显示文本转换器
    /// </summary>
    public class EnumToDisplayNameConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value switch
            {
                Gender gender => gender.GetDescription(),
                PatientStatus patientStatus => patientStatus.GetDescription(),
                CommonStatus commonStatus => commonStatus.GetDescription(),
                PrescriptionStatus prescriptionStatus => prescriptionStatus.GetDescription(),
                Enum enumValue => enumValue.GetDescription(),
                _ => value.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EnumToDisplayNameConverter 不支持反向转换");
        }
    }

    /// <summary>
    /// 患者状态转显示文本转换器
    /// </summary>
    public class PatientStatusToDisplayNameConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PatientStatus status)
            {
                return status.GetDescription();
            }
            return "未知状态";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 药材状态转显示文本转换器
    /// </summary>
    public class CommonStatusToDisplayNameConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CommonStatus status)
            {
                return status.GetDescription();
            }
            return "未知状态";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
