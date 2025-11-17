using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户服务优化接口 - 消除双重映射，直接返回Entity
    /// Phase 3 Task 3.3: Service层优化 - Entity直接返回策略
    /// </summary>
    public interface IUserServiceOptimized
    {
        /// <summary>
        /// 获取分页用户数据（直接返回User Entity）
        /// </summary>
        Task<Result<PagedResult<User>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取用户（直接返回User Entity）
        /// </summary>
        Task<Result<User>> GetByIdEntityAsync(Guid id);

        /// <summary>
        /// 创建用户（直接返回User Entity）
        /// </summary>
        Task<Result<User>> CreateEntityAsync(UserInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户（直接返回User Entity）
        /// </summary>
        Task<Result<User>> UpdateEntityAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索用户（直接返回User Entity列表）
        /// </summary>
        Task<Result<List<User>>> SearchEntityAsync(string keyword);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除用户（保持原有DTO结果，因为需要详细的操作报告）
        /// </summary>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 切换用户状态（直接返回User Entity）
        /// </summary>
        Task<Result<User>> ToggleStatusEntityAsync(Guid id);

        /// <summary>
        /// 重置密码（保持原有DTO结果，因为需要返回密码信息）
        /// </summary>
        Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request);

        /// <summary>
        /// 修改个人资料（直接返回User Entity）
        /// </summary>
        Task<Result<User>> ChangeProfileEntityAsync(Guid userId, ChangeProfileDto dto);
    }
}