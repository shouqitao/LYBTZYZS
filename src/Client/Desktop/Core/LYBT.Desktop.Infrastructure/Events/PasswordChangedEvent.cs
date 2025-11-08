using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events;

/// <summary>
/// 密码修改成功事件 - Issue #1906
/// 当用户修改密码成功后发布此事件，触发自动导航到登录界面
/// </summary>
public class PasswordChangedEvent : PubSubEvent
{
    // 无参数，仅用于通知密码已修改
}
