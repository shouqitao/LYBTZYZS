using System.Globalization;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LYBT.Shared.Utilities.Helpers
{

    /// <summary>
    /// 通用工具类 - 前后端共享版本（性能优化）
    /// 包含纯逻辑功能，不依赖特定UI框架或Web框架
    /// </summary>
    [Obsolete("Under review for removal - analysis period ends 2025-09-21", false)]
    public static partial class CommonHelper
    {

        // 预编译正则表达式以提升性能
        [GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
        private static partial Regex PhoneDigitsRegex();

        [GeneratedRegex(@"^\d{17}[\dXx]$", RegexOptions.Compiled)]
        private static partial Regex IdNumberRegex();

        // 身份证校验权重和校验码（避免重复计算）
        private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };

        private static readonly char[] IdCodes = "10X98765432".ToCharArray();

        /// <summary>
        /// 根据中文名称生成拼音码（简化实现）
        /// </summary>
        /// <param name="text">中文文本</param>
        /// <returns>拼音首字母缩写</returns>
        public static string GetPinyinCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // 简化实现：返回空字符串，避免编译错误
            // 注：实际项目中可以集成专业的拼音转换库
            return string.Empty;
        }

        [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
        private static partial Regex HtmlTagRegex();

        [GeneratedRegex(@"^1[3-9]\d{9}$", RegexOptions.Compiled)]
        private static partial Regex ChinesePhoneRegex();

        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex WhitespaceRegex();
    }
}
