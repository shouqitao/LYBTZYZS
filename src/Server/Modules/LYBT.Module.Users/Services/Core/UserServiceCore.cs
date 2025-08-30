using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Module.Users;

namespace LYBT.Module.Users.Services.Core
{
    /// <summary>
    /// 用户核心CRUD服务 - UltraThink架构
    /// 职责：基础增删改查操作，状态管理，数据验证
    /// </summary>
    public class UserServiceCore
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserServiceCore> _logger;
        private readonly UserOptions _options;

        public UserServiceCore(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserServiceCore> logger,
            IOptions<UserOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败: {Id}", id);
                return ServiceResult<UserDto>.Failure($"获取用户详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);

                // 检查用户名是否重复
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username);
                if (existingUser != null)
                    return ServiceResult<UserDto>.Failure("用户名已存在");

                // 创建新用户
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = dto.Username,
                    PasswordHash = PasswordHelper.Hash(dto.Password ?? _options.DefaultUserPassword),
                    RealName = dto.RealName,
                    Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Doctor,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Status = dto.Status,
                    PinYinCode = CommonHelper.GetPinyinCode(dto.RealName ?? dto.Username),
                    CreatedTime = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("创建用户成功: {Username} ({Id})", 
                    user.Username, user.Id);

                var resultDto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: Username {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // 更新字段
                user.RealName = dto.RealName;
                user.Role = Enum.TryParse<UserRole>(dto.Role, out var updateRole) ? updateRole : UserRole.Doctor;
                user.PhoneNumber = dto.PhoneNumber;
                user.Email = dto.Email;
                user.Status = dto.Status;
                user.PinYinCode = CommonHelper.GetPinyinCode(dto.RealName ?? user.Username);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新用户成功: {Username} ({Id})", 
                    user.Username, user.Id);

                var resultDto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败: {Id}", id);
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 软删除用户
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 软删除 - 设置状态为禁用
                user.Status = CommonStatus.Disabled;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除用户成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, CommonStatus status)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                var oldStatus = user.Status;
                user.Status = status;
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新用户状态成功: {Username} ({Id}) {OldStatus} -> {NewStatus}", 
                    user.Username, user.Id, oldStatus, status);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"更新用户状态失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(UserCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("用户信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Username))
                return ServiceResult<bool>.Failure("用户名不能为空");

            if (dto.Username.Length < 3 || dto.Username.Length > 50)
                return ServiceResult<bool>.Failure("用户名长度必须在3-50字符之间");

            if (string.IsNullOrWhiteSpace(dto.RealName))
                return ServiceResult<bool>.Failure("真实姓名不能为空");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证更新DTO
        /// </summary>
        private ServiceResult<bool> ValidateUpdateDto(UserUpdateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("用户信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.RealName))
                return ServiceResult<bool>.Failure("真实姓名不能为空");

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}