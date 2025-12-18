using LYBT.Shared.Models.Contracts.Users;
using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 登录成功事件
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<UserDetailDto>
    {
    }
}
