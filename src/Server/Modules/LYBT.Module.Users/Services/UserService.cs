using LYBT.Module.Users.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Services
{

    /// <summary>
    /// 用户服务 - UltraThink三层架构纯委托模式
    /// </summary>
    public class UserService(
        IUserQueryService queryService,
        IUserBusinessService businessService) : LYBT.Shared.Interfaces.Services.IUserService
    {
        private readonly IUserQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IUserBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region 查询操作

        public Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
            => _queryService.GetPagedAsync(query);

        public Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
            => _queryService.GetByIdAsync(id);

        public Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
            => _queryService.GetByUsernameAsync(username);

        public Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
            => _queryService.GetActiveUsersAsync();

        public Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
            => _queryService.SearchAsync(keyword);

        public Task<ServiceResult<List<object>>> GetRolesAsync()
            => _queryService.GetRolesAsync();

        public Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
            => _queryService.GetOperationLogsAsync(userId, query);

        public Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
            => _queryService.ValidateUsernameAsync(username);

        #endregion 查询操作

        #region Core Operations

        public Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
            => _businessService.CreateUserAsync(dto);

        public Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
            => _businessService.UpdateUserAsync(dto.Id, dto);

        public Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => _businessService.DeleteUserAsync(id);

        #endregion Core Operations

        #region Status Management

        public Task<ServiceResult<bool>> DisableAsync(Guid id)
            => _businessService.DisableAsync(id);

        public Task<ServiceResult<bool>> EnableAsync(Guid id)
            => _businessService.EnableAsync(id);

        public Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
            => _businessService.BatchDisableAsync(ids);

        public Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
            => _businessService.BatchEnableAsync(ids);

        #endregion Status Management

        #region Password Management

        public Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
            => _businessService.ResetPasswordAsync(id, newPassword);

        public Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
            => _businessService.ChangePasswordAsync(id, oldPassword, newPassword);

        public Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
            => _businessService.ChangeProfileAsync(dto.UserId, dto.RealName, dto.PhoneNumber ?? string.Empty);

        #endregion Password Management

        #region Doctor Compatibility

        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            var result = await _queryService.GetDoctorsAsync();
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId)
        {
            var result = await _queryService.IsDoctorAvailableAsync(doctorId);
            return result.IsSuccess && result.Data;
        }

        #endregion Doctor Compatibility
    }
}
