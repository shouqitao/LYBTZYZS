namespace LYBT.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 密码配置选项
    /// </summary>
    public class PasswordOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "PasswordOptions";

        /// <summary>
        /// 新建用户的默认密码
        /// </summary>
        public string DefaultUserPassword { get; set; } = "ChangeMe123";

        /// <summary>
        /// 系统管理员的默认密码
        /// </summary>
        public string DefaultAdminPassword { get; set; } = "Admin@123456";

        /// <summary>
        /// 密码最小长度
        /// </summary>
        public int MinLength { get; set; } = 8;

        /// <summary>
        /// 是否要求包含大写字母
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// 是否要求包含小写字母
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// 是否要求包含数字
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// 是否要求包含特殊字符
        /// </summary>
        public bool RequireSpecialChar { get; set; } = false;

        /// <summary>
        /// 密码过期天数（0表示永不过期）
        /// </summary>
        public int ExpirationDays { get; set; } = 90;

        /// <summary>
        /// 密码历史记录数量（防止重复使用最近的密码）
        /// </summary>
        public int PasswordHistoryCount { get; set; } = 5;
    }
}
