using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 诊断填写完成事件
    /// Epic #2210 Phase 4: 用于4:6统一工作区的诊断面板与处方面板通信
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedPayload>
    {
    }
}
