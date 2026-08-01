using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.DTOs.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Auth.Application.Mappers;

/// <summary>
/// 认证模块用户映射器 - Mapperly编译时生成
/// 将跨模块用户凭据转换为登录响应所需详情
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AuthUserMapper
{
    /// <summary>
    /// UserCredentialDto转换为UserDetailDto（登录响应）
    /// </summary>
    public partial UserDetailDto ToUserDetailDto(UserCredentialDto user);
}
