using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Users.Helpers
{
    /// <summary>
    /// UserService验证助手类 - UltraThink Helper模式
    /// 负责所有业务验证、规则检查和参数验证逻辑
    /// </summary>
    public class UserValidationHelper
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserValidationHelper> _logger;
        private readonly UserOptions _options;

        public UserValidationHelper(
            IUserRepository userRepository, 
            IMapper mapper, 
            ILogger<UserValidationHelper> logger,
            IOptions<UserOptions> options)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 验证用户创建请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUserCreationAsync(UserCreateDto dto)
        {
            try
            {
                // 基础字段验证
                if (string.IsNullOrWhiteSpace(dto.Username))
                    return ServiceResult<bool>.Failure("用户名不能为空");                if (string.IsNullOrWhiteSpace(dto.RealName))                    return ServiceResult<bool>.Failure("真实姓名不能为空");                // 用户名长度验证
                if (dto.Username.Length < 2)                    return ServiceResult<bool>.Failure("用户名长度不能少于2个字符");                if (dto.Username.Length > 50)                    return ServiceResult<bool>.Failure("用户名长度不能超过50个字符");                // 真实姓名长度验证
                if (dto.RealName.Length > 50)                    return ServiceResult<bool>.Failure("真实姓名长度不能超过50个字符");                // 电话号码格式验证（如果提供）
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && !IsValidPhoneNumber(dto.PhoneNumber))                    return ServiceResult<bool>.Failure("电话号码格式不正确");                // 检查用户名是否已存在
                var usernameExists = await _userRepository.ExistsByUsernameAsync(dto.Username);
                if (usernameExists)                    return ServiceResult<bool>.Failure("用户名已存在");                // 单一角色架构下，角色验证已通过Required特性和默认值处理
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证用户创建请求失败");                return ServiceResult<bool>.Failure("验证用户创建请求失败");            }
        }

        /// <summary>
        /// 验证用户更新请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUserUpdateAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                // 基础字段验证
                if (string.IsNullOrWhiteSpace(dto.RealName))                    return ServiceResult<bool>.Failure("真实姓名不能为空");                // 真实姓名长度验证
                if (dto.RealName.Length > 50)                    return ServiceResult<bool>.Failure("真实姓名长度不能超过50个字符");                // 电话号码格式验证（如果提供）
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && !IsValidPhoneNumber(dto.PhoneNumber))                    return ServiceResult<bool>.Failure("电话号码格式不正确");                // 检查用户是否存在
                var userExists = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                if (userExists == null)                    return ServiceResult<bool>.Failure("要更新的用户不存在");                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证用户更新请求失败: {Id}", id);                return ServiceResult<bool>.Failure("验证用户更新请求失败");            }
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            try
            {
                // 基础验证
                if (string.IsNullOrWhiteSpace(username))                    return ServiceResult<bool>.Failure("用户名不能为空");                if (username.Length < 2)                    return ServiceResult<bool>.Failure("用户名长度不能少于2个字符");                if (username.Length > 50)                    return ServiceResult<bool>.Failure("用户名长度不能超过50个字符");                // 检查是否已存在
                var exists = await _userRepository.ExistsByUsernameAsync(username);
                if (exists)                    return ServiceResult<bool>.Failure("用户名已存在，不可用");                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证用户名失败, Username: {Username}", username);                return ServiceResult<bool>.Failure("验证用户名失败");            }
        }

        /// <summary>
        /// 验证密码重置请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePasswordResetAsync(Guid id, string newPassword)
        {
            try
            {
                // 检查用户是否存在
                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                if (user == null)                    return ServiceResult<bool>.Failure("用户不存在");                // 密码强度验证
                var passwordValidation = ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsSuccess)
                    return passwordValidation;

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证密码重置请求失败: {Id}", id);                return ServiceResult<bool>.Failure("验证密码重置请求失败");            }
        }

        /// <summary>
        /// 验证密码修改请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePasswordChangeAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                // 检查用户是否存在
                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                if (user == null)                    return ServiceResult<bool>.Failure("用户不存在");                // 基础参数验证
                if (string.IsNullOrWhiteSpace(oldPassword))                    return ServiceResult<bool>.Failure("原密码不能为空");                if (string.IsNullOrWhiteSpace(newPassword))                    return ServiceResult<bool>.Failure("新密码不能为空");                if (oldPassword == newPassword)                    return ServiceResult<bool>.Failure("新密码不能与原密码相同");                // 新密码强度验证
                var passwordValidation = ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsSuccess)
                    return passwordValidation;

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证密码修改请求失败: {Id}", id);                return ServiceResult<bool>.Failure("验证密码修改请求失败");            }
        }

        /// <summary>
        /// 验证个人信息修改请求
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateProfileChangeAsync(Guid id, string realName, string phoneNumber)
        {
            try
            {
                // 检查用户是否存在
                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                if (user == null)                    return ServiceResult<bool>.Failure("用户不存在");                // 基础字段验证
                if (string.IsNullOrWhiteSpace(realName))                    return ServiceResult<bool>.Failure("真实姓名不能为空");                // 真实姓名长度验证
                if (realName.Length > 50)                    return ServiceResult<bool>.Failure("真实姓名长度不能超过50个字符");                // 电话号码格式验证（如果提供）
                if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidPhoneNumber(phoneNumber))                    return ServiceResult<bool>.Failure("电话号码格式不正确");                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证个人信息修改请求失败: {Id}", id);                return ServiceResult<bool>.Failure("验证个人信息修改请求失败");            }
        }

        /// <summary>
        /// 验证批量操作
        /// </summary>
        public ServiceResult<bool> ValidateBatchOperation(List<Guid> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)                    return ServiceResult<bool>.Failure("批量操作的ID列表不能为空");                if (ids.Count > _options.MaxBatchOperationSize)                    return ServiceResult<bool>.Failure($"批量操作数量不能超过 {_options.MaxBatchOperationSize}");                // 检查是否有重复ID
                if (ids.Count != ids.Distinct().Count())                    return ServiceResult<bool>.Failure("批量操作的ID列表中存在重复项");                // 检查是否有无效ID
                foreach (var id in ids)
                {
                    if (id == Guid.Empty)                        return ServiceResult<bool>.Failure("批量操作的ID列表中包含无效ID");                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证批量操作失败");                return ServiceResult<bool>.Failure("验证批量操作失败");            }
        }

        /// <summary>
        /// 验证GUID是否有效
        /// </summary>
        public bool IsValidGuid(Guid id)
        {
            return id != Guid.Empty;
        }

        /// <summary>
        /// 验证分页查询参数
        /// </summary>
        public ServiceResult<bool> ValidatePagedQuery(UserPagedQueryDto query)
        {
            try
            {
                if (query.PageIndex < 1)                    return ServiceResult<bool>.Failure("页码必须大于0");                if (query.PageSize < 1)                    return ServiceResult<bool>.Failure("页大小必须大于0");                if (query.PageSize > 1000)                    return ServiceResult<bool>.Failure("页大小不能超过1000");                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "验证分页查询参数失败");                return ServiceResult<bool>.Failure("验证分页查询参数失败");            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 验证电话号码格式
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // 电话号码是可选的

            // 简单的电话号码格式验证（支持手机号和固话）
            var trimmed = phoneNumber.Trim();
            
            // 长度验证
            if (trimmed.Length < 7 || trimmed.Length > 15)
                return false;

            // 字符验证（只允许数字、加号、短横线、空格、括号）
            foreach (char c in trimmed)
            {
                if (!char.IsDigit(c) && c != '+' && c != '-' && c != ' ' && c != '(' && c != ')')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        private ServiceResult<bool> ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))                return ServiceResult<bool>.Failure("密码不能为空");            if (password.Length < 6)                return ServiceResult<bool>.Failure("密码长度不能少于6个字符");            if (password.Length > 100)                return ServiceResult<bool>.Failure("密码长度不能超过100个字符");            // 检查是否包含空格
            if (password.Contains(' '))                return ServiceResult<bool>.Failure("密码不能包含空格");            // 简化的密码强度检查：至少包含一个字母和一个数字
            bool hasLetter = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsLetter(c))
                    hasLetter = true;
                else if (char.IsDigit(c))
                    hasDigit = true;
            }

            if (!hasLetter || !hasDigit)                return ServiceResult<bool>.Failure("密码必须同时包含字母和数字");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证字符串是否为有效搜索关键词
        /// </summary>
        public bool IsValidSearchKeyword(string? keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) && keyword.Trim().Length >= 1;
        }

        /// <summary>
        /// 验证用户状态是否有效
        /// </summary>
        public bool IsValidUserStatus(CommonStatus status)
        {
            return status == CommonStatus.Enabled || status == CommonStatus.Disabled;
        }

        #endregion
    }
}


