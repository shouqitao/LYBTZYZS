using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Core.Models.Authentication {
    /// <summary>
    /// 登录响应模型
    /// </summary>
    public class LoginResponse {
        /// <summary>JWT令牌</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        public UserInfo User { get; set; } = new();
    }
}