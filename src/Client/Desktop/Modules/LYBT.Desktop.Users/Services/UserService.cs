using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// 用户Remote Service实现
    /// 通过 IUserRepository 调用远程API
    /// </summary>
    public class UserService : CrudServiceBase<UserListDto, UserDetailDto, UserInputDto>, IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(
            IUserRepository userRepository,
            ILogger<UserService> logger)
            : base(logger, "User")
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        #region Core 实现

        protected override async Task<UserDetailDto> CreateCoreAsync(UserInputDto input, CancellationToken ct)
            => await _userRepository.CreateAsync(input);

        protected override async Task<UserDetailDto> UpdateCoreAsync(UserInputDto input, CancellationToken ct)
            => await _userRepository.UpdateAsync(input);

        protected override async Task DeleteCoreAsync(Guid id, CancellationToken ct)
            => await _userRepository.DeleteAsync(id);

        protected override async Task<UserDetailDto?> GetByIdCoreAsync(Guid id, CancellationToken ct)
            => await _userRepository.GetByIdAsync(id);

        protected override async Task<PagedResult<UserListDto>> GetPagedCoreAsync(int page, int pageSize, string? keyword, CancellationToken ct)
            => await _userRepository.GetPagedAsync(page, pageSize, keyword);

        protected override async Task<List<UserListDto>> SearchCoreAsync(string keyword, CancellationToken ct)
            => await _userRepository.SearchAsync(keyword);

        protected override async Task<UserDetailDto?> ToggleStatusCoreAsync(Guid id, CancellationToken ct)
            => await _userRepository.ToggleStatusAsync(id);

        #endregion

        #region 查询操作

        /// <summary>
        /// 获取所有用户
        /// </summary>
        public new async Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync<List<UserDetailDto>>("User.GetAll", async () =>
            {
                var pagedResult = await _userRepository.GetPagedAsync(1, int.MaxValue);
                var users = new List<UserDetailDto>();
                foreach (var item in pagedResult.Items)
                {
                    var detail = await _userRepository.GetByIdAsync(item.Id);
                    if (detail != null)
                        users.Add(detail);
                }
                return CommandResult<List<UserDetailDto>>.Succeeded(users);
            });
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            return await ExecuteAsync<UserDetailDto>("User.GetByUsername", async () =>
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                    return CommandResult<UserDetailDto>.NotFound("用户不存在");
                return CommandResult<UserDetailDto>.Succeeded(user);
            });
        }

        /// <summary>
        /// 获取医生列表
        /// </summary>
        public async Task<CommandResult<List<UserListDto>>> GetDoctorsAsync(CancellationToken ct = default)
        {
            return await ExecuteAsync<List<UserListDto>>("User.GetDoctors", async () =>
            {
                var doctors = await _userRepository.GetDoctorsAsync();
                return CommandResult<List<UserListDto>>.Succeeded(doctors);
            });
        }

        #endregion

        #region 个人资料管理

        /// <summary>
        /// 修改个人资料
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> ChangeProfileAsync(
            Guid userId, ChangeProfileDto dto, CancellationToken ct = default)
        {
            return await ExecuteAsync<UserDetailDto>("User.ChangeProfile", async () =>
            {
                var user = await _userRepository.ChangeProfileAsync(userId, dto);
                return CommandResult<UserDetailDto>.Succeeded(user);
            });
        }

        #endregion

        #region 密码管理

        /// <summary>
        /// 修改密码
        /// </summary>
        public async Task<CommandResult<bool>> ChangePasswordAsync(
            Guid userId, string oldPassword, string newPassword, CancellationToken ct = default)
        {
            return await ExecuteAsync<bool>("User.ChangePassword", async () =>
            {
                var request = new ChangePasswordRequest
                {
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                var serviceResult = await _userRepository.ChangePasswordAsync(userId, request);

                if (serviceResult.Success)
                    return CommandResult<bool>.Succeeded(true);
                else
                    return CommandResult<bool>.Failed(serviceResult.Error ?? "修改密码失败");
            });
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId, string newPassword, CancellationToken ct = default)
        {
            return await ExecuteAsync<ResetPasswordResponseDto>("User.ResetPassword", async () =>
            {
                var request = new ResetPasswordRequest
                {
                    MustChangeOnNextLogin = true
                };
                var serviceResult = await _userRepository.ResetPasswordAsync(userId, request);

                if (serviceResult.Success)
                    return CommandResult<ResetPasswordResponseDto>.Succeeded(serviceResult.Data!);
                else
                    return CommandResult<ResetPasswordResponseDto>.Failed(serviceResult.Error ?? "重置密码失败");
            });
        }

        #endregion
    }
}
