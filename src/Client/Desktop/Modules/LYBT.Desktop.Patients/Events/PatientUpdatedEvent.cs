using LYBT.Shared.Models.Contracts.Patients;
using Prism.Events;

namespace LYBT.Desktop.Patients.Events
{
    /// <summary>
    /// 患者更新事件 - CRUD统一模式
    /// 功能：在患者成功更新后发布此事件通知订阅者
    /// </summary>
    public class PatientUpdatedEvent : PubSubEvent<PatientDto>
    {
    }
}
