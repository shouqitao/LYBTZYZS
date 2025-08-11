using System;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Events;
using Prism.Events;

namespace LYBT.Desktop.Consultation.Services.Interfaces
{
    /// <summary>
    /// 看诊事件处理器接口
    /// </summary>
    public interface IConsultationEventHandler
    {
        /// <summary>
        /// 发布患者选择事件
        /// </summary>
        void PublishPatientSelected(PatientInfo patient);

        /// <summary>
        /// 订阅患者选择事件
        /// </summary>
        void SubscribeToPatientSelection(Action<PatientSelectedEventArgs> handler);

        /// <summary>
        /// 发布看诊开始事件
        /// </summary>
        void PublishConsultationStarted(ConsultationInfo consultation);

        /// <summary>
        /// 订阅看诊开始事件
        /// </summary>
        void SubscribeToConsultationStart(Action<ConsultationStartedEventArgs> handler);

        /// <summary>
        /// 发布看诊完成事件
        /// </summary>
        void PublishConsultationCompleted(ConsultationInfo consultation);

        /// <summary>
        /// 订阅看诊完成事件
        /// </summary>
        void SubscribeToConsultationCompletion(Action<ConsultationCompletedEventArgs> handler);

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        void PublishPrescriptionSaved(PrescriptionInfo prescription);

        /// <summary>
        /// 订阅处方保存事件
        /// </summary>
        void SubscribeToPrescriptionSave(Action<PrescriptionSavedEventArgs> handler);

        /// <summary>
        /// 发布数据刷新请求事件
        /// </summary>
        void PublishDataRefreshRequest(DataRefreshType refreshType);

        /// <summary>
        /// 订阅数据刷新请求事件
        /// </summary>
        void SubscribeToDataRefreshRequest(Action<DataRefreshRequestEventArgs> handler);

        /// <summary>
        /// 发布错误事件
        /// </summary>
        void PublishError(string module, string message, Exception? exception = null);

        /// <summary>
        /// 订阅错误事件
        /// </summary>
        void SubscribeToErrors(Action<ErrorEventArgs> handler);

        /// <summary>
        /// 发布导航请求事件
        /// </summary>
        void PublishNavigationRequest(string viewName, object? parameters = null);

        /// <summary>
        /// 订阅导航请求事件
        /// </summary>
        void SubscribeToNavigationRequest(Action<NavigationEventArgs> handler);

        /// <summary>
        /// 发布状态消息
        /// </summary>
        void PublishStatusMessage(string message, StatusMessageType type = StatusMessageType.Info);

        /// <summary>
        /// 订阅状态消息
        /// </summary>
        void SubscribeToStatusMessages(Action<StatusMessageEventArgs> handler);

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        void UnsubscribeAll();
    }
}