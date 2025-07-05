namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 统一API响应DTO
    /// </summary>
    public class ApiResponse<T> {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public ApiResponse() { }
        public ApiResponse(bool success, string? message = null, T? data = default) {
            Success = success;
            Message = message;
            Data = data;
        }
    }

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