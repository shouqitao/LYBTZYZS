using LYBT.Shared.Models.Contracts.Users;
using Prism.Events;

namespace LYBT.Desktop.Users.Events
{
    /// <summary>
    /// 用户密码重置事件 - Issue #1928 (Sprint 2)
    /// 功能：在管理员重置用户密码成功后发布此事件通知订阅者
    /// </summary>
    public class UserPasswordResetEvent : PubSubEvent<UserDto>
    {
    }
}
