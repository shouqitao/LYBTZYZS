using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 医案流程取消事件
    /// </summary>
    public class MedicalCaseFlowCancelledEvent : PubSubEvent<MedicalCaseFlowCancelledPayload>
    {
    }
}
