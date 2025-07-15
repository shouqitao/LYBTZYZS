using System.ComponentModel;
namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 登录成功返回 DTO
    /// </summary>
    public class LoginResponseDto {
        /// <summary>JWT Token</summary>
        [DisplayName("JWT Token")]
        public string Token { get; set; } = string.Empty;
        /// <summary>用户信息</summary>
        [DisplayName("用户信息")]
        public Users.Dtos.UserDto User { get; set; } = new();
    }
}