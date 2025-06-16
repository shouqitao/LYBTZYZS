using System.Text;

namespace LYBT.Common.Helpers {

    /// <summary>
    /// 拼音辅助工具类
    /// </summary>
    public static class PinyinHelper {

        /// <summary>
        /// 获取中文字符串的拼音首字母组合
        /// </summary>
        public static string GetPinyinCode(string chinese) {
            if (string.IsNullOrWhiteSpace(chinese))
                return string.Empty;
            StringBuilder sb = new();
            foreach (char c in chinese) {
                sb.Append(GetFirstPinyinChar(c));
            }
            return sb.ToString().ToUpper();
        }

        /// <summary>
        /// 获取字符的拼音首字母（待使用第三方库完善）
        /// </summary>
        private static char GetFirstPinyinChar(char c) {
            // TODO: 实际部署时使用第三方拼音库替换
            return c;
        }
    }
}