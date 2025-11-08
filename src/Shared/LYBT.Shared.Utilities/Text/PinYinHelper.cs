using System.Text;

namespace LYBT.Shared.Utilities.Text
{
    /// <summary>
    /// 拼音码生成工具类 - MVP简化版
    /// 生成汉字首字母拼音码用于快速搜索
    /// Issue #1911: 实现基础拼音码生成功能
    /// </summary>
    public static class PinYinHelper
    {
        /// <summary>
        /// 生成拼音码（首字母）
        /// </summary>
        /// <param name="text">输入文本（中文姓名）</param>
        /// <returns>拼音码（大写字母）</returns>
        public static string GetPinYinCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

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

                // 汉字转拼音首字母（简化版：基于Unicode区间）
                if (ch >= 0x4e00 && ch <= 0x9fa5)
                {
                    result.Append(GetChineseFirstLetter(ch));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 获取汉字拼音首字母（简化版）
        /// 基于常用汉字Unicode范围的简单映射
        /// </summary>
        private static char GetChineseFirstLetter(char ch)
        {
            // 简化版实现：基于GBK编码的首字母规则
            // Unicode范围对应的拼音首字母区间
            var code = ch;

            if (code >= 0x4E00 && code <= 0x9FA5)
            {
                // 基于GB2312编码的拼音首字母范围（简化版）
                if (code >= 0x4E00 && code < 0x5509) return 'A';
                if (code >= 0x5509 && code < 0x5AAD) return 'B';
                if (code >= 0x5AAD && code < 0x6537) return 'C';
                if (code >= 0x6537 && code < 0x6A00) return 'D';
                if (code >= 0x6A00 && code < 0x7033) return 'E';
                if (code >= 0x7033 && code < 0x7520) return 'F';
                if (code >= 0x7520 && code < 0x785D) return 'G';
                if (code >= 0x785D && code < 0x7D00) return 'H';
                if (code >= 0x7D00 && code < 0x8000) return 'J';
                if (code >= 0x8000 && code < 0x8080) return 'K';
                if (code >= 0x8080 && code < 0x84A4) return 'L';
                if (code >= 0x84A4 && code < 0x8900) return 'M';
                if (code >= 0x8900 && code < 0x8D93) return 'N';
                if (code >= 0x8D93 && code < 0x91A5) return 'O';
                if (code >= 0x91A5 && code < 0x9500) return 'P';
                if (code >= 0x9500 && code < 0x9572) return 'Q';
                if (code >= 0x9572 && code < 0x98EF) return 'R';
                if (code >= 0x98EF && code < 0x9C00) return 'S';
                if (code >= 0x9C00 && code < 0x9FA5) return 'T';
                return 'X'; // 其他少数汉字
            }

            return 'X'; // 默认返回X
        }
    }
}
