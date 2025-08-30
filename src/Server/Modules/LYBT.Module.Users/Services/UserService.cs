using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Helpers;
using LYBT.Module.Users.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户服务实现类 - UltraThink Helper模式重构
    /// 继承BaseService并委托给Helper类处理具体业务逻辑
    /// </summary>
    public class UserService : BaseService<User, UserDto, UserCreateDto, UserUpdateDto>, LYBT.Shared.Interfaces.Services.IUserService
    {
        private readonly Core.UserServiceCore _coreService;
        private readonly UserQueryService _queryService;
        private readonly UserBusinessService _businessService;

        protected override string EntityName => "用户";

        public UserService(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserService> logger,
            Core.UserServiceCore coreService,
            UserQueryService queryService,
            UserBusinessService businessService)
            : base(context, mapper, logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region 查询操作

        /// <summary>
        /// 分页/条件查找用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            return await _queryService.GetByIdAsync(id);
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            return await _queryService.GetByUsernameAsync(username);
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            return await _queryService.GetActiveUsersAsync();
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            return await _queryService.SearchAsync(keyword);
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            return await _queryService.GetRolesAsync();
        }

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            return await _queryService.GetOperationLogsAsync(userId, query);
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            return await _queryService.ValidateUsernameAsync(username);
        }

        #endregion

        #region CRUD操作

        /// <summary>
        /// 新增用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
        {
            var createDto = ConvertToCreateDto(dto);
            return await _businessService.CreateUserAsync(createDto);
        }

        /// <summary>
        /// 编辑用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
        {
            var updateDto = ConvertToUpdateDto(dto);
            return await _businessService.UpdateUserAsync(dto.Id, updateDto);
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _businessService.DeleteUserAsync(id);
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            return await _businessService.DisableAsync(id);
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            return await _businessService.EnableAsync(id);
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            return await _businessService.BatchDisableAsync(ids);
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            return await _businessService.BatchEnableAsync(ids);
        }

        #endregion

        #region 密码管理

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            return await _businessService.ResetPasswordAsync(id, newPassword);
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            return await _businessService.ChangePasswordAsync(id, oldPassword, newPassword);
        }

        /// <summary>
        /// 用户修改个人信息 - UltraThink优化：使用DTO模式保持一致性
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
        {
            return await _businessService.ChangeProfileAsync(dto.UserId, dto.RealName, dto.PhoneNumber ?? string.Empty);
        }

        #endregion

        #region 医生功能兼容接口

        /// <summary>
        /// 获取所有医生（即所有用户）
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            var result = await _queryService.GetDoctorsAsync();
            return result.IsSuccess ? result.Data : new List<UserDto>();
        }

        /// <summary>
        /// 获取医生的今日排班（简化版，默认都在班）
        /// </summary>
        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId)
        {
            var result = await _queryService.IsDoctorAvailableAsync(doctorId);
            return result.IsSuccess && result.Data;
        }

        #endregion

        #region BaseService实现

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected override object GetEntityId(User entity)
        {
            return entity.Id;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将UserMutationDto转换为UserCreateDto（内部辅助方法）
        /// </summary>
        private static UserCreateDto ConvertToCreateDto(UserMutationDto mutationDto)
        {
            return new UserCreateDto
            {
                Username = mutationDto.Username,
                Password = mutationDto.Password ?? "ChangeMe123", // 默认密码
                ConfirmPassword = mutationDto.ConfirmPassword ?? mutationDto.Password ?? "ChangeMe123",
                RealName = mutationDto.RealName,
                Role = mutationDto.Role,
                PhoneNumber = mutationDto.PhoneNumber,
                Email = mutationDto.Email,
                Status = mutationDto.Status
            };
        }
        
        /// <summary>
        /// 将UserMutationDto转换为UserUpdateDto（内部辅助方法）
        /// </summary>
        private static UserUpdateDto ConvertToUpdateDto(UserMutationDto mutationDto)
        {
            return new UserUpdateDto
            {
                RealName = mutationDto.RealName,
                Role = mutationDto.Role,
                PhoneNumber = mutationDto.PhoneNumber,
                Email = mutationDto.Email,
                Status = mutationDto.Status
            };
        }

        #endregion
    }
}