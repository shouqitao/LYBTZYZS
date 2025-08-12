using System;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.UserAggregate.Events
{
    /// <summary>
    /// 用户已创建事件 - UltraThink重构DDD架构
    /// 当新用户被创建时触发
    /// </summary>
    public record UserCreatedEvent(
        Guid UserId,
        string UserName,
        string RealName,
        string Email,
        string Role,
        Guid? CreatedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户信息已更新事件
    /// 当用户基本信息（姓名、邮箱等）被更新时触发
    /// </summary>
    public record UserInfoUpdatedEvent(
        Guid UserId,
        string NewRealName,
        string NewEmail,
        string OldRealName,
        string OldEmail,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户角色已更新事件
    /// 当用户角色发生变化时触发
    /// </summary>
    public record UserRoleUpdatedEvent(
        Guid UserId,
        string UserName,
        string NewRole,
        string OldRole,
        Guid UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户密码已更改事件
    /// 当用户密码被更改时触发
    /// </summary>
    public record UserPasswordChangedEvent(
        Guid UserId,
        string UserName,
        Guid? ChangedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户已激活事件
    /// 当用户账户被激活时触发
    /// </summary>
    public record UserActivatedEvent(
        Guid UserId,
        string UserName,
        Guid ActivatedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户已停用事件
    /// 当用户账户被停用时触发
    /// </summary>
    public record UserDeactivatedEvent(
        Guid UserId,
        string UserName,
        string Reason,
        Guid DeactivatedBy
    ) : DomainEvent;

    /// <summary>
    /// 用户已登录事件
    /// 当用户成功登录时触发
    /// </summary>
    public record UserLoggedInEvent(
        Guid UserId,
        string UserName,
        DateTime LoginTime
    ) : DomainEvent;

    /// <summary>
    /// 用户登录失败事件
    /// 当用户登录失败时触发
    /// </summary>
    public record UserLoginFailedEvent(
        Guid UserId,
        string UserName,
        int FailedAttempts
    ) : DomainEvent;

    /// <summary>
    /// 用户已锁定事件
    /// 当用户因多次登录失败被锁定时触发
    /// </summary>
    public record UserLockedEvent(
        Guid UserId,
        string UserName,
        int FailedAttempts,
        DateTime LockedUntil
    ) : DomainEvent;

    /// <summary>
    /// 用户已解锁事件
    /// 当锁定的用户被解锁时触发
    /// </summary>
    public record UserUnlockedEvent(
        Guid UserId,
        string UserName,
        Guid UnlockedBy
    ) : DomainEvent;
}