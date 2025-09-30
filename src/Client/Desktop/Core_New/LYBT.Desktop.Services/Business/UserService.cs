using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 用户服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public UserService(
            IUserRepository repository,
            ILogger<UserService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allUsers = allUsers.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 分页
                var totalCount = allUsers.Count;
                var items = allUsers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                return ServiceResult<UserDto>.Success(user);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建用户: {dto.Username}");

                // 转换DTO - 注意CreateDto使用Username而不是UserName
                var user = new UserDto
                {
                    Id = Guid.NewGuid(),
                    UserName = dto.Username,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Role = dto.Role,
                    Status = dto.Status,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                // TODO: 密码处理应该在Repository或更底层处理
                // 当前简化实现，实际应该加密dto.Password

                var created = await _repository.CreateAsync(user);
                return ServiceResult<UserDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 更新字段 - UpdateDto的字段是可选的
                if (!string.IsNullOrEmpty(dto.RealName))
                {
                    existing.RealName = dto.RealName;
                }
                
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    existing.PhoneNumber = dto.PhoneNumber;
                }
                
                if (!string.IsNullOrEmpty(dto.Email))
                {
                    existing.Email = dto.Email;
                }
                
                if (dto.Role.HasValue)
                {
                    existing.Role = dto.Role.Value;
                }

                existing.Status = dto.Status;
                existing.UpdateTime = DateTime.UtcNow;

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<UserDto>.Success(updated);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}