using Prism.Events;
using LYBT.Common.Enums.Users;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Events {
    /// <summary>
    /// 登录成功事件，传递角色信息
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<IList<UserRole>> { }
}
