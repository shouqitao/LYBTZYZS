using System;
using System.Threading.Tasks;
using LYBT.Domain.Aggregates.UserAggregate;
using LYBT.Domain.Aggregates.UserAggregate.ValueObjects;

namespace LYBT.Domain.Services
{
    /// <summary>
    /// 用户领域服务接口 - UltraThink重构DDD架构
    /// 处理跨聚合的用户相关业务逻辑
    /// </summary>
    public interface IUserDomainService
    {
        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="excludeUserId">排除的用户ID（用于更新时检查）</param>
        /// <returns>是否可用</returns>
        Task<bool> IsUserNameAvailableAsync(string userName, Guid? excludeUserId = null);

        /// <summary>
        /// 验证邮箱是否可用
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <param name="excludeUserId">排除的用户ID</param>
        /// <returns>是否可用</returns>
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);

        /// <summary>
        /// 验证密码强度
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>密码强度验证结果</returns>
        PasswordValidationResult ValidatePassword(string password);

        /// <summary>
        /// 生成密码哈希
        /// </summary>
        /// <param name="password">原始密码</param>
        /// <returns>密码哈希</returns>
        string HashPassword(string password);

        /// <summary>
        /// 验证密码
        /// </summary>
        /// <param name="password">原始密码</param>
        /// <param name="hash">密码哈希</param>
        /// <returns>是否匹配</returns>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// 检查用户是否可以执行特定操作
        /// </summary>
        /// <param name="user">执行操作的用户</param>
        /// <param name="targetUser">目标用户（可选）</param>
        /// <param name="operation">操作类型</param>
        /// <returns>是否有权限</returns>
        bool CanPerformOperation(User user, User targetUser, UserOperation operation);
    }

    /// <summary>
    /// 用户领域服务实现
    /// </summary>
    public class UserDomainService : IUserDomainService
    {
        private readonly IUserRepository _userRepository;

        public UserDomainService(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<bool> IsUserNameAvailableAsync(string userName, Guid? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            if (!UserName.IsValid(userName))
                return false;

            var existingUser = await _userRepository.FindByUserNameAsync(userName);
            
            // 如果没有找到用户，说明用户名可用
            if (existingUser == null)
                return true;

            // 如果找到的用户就是当前用户（更新场景），也认为可用
            if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                return true;

            return false;
        }

        /// <summary>
        /// 验证邮箱是否可用
        /// </summary>
        public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (!Email.IsValid(email))
                return false;

            var existingUser = await _userRepository.FindByEmailAsync(email);
            
            if (existingUser == null)
                return true;

            if (excludeUserId.HasValue && existingUser.Id == excludeUserId.Value)
                return true;

            return false;
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        public PasswordValidationResult ValidatePassword(string password)
        {
            var result = new PasswordValidationResult();

            if (string.IsNullOrWhiteSpace(password))
            {
                result.AddError("密码不能为空");
                return result;
            }

            // 长度检查
            if (password.Length < 8)
                result.AddError("密码长度至少8个字符");

            if (password.Length > 100)
                result.AddError("密码长度不能超过100个字符");

            // 复杂度检查
            bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (char.IsPunctuation(c) || char.IsSymbol(c)) hasSpecial = true;
            }

            var complexityCount = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + 
                                 (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

            if (complexityCount < 3)
                result.AddError("密码必须包含大写字母、小写字母、数字、特殊字符中的至少3种");

            // 常见弱密码检查
            var commonPasswords = new[] { "password", "123456", "admin", "root", "user" };
            if (commonPasswords.Any(cp => password.ToLower().Contains(cp)))
                result.AddWarning("密码包含常见弱密码模式");

            // 计算密码强度
            result.Strength = CalculatePasswordStrength(password, hasLower, hasUpper, hasDigit, hasSpecial);

            return result;
        }

        /// <summary>
        /// 生成密码哈希
        /// </summary>
        public string HashPassword(string password)
        {
            // 使用BCrypt进行密码哈希
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查用户是否可以执行特定操作
        /// </summary>
        public bool CanPerformOperation(User user, User targetUser, UserOperation operation)
        {
            if (user == null) return false;
            if (!user.IsActive || !user.CanLogin()) return false;

            return operation switch
            {
                UserOperation.CreateUser => user.Role.CanManageUsers(),
                UserOperation.UpdateUser => CanUpdateUser(user, targetUser),
                UserOperation.DeleteUser => CanDeleteUser(user, targetUser),
                UserOperation.ResetPassword => CanResetPassword(user, targetUser),
                UserOperation.ChangeUserRole => CanChangeRole(user, targetUser),
                UserOperation.ViewUserList => user.Role.CanManageUsers(),
                UserOperation.ViewPatients => user.Role.CanAccessPatientInfo(),
                UserOperation.ManageHerbs => user.Role.CanManageHerbs(),
                UserOperation.Prescribe => user.Role.CanPrescribe(),
                _ => false
            };
        }

        #region Private Methods

        /// <summary>
        /// 计算密码强度
        /// </summary>
        private PasswordStrength CalculatePasswordStrength(string password, bool hasLower, bool hasUpper, bool hasDigit, bool hasSpecial)
        {
            var score = 0;

            // 长度评分
            if (password.Length >= 8) score += 1;
            if (password.Length >= 12) score += 1;
            if (password.Length >= 16) score += 1;

            // 复杂度评分
            if (hasLower) score += 1;
            if (hasUpper) score += 1;
            if (hasDigit) score += 1;
            if (hasSpecial) score += 1;

            // 多样性评分（不重复字符数）
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars >= password.Length * 0.7) score += 1;

            return score switch
            {
                >= 7 => PasswordStrength.Strong,
                >= 5 => PasswordStrength.Medium,
                >= 3 => PasswordStrength.Weak,
                _ => PasswordStrength.VeryWeak
            };
        }

        /// <summary>
        /// 检查是否可以更新用户
        /// </summary>
        private bool CanUpdateUser(User user, User targetUser)
        {
            if (targetUser == null) return false;

            // 用户可以更新自己的信息
            if (user.Id == targetUser.Id) return true;

            // 管理员可以更新其他用户
            if (user.Role.CanManageUsers())
            {
                // 管理员不能修改其他管理员
                return targetUser.Role != UserRole.Admin;
            }

            return false;
        }

        /// <summary>
        /// 检查是否可以删除用户
        /// </summary>
        private bool CanDeleteUser(User user, User targetUser)
        {
            if (targetUser == null) return false;

            // 不能删除自己
            if (user.Id == targetUser.Id) return false;

            // 只有管理员可以删除用户
            if (!user.Role.CanManageUsers()) return false;

            // 管理员不能删除其他管理员
            return targetUser.Role != UserRole.Admin;
        }

        /// <summary>
        /// 检查是否可以重置密码
        /// </summary>
        private bool CanResetPassword(User user, User targetUser)
        {
            if (targetUser == null) return false;

            // 用户可以重置自己的密码
            if (user.Id == targetUser.Id) return true;

            // 管理员可以重置其他用户密码，但不能重置其他管理员的密码
            if (user.Role.CanManageUsers())
                return targetUser.Role != UserRole.Admin;

            return false;
        }

        /// <summary>
        /// 检查是否可以更改角色
        /// </summary>
        private bool CanChangeRole(User user, User targetUser)
        {
            if (targetUser == null) return false;

            // 不能更改自己的角色
            if (user.Id == targetUser.Id) return false;

            // 只有管理员可以更改角色
            if (!user.Role.CanManageUsers()) return false;

            // 不能更改管理员的角色
            return targetUser.Role != UserRole.Admin;
        }

        #endregion
    }

    #region 辅助类和枚举

    /// <summary>
    /// 用户操作类型枚举
    /// </summary>
    public enum UserOperation
    {
        CreateUser,
        UpdateUser,
        DeleteUser,
        ResetPassword,
        ChangeUserRole,
        ViewUserList,
        ViewPatients,
        ManageHerbs,
        Prescribe
    }

    /// <summary>
    /// 密码强度枚举
    /// </summary>
    public enum PasswordStrength
    {
        VeryWeak,
        Weak,
        Medium,
        Strong
    }

    /// <summary>
    /// 密码验证结果
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public PasswordStrength Strength { get; set; }

        public void AddError(string error)
        {
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// 用户仓储接口扩展（领域层定义）
    /// </summary>
    public interface IUserRepository
    {
        Task<User> FindByIdAsync(Guid id);
        Task<User> FindByUserNameAsync(string userName);
        Task<User> FindByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }

    #endregion
}