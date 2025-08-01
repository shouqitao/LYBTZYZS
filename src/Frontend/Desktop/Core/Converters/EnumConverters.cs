using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Core.Converters
{
    /// <summary>
    /// 通用枚举转显示文本转换器
    /// </summary>
    public class EnumToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            return value switch
            {
                Gender gender => gender.GetDescription(),
                UserRole userRole => userRole.GetDescription(),
                PatientStatus patientStatus => patientStatus.GetDescription(),
                HerbStatus herbStatus => herbStatus.GetDescription(),
                BillingStatus billingStatus => billingStatus.GetDescription(),
                PharmacyStatus pharmacyStatus => pharmacyStatus.GetDescription(),
                Enum enumValue => enumValue.GetDescription(),
                _ => value.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EnumToDisplayNameConverter 不支持反向转换");
        }
    }

    /// <summary>
    /// 用户角色转显示文本转换器
    /// </summary>
    public class UserRoleToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                return role.GetDescription();
            }
            return "未知角色";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
    public class HerbStatusToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HerbStatus status)
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