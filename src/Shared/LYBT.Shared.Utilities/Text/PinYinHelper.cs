using System.Text;

namespace LYBT.Shared.Utilities.Text
{
    /// <summary>
    /// 拼音码生成工具类 - 基于NPinyin库
    /// 生成汉字首字母拼音码用于快速搜索
    /// Issue #1911: 实现基础拼音码生成功能
    /// Issue #1911 Bug修复: 使用NPinyin库替代不准确的Unicode区间映射
    /// </summary>
    public static class PinYinHelper
    {
        /// <summary>
        /// 生成拼音码（首字母）
        /// </summary>
        /// <param name="text">输入文本（中文姓名）</param>
        /// <returns>拼音码（大写字母）</returns>
        /// <example>
        /// GetPinYinCode("张韶涵") → "ZSH"
        /// GetPinYinCode("刘伟明") → "LWM"
        /// GetPinYinCode("John123") → "JOHN123"
        /// </example>
        public static string GetPinYinCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            try
            {
                // 使用NPinyin库获取拼音首字母（准确且高效）
                // GetInitials返回首字母，例如："张韶涵" → "ZSH"
                var initials = NPinyin.Pinyin.GetInitials(text);

                // 转为大写并去除空格
                return initials.ToUpperInvariant().Replace(" ", string.Empty);
            }
            catch
            {
                // 降级方案：如果NPinyin出错，使用简单的字符处理
                return GetPinYinCodeFallback(text);
            }
        }

        /// <summary>
        /// 降级方案：简单的字符处理（不依赖拼音库）
        /// </summary>
        private static string GetPinYinCodeFallback(string text)
        {
            var result = new StringBuilder();

            foreach (var ch in text)
            {
                // 跳过空白字符
                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }

                // 英文字母直接转大写
                if (char.IsLetter(ch) && ch < 128)
                {
                    result.Append(char.ToUpper(ch));
                    continue;
                }

                // 数字直接添加
                if (char.IsDigit(ch))
                {
                    result.Append(ch);
                    continue;
                }

                // 其他字符（包括汉字）忽略
            }

            return result.ToString();
        }
    }
}
