using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 处方填写完成事件
    /// </summary>
    public class PrescriptionCompletedEvent : PubSubEvent<PrescriptionCompletedPayload>
    {
    }
}
