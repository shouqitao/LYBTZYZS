using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Users
{
    // OpenSpec: dto-architecture-specification
    // UserDto空继承别名已删除,统一使用UserDetailDto
    // 参见 docs/architecture/dto-architecture-specification.md

    /// <summary>
    /// 管理员重置密码响应DTO (Issue #1162)
    /// </summary>
    public class ResetPasswordResponseDto
    {
        /// <summary>操作是否成功</summary>
        [DisplayName("成功")]
        public bool Success { get; set; }

        /// <summary>临时密码(自动生成时返回)</summary>
        [DisplayName("临时密码")]
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
