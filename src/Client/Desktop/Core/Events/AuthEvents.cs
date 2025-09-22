using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// Fired when a user successfully logs in.
    /// </summary>
    public class LoginSuccessEvent : PubSubEvent
    {
    }

    /// <summary>
    /// Fired to request/logout across modules.
    /// </summary>
    public class LogoutEvent : PubSubEvent
    {
    }

    /// <summary>
    /// Requests to quickly start a consultation workflow.
    /// </summary>
    public class QuickStartConsultationEvent : PubSubEvent
    {
    }
}

