namespace LYBT.Module.Users.Configuration
{
    /// <summary>
    /// 用户模块配置选项
    /// </summary>
    public class UserModuleOptions
    {
        /// <summary>
        /// 是否启用用户缓存
        /// </summary>
        public bool EnableCache { get; set; } = false;
        
        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
        
        /// <summary>
        /// 密码最小长度
        /// </summary>
        public int MinPasswordLength { get; set; } = 8;
        
        /// <summary>
        /// 是否要求密码包含特殊字符
        /// </summary>
        public bool RequireSpecialCharacter { get; set; } = true;
        
        /// <summary>
        /// 登录失败最大次数
        /// </summary>
        public int MaxFailedLoginAttempts { get; set; } = 5;
        
        /// <summary>
        /// 账户锁定时长（分钟）
        /// </summary>
        public int AccountLockoutMinutes { get; set; } = 30;
    }
}