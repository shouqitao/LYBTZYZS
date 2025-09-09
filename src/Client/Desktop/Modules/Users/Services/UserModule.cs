using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户模块 - UltraThink双层架构纯委托层（精简版）
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// </summary>
public class UserModule(
    IUserQueryService queryService,
    IUserBusinessService businessService) : IUserService
{
    private readonly IUserQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IUserBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    #region 查询操作 - QueryService专业负责

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        => await _queryService.GetByUsernameAsync(username);

    public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        => await _queryService.GetActiveUsersAsync();

    public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    #endregion 查询操作 - QueryService专业负责

    #region 业务操作 - BusinessService专业负责

    public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
        => await _businessService.CreateAsync(dto);

    public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
        => await _businessService.UpdateAsync(dto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteAsync(id);

    #endregion 业务操作 - BusinessService专业负责

    #region 状态管理 - BusinessService批量操作

    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        => await _businessService.EnableAsync(id);

    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        => await _businessService.DisableAsync(id);

    public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        => await _businessService.BatchEnableAsync(ids);

    public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        => await _businessService.BatchDisableAsync(ids);

    #endregion 状态管理 - BusinessService批量操作

    #region 密码管理 - BusinessService安全操作

    public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        => await _businessService.ResetPasswordAsync(id, newPassword);

    public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        => await _businessService.ChangePasswordAsync(id, oldPassword, newPassword);

    #endregion 密码管理 - BusinessService安全操作

    #region 个人信息管理 - BusinessService

    public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
        => await _businessService.ChangeProfileAsync(dto);

    #endregion 个人信息管理 - BusinessService

    #region 辅助功能 - QueryService支持

    public async Task<ServiceResult<List<object>>> GetRolesAsync()
        => await _queryService.GetRolesAsync();

    public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        => await _queryService.ValidateUsernameAsync(username);

    #endregion 辅助功能 - QueryService支持
}
