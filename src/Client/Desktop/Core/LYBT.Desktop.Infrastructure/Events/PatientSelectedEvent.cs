using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload>
    {
    }
}
