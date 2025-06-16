using System.Text;

namespace LYBT.Common.Helpers {

    public static class PinyinHelper {

        public static string GetPinyinCode(string chinese) {
            if (string.IsNullOrWhiteSpace(chinese))
                return string.Empty;
            StringBuilder sb = new();
            foreach (char c in chinese) {
                sb.Append(GetFirstPinyinChar(c));
            }
            return sb.ToString().ToUpper();
        }

        private static char GetFirstPinyinChar(char c) {
            // TODO: 实际部署时使用第三方拼音库替换
            return c;
        }
    }
}