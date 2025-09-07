using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作（统一标准CRUD命名）
/// </summary>
public interface IUserBusinessService
{

    #region 标准CRUD操作

    /// <summary>
    /// 创建用户 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户信息 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid userId);

    #endregion 标准CRUD操作

    #region 状态管理操作

    /// <summary>
    /// 启用用户
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid userId);

    /// <summary>
    /// 禁用用户
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid userId);

    /// <summary>
    /// 批量启用用户 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量禁用用户 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids, CancellationToken cancellationToken = default);

    #endregion 状态管理操作

    #region 密码管理操作

    /// <summary>
    /// 重置用户密码 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改个人信息 - DT-011取消令牌支持
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto profileDto, CancellationToken cancellationToken = default);

    #endregion 密码管理操作
}
