using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的字节数组到图像转换器
    /// 合并了原有的：
    /// - ByteArrayToImageSourceConverter
    /// - Base64ToImageConverter
    /// - ImagePathToSourceConverter
    /// 参数说明：
    /// - "Thumbnail" - 生成缩略图
    /// - "Width:100" - 指定宽度
    /// - "Height:100" - 指定高度
    /// - "Placeholder:/Images/placeholder.png" - 指定占位图
    /// - "Cache" - 启用缓存
    /// </summary>
    public class ByteArrayToImageConverter : IValueConverter, IMultiValueConverter
    {
        private static readonly BitmapImage DefaultPlaceholder = CreateDefaultPlaceholder();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return GetPlaceholder(parameter as string);
            }

            try
            {
                BitmapImage? image = null;

                // 根据输入类型处理
                if (value is byte[] bytes && bytes.Length > 0)
                {
                    image = BytesToImage(bytes, parameter as string);
                }
                else if (value is string strValue)
                {
                    if (IsBase64String(strValue))
                    {
                        // Base64字符串
                        var imageBytes = System.Convert.FromBase64String(strValue);
                        image = BytesToImage(imageBytes, parameter as string);
                    }
                    else if (IsValidPath(strValue))
                    {
                        // 文件路径
                        image = LoadFromPath(strValue, parameter as string);
                    }
                    else if (Uri.TryCreate(strValue, UriKind.Absolute, out var uri))
                    {
                        // URI路径
                        image = LoadFromUri(uri, parameter as string);
                    }
                }
                else if (value is Uri uri)
                {
                    image = LoadFromUri(uri, parameter as string);
                }
                else if (value is Stream stream)
                {
                    image = LoadFromStream(stream, parameter as string);
                }

                return image ?? GetPlaceholder(parameter as string);
            }
            catch (Exception)
            {
                // 加载失败，返回占位图
                return GetPlaceholder(parameter as string);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is BitmapSource bitmapSource)
            {
                // 转换为字节数组
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                
                using var stream = new MemoryStream();
                encoder.Save(stream);
                return stream.ToArray();
            }

            return DependencyProperty.UnsetValue;
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return GetPlaceholder(parameter as string);

            // 多值支持：第一个是图像数据，第二个可以是参数
            var imageParam = values.Length > 1 && values[1] is string param ? param : parameter as string;
            return Convert(values[0], targetType, imageParam, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 从字节数组创建图像
        /// </summary>
        private static BitmapImage BytesToImage(byte[] bytes, string? parameter)
        {
            var image = new BitmapImage();
            var parameters = ParseParameters(parameter);

            using (var stream = new MemoryStream(bytes))
            {
                image.BeginInit();
                
                if (parameters.TryGetValue("cache", out _))
                {
                    image.CacheOption = BitmapCacheOption.OnLoad;
                }
                else
                {
                    image.CacheOption = BitmapCacheOption.None;
                }

                // 设置尺寸
                if (parameters.TryGetValue("width", out var widthStr) && int.TryParse(widthStr, out var width))
                {
                    image.DecodePixelWidth = width;
                }
                
                if (parameters.TryGetValue("height", out var heightStr) && int.TryParse(heightStr, out var height))
                {
                    image.DecodePixelHeight = height;
                }

                if (parameters.ContainsKey("thumbnail"))
                {
                    image.DecodePixelWidth = 100;
                    image.CreateOptions = BitmapCreateOptions.DelayCreation;
                }

                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
            }

            return image;
        }

        /// <summary>
        /// 从文件路径加载图像
        /// </summary>
        private static BitmapImage LoadFromPath(string path, string? parameter)
        {
            if (!File.Exists(path))
            {
                return CreateDefaultPlaceholder();
            }

            var bytes = File.ReadAllBytes(path);
            return BytesToImage(bytes, parameter);
        }

        /// <summary>
        /// 从URI加载图像
        /// </summary>
        private static BitmapImage LoadFromUri(Uri uri, string? parameter)
        {
            var image = new BitmapImage();
            var parameters = ParseParameters(parameter);

            image.BeginInit();
            image.UriSource = uri;
            
            if (parameters.TryGetValue("cache", out _))
            {
                image.CacheOption = BitmapCacheOption.OnLoad;
            }

            if (parameters.TryGetValue("width", out var widthStr) && int.TryParse(widthStr, out var width))
            {
                image.DecodePixelWidth = width;
            }
            
            if (parameters.TryGetValue("height", out var heightStr) && int.TryParse(heightStr, out var height))
            {
                image.DecodePixelHeight = height;
            }

            image.EndInit();
            image.Freeze();

            return image;
        }

        /// <summary>
        /// 从流加载图像
        /// </summary>
        private static BitmapImage LoadFromStream(Stream stream, string? parameter)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return BytesToImage(memoryStream.ToArray(), parameter);
        }

        /// <summary>
        /// 获取占位图
        /// </summary>
        private static ImageSource GetPlaceholder(string? parameter)
        {
            var parameters = ParseParameters(parameter);
            
            if (parameters.TryGetValue("placeholder", out var placeholderPath))
            {
                if (File.Exists(placeholderPath))
                {
                    try
                    {
                        return LoadFromPath(placeholderPath, null);
                    }
                    catch
                    {
                        // 加载失败，使用默认
                    }
                }
            }

            return DefaultPlaceholder;
        }

        /// <summary>
        /// 创建默认占位图
        /// </summary>
        private static BitmapImage CreateDefaultPlaceholder()
        {
            // 创建一个简单的灰色占位图
            var bitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 100, 100));
                context.DrawLine(new Pen(Brushes.Gray, 1), new Point(0, 0), new Point(100, 100));
                context.DrawLine(new Pen(Brushes.Gray, 1), new Point(100, 0), new Point(0, 100));
            }
            
            bitmap.Render(visual);

            // 转换为BitmapImage
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;
        }

        /// <summary>
        /// 判断是否为Base64字符串
        /// </summary>
        private static bool IsBase64String(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length % 4 != 0)
                return false;

            try
            {
                System.Convert.FromBase64String(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为有效路径
        /// </summary>
        private static bool IsValidPath(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                return !string.IsNullOrEmpty(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析参数
        /// </summary>
        private static Dictionary<string, string> ParseParameters(string? parameter)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(parameter))
                return result;

            var parts = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains(':'))
                {
                    var kvp = part.Split(':', 2);
                    result[kvp[0].ToLower()] = kvp[1];
                }
                else
                {
                    result[part.ToLower()] = string.Empty;
                }
            }

            return result;
        }
    }
}