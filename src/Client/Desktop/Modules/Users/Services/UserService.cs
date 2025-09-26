using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// 用户服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserApi _userApi;
        private readonly ILogger<UserService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public UserService(
            IUserApi userApi,
            ILogger<UserService> logger,
            IExceptionHandler exceptionHandler)
        {
            _userApi = userApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<UserDto>>(async () =>
            {
                var response = await _userApi.GetUsersAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<UserDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<UserDto>(async () =>
            {
                var response = await _userApi.GetUserByIdAsync(id);
                return ServiceResult<UserDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            return await _exceptionHandler.HandleException<UserDto>(async () =>
            {
                var response = await _userApi.CreateUserAsync(dto);
                return ServiceResult<UserDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<UserDto>(async () =>
            {
                var response = await _userApi.UpdateUserAsync(id, dto);
                return ServiceResult<UserDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _userApi.DeleteUserAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}