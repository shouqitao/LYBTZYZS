namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 登录成功返回 DTO
    /// </summary>
    public class LoginResponseDto {

        /// <summary>JWT Token</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>用户信息</summary>
        public Users.Dtos.UserDto User { get; set; } = new();
    }
}