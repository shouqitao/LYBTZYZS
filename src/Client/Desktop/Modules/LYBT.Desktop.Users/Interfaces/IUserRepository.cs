using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Interfaces
{
    /// <summary>
    /// 用户数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IUserRepository
    {
        Task<List<UserDto>> GetAllAsync();
        Task<PagedResult<UserDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto> CreateAsync(UserInputDto user);
        Task<UserDto> UpdateAsync(UserInputDto user);
        Task<bool> DeleteAsync(Guid id);
        Task<UserDto> GetByUsernameAsync(string username);
        Task<List<UserDto>> SearchAsync(string keyword);
        Task<List<UserDto>> GetDoctorsAsync();

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="dto">个人资料DTO</param>
        Task<UserDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">修改密码请求</param>
        Task<ServiceResult> ChangePasswordAsync(Guid userId, LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request);

        /// <summary>
        /// 管理员重置用户密码 (Issue #1911)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">重置密码请求数据</param>
        /// <returns>包含新密码的响应结果</returns>
        Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId, 
            ResetPasswordRequestDto request);

        /// <summary>
        /// 批量导入用户 (Issue #2003 Task 2.10)
        /// Desktop主导模式：Desktop解析Excel并组装DTO，Repository调用API
        /// </summary>
        /// <param name="request">批量导入请求（包含用户列表和重复处理策略）</param>
        /// <returns>导入结果</returns>
        Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportRequestDto request);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        Task<UserDto?> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        Task<UserDto?> RestoreAsync(Guid id);
    }
}
