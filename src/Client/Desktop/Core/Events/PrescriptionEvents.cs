using System;
using Prism.Events;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Desktop.Core.Models.Events;
using LYBT.Desktop.Core.Models.Navigation;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 保存步骤数据事件
    /// </summary>
    public class SaveStepDataEvent : PubSubEvent<SaveStepDataEventArgs> { }

    public class SaveStepDataEventArgs : EventArgs
    {
        public string StepName { get; set; } = string.Empty;
        public object? Data { get; set; }
        public bool IsAsync { get; set; }
    }

    /// <summary>
    /// 数据变更事件
    /// </summary>
    public class DataChangedEvent : PubSubEvent<DataChangedEventArgs> { }

    /// <summary>
    /// 导航事件
    /// </summary>
    public class NavigationEvent : PubSubEvent<NavigationInfo> { }

    /// <summary>
    /// 处方编辑器关闭事件
    /// </summary>
    public class PrescriptionComposerClosedEvent : PubSubEvent<PrescriptionComposerClosedEventArgs> { }

    public class PrescriptionComposerClosedEventArgs : EventArgs
    {
        public bool SavedChanges { get; set; }
        public Guid? PrescriptionId { get; set; }
        public string? Reason { get; set; }
    }

}