using System.ComponentModel;
namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 登录成功返回 DTO
    /// </summary>
    public class LoginResponseDto {
        /// <summary>JWT令牌</summary>
        [DisplayName("JWT Token")]
/// <summary>
/// Token 属性。
/// </summary>
        public string Token { get; set; } = string.Empty;
        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
/// <summary>
/// User 属性。
/// </summary>
        public Users.Dtos.UserDto User { get; set; } = new();
    }
}
