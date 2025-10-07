using AutoMapper;
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
    /// Desktop Client简化实现，部分业务逻辑委托给Repository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly IMapper _mapper;

        public UserService(
            IUserRepository repository,
            ILogger<UserService> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region 查询操作

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

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"搜索用户: {keyword}");

                var allUsers = await _repository.GetAllAsync();
                var results = allUsers.Where(u =>
                    u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return ServiceResult<List<UserDto>>.Success(results);
            }, nameof(SearchAsync));
        }

        #endregion

        #region 业务操作

        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建用户: {dto.Username}");

                // 使用 AutoMapper 转换 DTO
                var user = _mapper.Map<UserDto>(dto);
                user.Id = Guid.NewGuid();

                // TODO: 密码处理应该在Repository或更底层处理
                // 当前简化实现,实际应该加密dto.Password

                var created = await _repository.CreateAsync(user);
                return ServiceResult<UserDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 使用 AutoMapper 更新字段 (Profile 中已配置条件更新)
                _mapper.Map(dto, existing);

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

        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                    return ServiceResult.Failure("用户不存在");

                user.Status = CommonStatus.Disabled;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult.Success();
            }, nameof(DisableAsync));
        }

        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                    return ServiceResult.Failure("用户不存在");

                user.Status = CommonStatus.Enabled;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult.Success();
            }, nameof(EnableAsync));
        }

        public async Task<ServiceResult> ResetPasswordAsync(Guid id, string newPassword)
        {
            // Desktop Client不实现密码重置逻辑，应该由Server端处理
            return await Task.FromResult(ServiceResult.Failure("Desktop Client不支持密码重置"));
        }

        public async Task<ServiceResult> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            // Desktop Client不实现密码修改逻辑，应该由Server端处理
            return await Task.FromResult(ServiceResult.Failure("Desktop Client不支持密码修改"));
        }

        public async Task<ServiceResult> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult.Failure("用户不存在");

                user.RealName = realName;
                user.PhoneNumber = phoneNumber;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult.Success();
            }, nameof(ChangeProfileAsync));
        }

        #endregion
    }
}
