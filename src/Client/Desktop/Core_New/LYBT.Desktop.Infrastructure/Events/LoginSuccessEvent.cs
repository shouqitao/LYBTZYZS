using Prism.Events;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 登录成功事件
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<UserDto>
    {
    }
}