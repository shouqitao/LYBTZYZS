using LYBT.Shared.Models.Contracts.Users;
using Prism.Events;

namespace LYBT.Desktop.Users.Events
{
    /// <summary>
    /// 用户个人资料更新事件 - Issue #1929 (Sprint 3)
    /// 功能：当用户编辑个人资料成功后发布此事件通知订阅者
    /// </summary>
    public class UserProfileUpdatedEvent : PubSubEvent<UserDto>
    {
    }
}
