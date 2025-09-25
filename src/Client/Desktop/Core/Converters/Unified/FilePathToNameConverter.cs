using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的文件路径转换器
    /// 合并了原有的：
    /// - PathToFileNameConverter
    /// - FilePathConverter
    /// - DirectoryNameConverter
    /// 参数说明：
    /// - "FileName" - 仅文件名（包含扩展名）
    /// - "FileNameNoExt" - 文件名（不含扩展名）
    /// - "Extension" - 仅扩展名
    /// - "Directory" - 目录路径
    /// - "DirectoryName" - 目录名称
    /// - "Parent" - 父目录路径
    /// - "Full" - 完整路径
    /// - "Relative" - 相对路径（相对于应用程序目录）
    /// - "Uri" - 转换为Uri格式
    /// </summary>
    public class FilePathToNameConverter : IValueConverter, IMultiValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            string path = value switch
            {
                Uri uri => uri.LocalPath,
                string str => str,
                _ => value.ToString() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var format = parameter as string ?? "FileName";

            try
            {
                return format.ToLowerInvariant() switch
                {
                    "filename" => Path.GetFileName(path),
                    "filenamenoext" => Path.GetFileNameWithoutExtension(path),
                    "extension" or "ext" => Path.GetExtension(path),
                    "directory" or "dir" => Path.GetDirectoryName(path) ?? string.Empty,
                    "directoryname" or "dirname" => GetDirectoryName(path),
                    "parent" => GetParentDirectory(path),
                    "full" => Path.GetFullPath(path),
                    "relative" => GetRelativePath(path),
                    "uri" => ConvertToUri(path),
                    "short" => GetShortPath(path),
                    "exists" => File.Exists(path) || Directory.Exists(path),
                    _ => Path.GetFileName(path)
                };
            }
            catch (ArgumentException)
            {
                // 无效路径
                return string.Empty;
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 不支持反向转换
            throw new NotImplementedException();
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return DependencyProperty.UnsetValue;

            // 多值支持：组合路径
            if (values.Length > 1)
            {
                var paths = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    paths[i] = values[i]?.ToString() ?? string.Empty;
                }
                
                try
                {
                    var combinedPath = Path.Combine(paths);
                    return Convert(combinedPath, targetType, parameter, culture);
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            return Convert(values[0], targetType, parameter, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取目录名称（最后一级目录）
        /// </summary>
        private static string GetDirectoryName(string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirPath))
                return string.Empty;

            var dirInfo = new DirectoryInfo(dirPath);
            return dirInfo.Name;
        }

        /// <summary>
        /// 获取父目录路径
        /// </summary>
        private static string GetParentDirectory(string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirPath))
                return string.Empty;

            var parentPath = Path.GetDirectoryName(dirPath);
            return parentPath ?? string.Empty;
        }

        /// <summary>
        /// 获取相对路径
        /// </summary>
        private static string GetRelativePath(string path)
        {
            try
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var fullPath = Path.GetFullPath(path);
                
                if (fullPath.StartsWith(appDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return fullPath[appDirectory.Length..].TrimStart(Path.DirectorySeparatorChar);
                }

                // 如果不在应用程序目录下，返回原路径
                return path;
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        /// 转换为Uri格式
        /// </summary>
        private static string ConvertToUri(string path)
        {
            try
            {
                var uri = new Uri(Path.GetFullPath(path));
                return uri.ToString();
            }
            catch
            {
                return $"file:///{path.Replace('\\', '/')}";
            }
        }

        /// <summary>
        /// 获取缩短的路径显示
        /// </summary>
        private static string GetShortPath(string path, int maxLength = 30)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            var fileName = Path.GetFileName(path);
            if (fileName.Length >= maxLength)
                return fileName;

            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var parts = directory.Split(Path.DirectorySeparatorChar);
            
            if (parts.Length <= 2)
                return path;

            // 保留第一个和最后一个目录
            var shortPath = $"{parts[0]}{Path.DirectorySeparatorChar}...{Path.DirectorySeparatorChar}{parts[^1]}{Path.DirectorySeparatorChar}{fileName}";
            
            if (shortPath.Length > maxLength && parts.Length > 2)
            {
                shortPath = $"...{Path.DirectorySeparatorChar}{fileName}";
            }

            return shortPath;
        }
    }
}