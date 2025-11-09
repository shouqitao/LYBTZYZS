using LYBT.Shared.Models.Contracts.Patients;
using Prism.Events;

namespace LYBT.Desktop.Patients.Events
{
    /// <summary>
    /// 患者创建事件 - CRUD统一模式
    /// 功能：在患者成功创建后发布此事件通知订阅者
    /// </summary>
    public class PatientCreatedEvent : PubSubEvent<PatientDto>
    {
    }
}
