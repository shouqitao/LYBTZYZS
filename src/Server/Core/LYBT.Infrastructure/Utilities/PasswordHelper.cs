namespace LYBT.Infrastructure.Utilities
{
    /// <summary>
    /// 密码工具类
    /// Issue #1757: 从UserService提取密码生成方法
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// 生成临时密码 (Issue #1162)
        /// 格式：大写字母(1) + 小写字母(4) + 数字(3) = 8位
        /// 示例：Abcd123
        /// </summary>
        /// <returns>8位临时密码</returns>
        public static string GenerateTemporaryPassword()
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";

            var random = new Random();
            var password = new char[8];

            // 1个大写字母
            password[0] = upperChars[random.Next(upperChars.Length)];

            // 4个小写字母
            for (int i = 1; i <= 4; i++)
            {
                password[i] = lowerChars[random.Next(lowerChars.Length)];
            }

            // 3个数字
            for (int i = 5; i < 8; i++)
            {
                password[i] = numberChars[random.Next(numberChars.Length)];
            }

            return new string(password);
        }
    }
}
