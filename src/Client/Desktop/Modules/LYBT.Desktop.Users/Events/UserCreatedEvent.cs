using LYBT.Shared.Models.Contracts.Users;
using Prism.Events;

namespace LYBT.Desktop.Users.Events
{
    /// <summary>
    /// 用户创建事件 - Issue #1927 (Sprint 1)
    /// 功能：在用户成功创建后发布此事件通知订阅者
    /// </summary>
    public class UserCreatedEvent : PubSubEvent<UserDto>
    {
    }
}
