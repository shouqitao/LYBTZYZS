using LYBT.Module.Users.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Services {

    /// <summary>
    /// 用户服务 - UltraThink三层架构纯委托模式
    /// </summary>
    public class UserService(
        IUserQueryService queryService,
        IUserBusinessService businessService) : LYBT.Shared.Interfaces.Services.IUserService {
        private readonly IUserQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IUserBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region 查询操作

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
            => await _queryService.GetByUsernameAsync(username);

        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
            => await _queryService.GetActiveUsersAsync();

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<ServiceResult<List<object>>> GetRolesAsync()
            => await _queryService.GetRolesAsync();

        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
            => await _queryService.GetOperationLogsAsync(userId, query);

        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
            => await _queryService.ValidateUsernameAsync(username);

        #endregion 查询操作

        #region Core Operations

        public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
            => await _businessService.CreateUserAsync(dto);

        public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
            => await _businessService.UpdateUserAsync(dto.Id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteUserAsync(id);

        #endregion Core Operations

        #region Status Management

        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
            => await _businessService.DisableAsync(id);

        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
            => await _businessService.EnableAsync(id);

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
            => await _businessService.BatchDisableAsync(ids);

        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
            => await _businessService.BatchEnableAsync(ids);

        #endregion Status Management

        #region Password Management

        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
            => await _businessService.ResetPasswordAsync(id, newPassword);

        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
            => await _businessService.ChangePasswordAsync(id, oldPassword, newPassword);

        public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
            => await _businessService.ChangeProfileAsync(dto.UserId, dto.RealName, dto.PhoneNumber ?? string.Empty);

        #endregion Password Management

        #region Doctor Compatibility

        public async Task<List<UserDto>> GetDoctorsAsync() {
            var result = await _queryService.GetDoctorsAsync();
            return result.IsSuccess ? (result.Data ?? []) : [];
        }

        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId) {
            var result = await _queryService.IsDoctorAvailableAsync(doctorId);
            return result.IsSuccess && result.Data;
        }

        #endregion Doctor Compatibility
    }
}
