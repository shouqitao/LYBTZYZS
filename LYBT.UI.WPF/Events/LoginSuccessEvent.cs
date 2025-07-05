using Prism.Events;
using LYBT.Common.Enums.Users;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Events {
    /// <summary>
    /// 登录成功事件，用于在应用程序中传递用户角色信息
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<IList<UserRole>> {
        // 这是一个 Prism 事件类，不需要额外的实现
        // 使用方式：
        // 发布事件：_eventAggregator.GetEvent<LoginSuccessEvent>().Publish(roles);
        // 订阅事件：_eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
    }
}
