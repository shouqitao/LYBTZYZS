using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Users
{
    // OpenSpec: dto-architecture-specification
    // UserDto空继承别名已删除,统一使用UserDetailDto
    // 参见 docs/architecture/dto-architecture-specification.md

    /// <summary>
    /// 管理员重置密码请求DTO (Issue #1162)
    /// 修复:重置密码不需要传递新密码,使用配置文件中的默认密码
    /// </summary>
    public class ResetPasswordRequestDto
    {
        /// <summary>是否强制用户下次登录时修改密码</summary>
        [DisplayName("强制修改密码")]
        public bool MustChangeOnNextLogin { get; set; } = true;
    }
}
