using LYBT.Common.Enums;
using LYBT.Common.Enums.Users;
using Prism.Events;
using System;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Events {
    /// <summary>
    /// 导航到功能界面事件
    /// </summary>
    public class NavigateToFunctionEvent : PubSubEvent<string> { }

    /// <summary>
    /// 导航到集成内容区域事件
    /// </summary>
    public class NavigateToIntegratedContentEvent : PubSubEvent<NavigationArgs> { }

    /// <summary>
    /// 导航到医生档案事件
    /// </summary>
    public class NavigateToDoctorProfileEvent : PubSubEvent<DoctorProfileNavigationArgs> { }

    /// <summary>
    /// 主题切换事件
    /// </summary>
    public class ThemeChangedEvent : PubSubEvent<string> { }

    /// <summary>
    /// 退出登录事件
    /// </summary>
    public class LogoutEvent : PubSubEvent { }

    /// <summary>
    /// 导航完成事件
    /// </summary>
    public class NavigationCompletedEvent : PubSubEvent<string> { }

    /// <summary>
    /// 系统状态更新事件
    /// </summary>
    public class SystemStatusUpdatedEvent : PubSubEvent<string> { }

    /// <summary>
    /// 用户信息更新事件
    /// </summary>
    public class UserInfoUpdatedEvent : PubSubEvent<IList<UserRole>> { }

    /// <summary>
    /// 切换导航抽屉事件
    /// </summary>
    public class ToggleNavDrawerEvent : PubSubEvent { }

    /// <summary>
    /// 医生档案导航参数
    /// </summary>
    public class DoctorProfileNavigationArgs {
        public ProfileMode Mode { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
    }

    /// <summary>
    /// 通用导航参数
    /// </summary>
    public class NavigationArgs {
        public string TargetView { get; set; }
        public string RegionName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}