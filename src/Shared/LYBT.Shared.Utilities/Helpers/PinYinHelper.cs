using System.Text;

namespace LYBT.Shared.Utilities
{
    /// <summary>
    /// 拼音码辅助类 - MVP版本
    /// 提供基础的汉字首字母提取功能（Epic #1934）
    /// </summary>
    public static class PinYinHelper
    {
        /// <summary>
        /// 获取中文字符串的拼音首字母（大写）
        /// </summary>
        /// <param name="text">中文字符串</param>
        /// <returns>拼音首字母，如"张三"返回"ZS"</returns>
        public static string GetInitials(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (char c in text)
            {
                // 跳过非中文字符
                if (c < 0x4E00 || c > 0x9FA5)
                {
                    continue;
                }

                // 获取拼音首字母
                var initial = GetChineseCharInitial(c);
                sb.Append(initial);
            }

            return sb.ToString().ToUpper();
        }

        /// <summary>
        /// 获取单个汉字的拼音首字母
        /// 基于汉字Unicode编码范围粗略估算
        /// </summary>
        private static char GetChineseCharInitial(char c)
        {
            int charCode = c;

            // 基于Unicode编码范围粗略映射（MVP版本）
            // 完整实现需要使用拼音库，此处使用简化映射
            if (charCode >= 0x554A && charCode <= 0x963F) // A
                return 'A';
            else if (charCode >= 0x9640 && charCode <= 0x64E5) // B
                return 'B';
            else if (charCode >= 0x64E6 && charCode <= 0x6572) // C
                return 'C';
            else if (charCode >= 0x6573 && charCode <= 0x6EFF) // D
                return 'D';
            else if (charCode >= 0x6F00 && charCode <= 0x7184) // E
                return 'E';
            else if (charCode >= 0x7185 && charCode <= 0x73E9) // F
                return 'F';
            else if (charCode >= 0x73EA && charCode <= 0x7691) // G
                return 'G';
            else if (charCode >= 0x7692 && charCode <= 0x7961) // H
                return 'H';
            else if (charCode >= 0x7962 && charCode <= 0x7D99) // J
                return 'J';
            else if (charCode >= 0x7D9A && charCode <= 0x7F50) // K
                return 'K';
            else if (charCode >= 0x7F51 && charCode <= 0x8288) // L
                return 'L';
            else if (charCode >= 0x8289 && charCode <= 0x84E5) // M
                return 'M';
            else if (charCode >= 0x84E6 && charCode <= 0x8783) // N
                return 'N';
            else if (charCode >= 0x8784 && charCode <= 0x88E4) // O
                return 'O';
            else if (charCode >= 0x88E5 && charCode <= 0x8B70) // P
                return 'P';
            else if (charCode >= 0x8B71 && charCode <= 0x8D73) // Q
                return 'Q';
            else if (charCode >= 0x8D74 && charCode <= 0x8E8F) // R
                return 'R';
            else if (charCode >= 0x8E90 && charCode <= 0x906D) // S
                return 'S';
            else if (charCode >= 0x906E && charCode <= 0x9387) // T
                return 'T';
            else if (charCode >= 0x9388 && charCode <= 0x963E) // W
                return 'W';
            else if (charCode >= 0x963F && charCode <= 0x9719) // X
                return 'X';
            else if (charCode >= 0x971A && charCode <= 0x9A4C) // Y
                return 'Y';
            else if (charCode >= 0x9A4D && charCode <= 0x9FA5) // Z
                return 'Z';
            else
                return 'A'; // 默认返回A
        }
    }
}
