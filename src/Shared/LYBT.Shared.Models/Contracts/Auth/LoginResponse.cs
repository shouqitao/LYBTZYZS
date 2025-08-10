using LYBT.Shared.Models.Core;
using System.ComponentModel;

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
        public BaseUser User { get; set; } = new();
    }
}