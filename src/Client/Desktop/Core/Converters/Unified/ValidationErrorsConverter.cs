using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的验证错误转换器
    /// 合并了原有的：
    /// - ValidationErrorToMessageConverter
    /// - HasErrorsToVisibilityConverter
    /// - ErrorsToColorConverter
    /// 参数说明：
    /// - "Message" - 转换为错误消息字符串
    /// - "First" - 仅返回第一个错误消息
    /// - "Count" - 返回错误数量
    /// - "Visibility" - 转换为可见性
    /// - "Color" - 转换为颜色
    /// - "Brush" - 转换为画刷
    /// - "Icon" - 转换为图标路径
    /// - "Tooltip" - 格式化为工具提示
    /// </summary>
    public class ValidationErrorsConverter : IValueConverter, IMultiValueConverter
    {
        private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(244, 67, 54));
        private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(255, 193, 7));
        private static readonly SolidColorBrush ValidBrush = new(Color.FromRgb(76, 175, 80));

        static ValidationErrorsConverter()
        {
            ErrorBrush.Freeze();
            WarningBrush.Freeze();
            ValidBrush.Freeze();
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var errors = ExtractErrors(value);
            var format = (parameter as string ?? "Message").ToLowerInvariant();

            return format switch
            {
                "message" => GetErrorMessages(errors, false),
                "first" => GetFirstError(errors),
                "count" => errors.Count,
                "visibility" => errors.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
                "hidden" => errors.Count > 0 ? Visibility.Visible : Visibility.Hidden,
                "color" => GetErrorColor(errors),
                "brush" => GetErrorBrush(errors),
                "icon" => GetErrorIcon(errors),
                "tooltip" => GetTooltipMessage(errors),
                "isvalid" => errors.Count == 0,
                "haserrors" => errors.Count > 0,
                "severity" => GetErrorSeverity(errors),
                "list" => GetErrorList(errors),
                _ => GetErrorMessages(errors, false)
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return DependencyProperty.UnsetValue;

            // 合并所有值中的错误
            var allErrors = new List<ValidationError>();
            foreach (var value in values)
            {
                allErrors.AddRange(ExtractErrors(value));
            }

            return Convert(allErrors, targetType, parameter, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 从值中提取验证错误
        /// </summary>
        private static List<ValidationError> ExtractErrors(object? value)
        {
            var errors = new List<ValidationError>();

            switch (value)
            {
                case ValidationError error:
                    errors.Add(error);
                    break;
                    
                case IEnumerable<ValidationError> errorList:
                    errors.AddRange(errorList);
                    break;
                    
                case string errorMessage when !string.IsNullOrWhiteSpace(errorMessage):
                    errors.Add(CreateValidationError(errorMessage));
                    break;
                    
                case Exception exception:
                    errors.Add(CreateValidationError(exception.Message));
                    break;
                    
                case bool hasError when hasError:
                    errors.Add(CreateValidationError("验证失败"));
                    break;
                    
                case IEnumerable enumerable:
                    foreach (var item in enumerable)
                    {
                        if (item is ValidationError err)
                            errors.Add(err);
                        else if (item is Exception ex)
                            errors.Add(CreateValidationError(ex.Message));
                        else if (item is string msg && !string.IsNullOrWhiteSpace(msg))
                            errors.Add(CreateValidationError(msg));
                    }
                    break;
            }

            return errors;
        }

        /// <summary>
        /// 创建验证错误对象
        /// </summary>
        private static ValidationError CreateValidationError(string message)
        {
            return new ValidationError(new DataErrorValidationRule(), new object())
            {
                ErrorContent = message
            };
        }

        /// <summary>
        /// 获取错误消息
        /// </summary>
        private static string GetErrorMessages(List<ValidationError> errors, bool firstOnly)
        {
            if (errors.Count == 0)
                return string.Empty;

            if (firstOnly)
                return GetErrorContent(errors[0]);

            var messages = errors.Select(GetErrorContent).Where(m => !string.IsNullOrWhiteSpace(m));
            return string.Join("\n", messages);
        }

        /// <summary>
        /// 获取第一个错误
        /// </summary>
        private static string GetFirstError(List<ValidationError> errors)
        {
            return errors.Count > 0 ? GetErrorContent(errors[0]) : string.Empty;
        }

        /// <summary>
        /// 获取错误内容
        /// </summary>
        private static string GetErrorContent(ValidationError error)
        {
            return error.ErrorContent switch
            {
                string str => str,
                Exception ex => ex.Message,
                _ => error.ErrorContent?.ToString() ?? "验证错误"
            };
        }

        /// <summary>
        /// 获取错误颜色
        /// </summary>
        private static Color GetErrorColor(List<ValidationError> errors)
        {
            if (errors.Count == 0)
                return Colors.Transparent;
            
            // 根据错误严重程度返回不同颜色
            var severity = GetErrorSeverity(errors);
            return severity switch
            {
                "Error" => Color.FromRgb(244, 67, 54),
                "Warning" => Color.FromRgb(255, 193, 7),
                "Info" => Color.FromRgb(33, 150, 243),
                _ => Color.FromRgb(244, 67, 54)
            };
        }

        /// <summary>
        /// 获取错误画刷
        /// </summary>
        private static Brush GetErrorBrush(List<ValidationError> errors)
        {
            if (errors.Count == 0)
                return Brushes.Transparent;

            var severity = GetErrorSeverity(errors);
            return severity switch
            {
                "Warning" => WarningBrush,
                "Valid" => ValidBrush,
                _ => ErrorBrush
            };
        }

        /// <summary>
        /// 获取错误图标
        /// </summary>
        private static string GetErrorIcon(List<ValidationError> errors)
        {
            if (errors.Count == 0)
                return "/Images/Icons/check.png";

            var severity = GetErrorSeverity(errors);
            return severity switch
            {
                "Error" => "/Images/Icons/error.png",
                "Warning" => "/Images/Icons/warning.png",
                "Info" => "/Images/Icons/info.png",
                _ => "/Images/Icons/error.png"
            };
        }

        /// <summary>
        /// 获取工具提示消息
        /// </summary>
        private static string GetTooltipMessage(List<ValidationError> errors)
        {
            if (errors.Count == 0)
                return "验证通过";

            if (errors.Count == 1)
                return GetErrorContent(errors[0]);

            var sb = new StringBuilder();
            sb.AppendLine($"发现 {errors.Count} 个问题：");
            
            for (int i = 0; i < Math.Min(errors.Count, 5); i++)
            {
                sb.AppendLine($"• {GetErrorContent(errors[i])}");
            }

            if (errors.Count > 5)
            {
                sb.AppendLine($"... 还有 {errors.Count - 5} 个问题");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 获取错误严重程度
        /// </summary>
        private static string GetErrorSeverity(List<ValidationError> errors)
        {
            if (errors.Count == 0)
                return "Valid";

            // 根据错误内容判断严重程度
            foreach (var error in errors)
            {
                var content = GetErrorContent(error).ToLower();
                if (content.Contains("错误") || content.Contains("error") || content.Contains("必须"))
                    return "Error";
            }

            foreach (var error in errors)
            {
                var content = GetErrorContent(error).ToLower();
                if (content.Contains("警告") || content.Contains("warning") || content.Contains("建议"))
                    return "Warning";
            }

            return "Info";
        }

        /// <summary>
        /// 获取错误列表
        /// </summary>
        private static List<string> GetErrorList(List<ValidationError> errors)
        {
            return errors.Select(GetErrorContent).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        }
    }
}