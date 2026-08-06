using Prism.Events;

namespace LYBT.Desktop.Registration.Events;

/// <summary>
/// 挂号数据变更通知（US-REG-008）。
/// SignalR 推送或降级轮询触发时发布，订阅方（待诊列表）应重新加载队列。
/// </summary>
public sealed class RegistrationRefreshedEvent : PubSubEvent
{
}
