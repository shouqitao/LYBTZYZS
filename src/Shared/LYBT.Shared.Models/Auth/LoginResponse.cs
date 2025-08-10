using System.ComponentModel;
using LYBT.Shared.Models.Core;

namespace LYBT.Shared.Models.Auth
{

    /// <summary>
    /// 登录成功返回数据 - API契约
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