using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 诊断填写完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedPayload>
    {
    }
}
