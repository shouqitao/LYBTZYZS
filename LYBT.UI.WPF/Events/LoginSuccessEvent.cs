using Prism.Events;
using LYBT.Module.Users.Dtos;

namespace LYBT.UI.WPF.Events {
    /// <summary>
    /// Published when a user logs in successfully.
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent<UserDto> {
    }
}
