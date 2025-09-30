using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 用户数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class UserRepository : BaseApiRepository<UserDto>, IUserRepository
    {
        public UserRepository(
            IApiService apiService,
            ILogger<UserRepository> logger)
            : base(apiService, logger, "api/users")
        {
        }

        public override Task<List<UserDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<UserDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<UserDto> CreateAsync(UserDto user)
        {
            return base.CreateAsync(user);
        }

        public Task<UserDto> UpdateAsync(UserDto user)
        {
            if (user?.Id == null)
            {
                _logger.LogError("Cannot update user with null or invalid id");
                return Task.FromResult<UserDto>(null);
            }
            return base.UpdateAsync(user.Id, user);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public async Task<UserDto> GetByUsernameAsync(string username)
        {
            try
            {
                return await _apiService.GetAsync<UserDto>($"{_endpoint}/username/{username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根据用户名获取用户失败: {username}");
                throw;
            }
        }

        public override Task<List<UserDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            try
            {
                var result = await _apiService.GetAsync<List<UserDto>>($"{_endpoint}/doctors");
                return result ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctors");
                return new List<UserDto>();
            }
        }
    }
}