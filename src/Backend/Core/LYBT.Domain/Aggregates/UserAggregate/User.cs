using System;
using LYBT.Domain.Common;
using LYBT.Domain.Aggregates.UserAggregate.Events;
using LYBT.Domain.Aggregates.UserAggregate.ValueObjects;
using LYBT.Domain.Exceptions;

namespace LYBT.Domain.Aggregates.UserAggregate
{
    /// <summary>
    /// 用户聚合根 - UltraThink重构DDD架构
    /// 管理用户的生命周期和业务规则
    /// </summary>
    public class User : AggregateRoot
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public UserName UserName { get; private set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        public RealName RealName { get; private set; }

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public Email Email { get; private set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public PhoneNumber PhoneNumber { get; private set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public UserRole Role { get; private set; }

        /// <summary>
        /// 密码哈希
        /// </summary>
        public string PasswordHash { get; private set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; private set; }

        /// <summary>
        /// 登录失败次数
        /// </summary>
        public int FailedLoginAttempts { get; private set; }

        /// <summary>
        /// 账户锁定到期时间
        /// </summary>
        public DateTime? LockedUntil { get; private set; }

        // EF Core需要的无参构造函数
        private User() { }

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="realName">真实姓名</param>
        /// <param name="email">邮箱</param>
        /// <param name="role">角色</param>
        /// <param name="passwordHash">密码哈希</param>
        /// <param name="createdBy">创建人</param>
        public static User Create(
            string userName,
            string realName,
            string email,
            UserRole role,
            string passwordHash,
            Guid? createdBy = null)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new UserDomainException("密码哈希不能为空");

            var user = new User
            {
                UserName = UserName.Create(userName),
                RealName = RealName.Create(realName),
                Email = Email.Create(email),
                Role = role ?? throw new UserDomainException("用户角色不能为空"),
                PasswordHash = passwordHash,
                IsActive = true,
                FailedLoginAttempts = 0
            };

            user.SetCreationInfo(createdBy);

            // 发布用户创建领域事件
            user.AddDomainEvent(new UserCreatedEvent(
                user.Id,
                user.UserName.Value,
                user.RealName.Value,
                user.Email.Value,
                user.Role.Name,
                createdBy));

            return user;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="realName">真实姓名</param>
        /// <param name="email">邮箱</param>
        /// <param name="phoneNumber">电话号码</param>
        /// <param name="updatedBy">更新人</param>
        public void UpdateInfo(string realName, string email, string phoneNumber = null, Guid? updatedBy = null)
        {
            var originalEmail = Email?.Value;
            var originalRealName = RealName?.Value;

            if (!string.IsNullOrWhiteSpace(realName))
            {
                RealName = RealName.Create(realName);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                Email = Email.Create(email);
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                PhoneNumber = PhoneNumber.Create(phoneNumber);
            }

            MarkAsUpdated(updatedBy);

            // 如果邮箱或姓名发生变化，发布事件
            if (originalEmail != Email.Value || originalRealName != RealName.Value)
            {
                AddDomainEvent(new UserInfoUpdatedEvent(
                    Id,
                    RealName.Value,
                    Email.Value,
                    originalRealName,
                    originalEmail,
                    updatedBy));
            }
        }

        /// <summary>
        /// 更新用户角色
        /// </summary>
        /// <param name="newRole">新角色</param>
        /// <param name="updatedBy">更新人</param>
        public void UpdateRole(UserRole newRole, Guid updatedBy)
        {
            if (newRole == null)
                throw new UserDomainException("新角色不能为空");

            // 业务规则：不能将管理员降级为普通用户
            if (Role == UserRole.Admin && newRole != UserRole.Admin)
                throw new UserDomainException("管理员角色不能被降级");

            // 业务规则：已停用的用户不能更改角色
            if (!IsActive)
                throw new UserDomainException("已停用的用户不能更改角色");

            var originalRole = Role;
            Role = newRole;
            MarkAsUpdated(updatedBy);

            if (originalRole != newRole)
            {
                AddDomainEvent(new UserRoleUpdatedEvent(
                    Id,
                    UserName.Value,
                    newRole.Name,
                    originalRole?.Name,
                    updatedBy));
            }
        }

        /// <summary>
        /// 更改密码
        /// </summary>
        /// <param name="newPasswordHash">新密码哈希</param>
        /// <param name="updatedBy">更新人</param>
        public void ChangePassword(string newPasswordHash, Guid? updatedBy = null)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new UserDomainException("密码哈希不能为空");

            // 业务规则：已停用的用户不能更改密码
            if (!IsActive)
                throw new UserDomainException("已停用的用户不能更改密码");

            PasswordHash = newPasswordHash;
            MarkAsUpdated(updatedBy);

            AddDomainEvent(new UserPasswordChangedEvent(Id, UserName.Value, updatedBy));
        }

        /// <summary>
        /// 激活用户
        /// </summary>
        /// <param name="activatedBy">激活人</param>
        public void Activate(Guid activatedBy)
        {
            if (IsActive)
                return;

            IsActive = true;
            LockedUntil = null;
            FailedLoginAttempts = 0;
            MarkAsUpdated(activatedBy);

            AddDomainEvent(new UserActivatedEvent(Id, UserName.Value, activatedBy));
        }

        /// <summary>
        /// 停用用户
        /// </summary>
        /// <param name="deactivatedBy">停用人</param>
        /// <param name="reason">停用原因</param>
        public void Deactivate(Guid deactivatedBy, string reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            MarkAsUpdated(deactivatedBy);

            AddDomainEvent(new UserDeactivatedEvent(Id, UserName.Value, reason, deactivatedBy));
        }

        /// <summary>
        /// 记录成功登录
        /// </summary>
        public void RecordSuccessfulLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            FailedLoginAttempts = 0;
            LockedUntil = null;
            
            AddDomainEvent(new UserLoggedInEvent(Id, UserName.Value, LastLoginAt.Value));
        }

        /// <summary>
        /// 记录失败登录
        /// </summary>
        /// <param name="maxFailedAttempts">最大失败次数</param>
        /// <param name="lockoutDurationMinutes">锁定时长（分钟）</param>
        public void RecordFailedLogin(int maxFailedAttempts = 5, int lockoutDurationMinutes = 15)
        {
            // 业务规则：已停用的用户不记录失败登录
            if (!IsActive)
                throw new UserDomainException("已停用的用户无法登录");

            FailedLoginAttempts++;

            if (FailedLoginAttempts >= maxFailedAttempts)
            {
                LockedUntil = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
                
                AddDomainEvent(new UserLockedEvent(
                    Id, 
                    UserName.Value, 
                    FailedLoginAttempts, 
                    LockedUntil.Value));
            }
            else
            {
                AddDomainEvent(new UserLoginFailedEvent(Id, UserName.Value, FailedLoginAttempts));
            }
        }

        /// <summary>
        /// 检查用户是否被锁定
        /// </summary>
        /// <returns>是否被锁定</returns>
        public bool IsLocked()
        {
            return LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
        }

        /// <summary>
        /// 检查是否可以登录
        /// </summary>
        /// <returns>是否可以登录</returns>
        public bool CanLogin()
        {
            return IsActive && !IsLocked();
        }

        /// <summary>
        /// 解锁用户
        /// </summary>
        /// <param name="unlockedBy">解锁人</param>
        public void Unlock(Guid unlockedBy)
        {
            if (!IsLocked())
                return;

            LockedUntil = null;
            FailedLoginAttempts = 0;
            MarkAsUpdated(unlockedBy);

            AddDomainEvent(new UserUnlockedEvent(Id, UserName.Value, unlockedBy));
        }

        /// <summary>
        /// 检查是否为医生
        /// </summary>
        public bool IsDoctor() => Role == UserRole.Doctor;

        /// <summary>
        /// 检查是否为管理员
        /// </summary>
        public bool IsAdmin() => Role == UserRole.Admin;

        /// <summary>
        /// 检查是否具有指定角色
        /// </summary>
        public bool HasRole(UserRole role) => Role == role;
    }
}