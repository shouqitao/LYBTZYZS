using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class UserService : Shared.Interfaces.Services.IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dto = new PagedResult<UserDto>
                {
                    Items = _mapper.Map<List<UserDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<UserDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
            }
        }

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(entity);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败");
                return ServiceResult<UserDto>.Failure("获取用户详情失败");
            }
        }

        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<User>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return ServiceResult<UserDto>.Failure("创建用户失败");
            }
        }

        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return ServiceResult<UserDto>.Failure("更新用户失败");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return ServiceResult.Failure("删除用户失败");
            }
        }
    }
}