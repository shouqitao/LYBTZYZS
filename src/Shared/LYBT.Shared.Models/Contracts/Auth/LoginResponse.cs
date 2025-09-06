using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Models.Contracts.Auth {

    /// <summary>
    /// 登录成功返回响应 - 前后端共享API契约
    /// UltraThink v2.0: 使用UserDto替代BaseUser
    /// </summary>
    public class LoginResponse {

        /// <summary>JWT令牌</summary>
        [DisplayName("JWT Token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public UserDto User { get; set; } = new();

        /// <summary>刷新令牌</summary>
        [DisplayName("刷新令牌")]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>令牌过期时间</summary>
        [DisplayName("过期时间")]
        public DateTime ExpiresAt { get; set; }
    }
}
