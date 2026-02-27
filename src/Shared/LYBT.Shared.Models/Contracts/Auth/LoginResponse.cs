using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 登录成功返回响应 - 前后端共享API契约
    /// </summary>
    public class LoginResponse
    {

        /// <summary>JWT令牌</summary>
        [DisplayName("JWT Token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public UserDetailDto User { get; set; } = new();

        /// <summary>刷新令牌</summary>
        [DisplayName("刷新令牌")]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>令牌过期时间</summary>
        [DisplayName("过期时间")]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 自动登录令牌（仅当RememberMe=true时返回）
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        /// <remarks>
        /// <para>功能: 用于"记住密码"场景的自动登录</para>
        /// <para>安全: 服务端生成的长期有效令牌，可随时撤销</para>
        /// <para>存储: 客户端使用DPAPI+HMAC安全存储</para>
        /// </remarks>
        [DisplayName("自动登录令牌")]
        public string? AutoLoginToken { get; set; }

        /// <summary>
        /// T5-P2-31: 是否需要在登录后修改密码
        /// 管理员重置密码后，用户首次登录时设置为 true
        /// </summary>
        [DisplayName("须改密")]
        public bool MustChangePassword { get; set; }
    }
}
