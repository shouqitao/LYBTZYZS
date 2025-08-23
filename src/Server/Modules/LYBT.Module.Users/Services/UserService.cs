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
        private readonly UserQueryHelper _queryHelper;
        private readonly UserValidationHelper _validationHelper;
        private readonly UserBusinessHelper _businessHelper;
        private readonly UserOptions _options;

        protected override string EntityName => "用户";

        public UserService(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserService> logger,
            IOptions<UserOptions> options,
            UserQueryHelper queryHelper,
            UserValidationHelper validationHelper,
            UserBusinessHelper businessHelper)
            : base(context, mapper, logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _queryHelper = queryHelper ?? throw new ArgumentNullException(nameof(queryHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _businessHelper = businessHelper ?? throw new ArgumentNullException(nameof(businessHelper));
        }

        #region 查询操作

        /// <summary>
        /// 分页/条件查找用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetPagedAsync(query),
                "分页查询用户", query);
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdAsync(id),
                "获取用户详情", id);
        }

        /// <summary>
        /// 根据用户名获取用户信息
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByUsernameAsync(username),
                "根据用户名获取用户", username);
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetActiveUsersAsync(),
                "获取活跃用户列表");
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.SearchAsync(keyword),
                "搜索用户", keyword);
        }

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        #region 已废弃功能 - UltraThink精简
        /*
        // 用户统计功能已删除 - 小诊所不需要复杂统计
        public async Task<ServiceResult<object>> GetStatisticsAsync()
        {
            // 功能已废弃，统计需求在前端实现简单计数即可
        }
        */
        #endregion

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetRolesAsync(),
                "获取角色列表");
        }

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetOperationLogsAsync(userId, query),
                "获取用户操作日志", userId);
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            return await ExecuteSafelyAsync(
                async () => await _validationHelper.ValidateUsernameAsync(username),
                "验证用户名", username);
        }

        #endregion

        #region CRUD操作

        /// <summary>
        /// 新增用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
        {
            // 设置为创建操作
            dto.IsCreateOperation = true;
            
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CreateUserAsync(ConvertToCreateDto(dto)),
                "创建用户", dto.Username);
        }

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
        /// 编辑用户 - UltraThink优化：使用统一变更DTO
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
        {
            // 设置为更新操作
            dto.IsCreateOperation = false;
            
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.UpdateUserAsync(dto.Id, ConvertToUpdateDto(dto)),
                "更新用户", dto.Id);
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

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.DeleteUserAsync(id),
                "删除用户", id);
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.DisableUserAsync(id),
                "禁用用户", id);
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.EnableUserAsync(id),
                "启用用户", id);
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.BatchDisableUsersAsync(ids),
                "批量禁用用户", ids);
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.BatchEnableUsersAsync(ids),
                "批量启用用户", ids);
        }

        #endregion

        #region 密码管理

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ResetPasswordAsync(id, newPassword),
                "重置用户密码", id);
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ChangePasswordAsync(id, oldPassword, newPassword),
                "修改用户密码", id);
        }

        /// <summary>
        /// 用户修改个人信息 - UltraThink优化：使用DTO模式保持一致性
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ChangeProfileAsync(dto.UserId, dto.RealName, dto.PhoneNumber ?? string.Empty),
                "修改用户个人信息", dto.UserId);
        }

        #endregion

        #region 医生功能兼容接口


        /// <summary>
        /// 获取所有医生（即所有用户）
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            var result = await ExecuteSafelyAsync(
                async () => await _queryHelper.GetDoctorsAsync(),
                "获取医生列表");
            return result.IsSuccess ? result.Data : new List<UserDto>();
        }

        /// <summary>
        /// 根据科室获取医生
        /// </summary>
        #region 已废弃功能 - 科室管理
        /*
        // 科室管理功能已删除 - 小诊所无需科室划分
        public async Task<List<UserDto>> GetDoctorsByDepartmentAsync(string department)
        {
            // 功能已废弃，直接使用GetDoctorsAsync获取所有医生
        }
        */
        #endregion

        /// <summary>
        /// 获取医生的今日排班（简化版，默认都在班）
        /// </summary>
        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId)
        {
            var result = await ExecuteSafelyAsync(
                async () => await _queryHelper.IsDoctorAvailableAsync(doctorId),
                "检查医生可用性", doctorId);
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
    }
}