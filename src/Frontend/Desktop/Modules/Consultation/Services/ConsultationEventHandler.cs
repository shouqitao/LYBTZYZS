using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Events;
using LYBT.WPF.Client.Modules.Consultation.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 看诊事件处理器 - 负责处理和协调看诊模块的所有事件
    /// </summary>
    public class ConsultationEventHandler : IConsultationEventHandler, IDisposable
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ConsultationEventHandler> _logger;
        private readonly List<SubscriptionToken> _subscriptions = new();
        private bool _disposed;

        public ConsultationEventHandler(
            IEventAggregator eventAggregator,
            ILogger<ConsultationEventHandler> logger)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        #region 患者相关事件

        /// <summary>
        /// 发布患者选择事件
        /// </summary>
        public void PublishPatientSelected(PatientInfo patient)
        {
            try
            {
                _logger.LogInformation($"发布患者选择事件: {patient?.Name} (ID: {patient?.Id})");
                var eventArgs = new PatientSelectedEventArgs(patient.Id, patient.Name);
                _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布患者选择事件时发生异常");
                PublishError("ConsultationEventHandler", "发布患者选择事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅患者选择事件
        /// </summary>
        public void SubscribeToPatientSelection(Action<PatientSelectedEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<PatientSelectedEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅患者选择事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅患者选择事件时发生异常");
            }
        }

        #endregion

        #region 看诊相关事件

        /// <summary>
        /// 发布看诊开始事件
        /// </summary>
        public void PublishConsultationStarted(ConsultationInfo consultation)
        {
            try
            {
                _logger.LogInformation($"发布看诊开始事件: 患者ID {consultation?.PatientId}, 看诊ID {consultation?.Id}");
                var eventArgs = new ConsultationStartedEventArgs(consultation.Id, consultation.PatientId, consultation.PatientName);
                _eventAggregator.GetEvent<ConsultationStartedEvent>().Publish(eventArgs);
                
                // 同时发布状态消息
                PublishStatusMessage("看诊已开始", StatusMessageType.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布看诊开始事件时发生异常");
                PublishError("ConsultationEventHandler", "发布看诊开始事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅看诊开始事件
        /// </summary>
        public void SubscribeToConsultationStart(Action<ConsultationStartedEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ConsultationStartedEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅看诊开始事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅看诊开始事件时发生异常");
            }
        }

        /// <summary>
        /// 发布看诊完成事件
        /// </summary>
        public void PublishConsultationCompleted(ConsultationInfo consultation)
        {
            try
            {
                _logger.LogInformation($"发布看诊完成事件: 看诊ID {consultation?.Id}");
                var eventArgs = new ConsultationCompletedEventArgs(consultation.Id, consultation.PatientId, consultation.PatientName);
                _eventAggregator.GetEvent<ConsultationCompletedEvent>().Publish(eventArgs);
                
                // 同时发布状态消息和数据刷新请求
                PublishStatusMessage("看诊已完成", StatusMessageType.Success);
                PublishDataRefreshRequest(DataRefreshType.Consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布看诊完成事件时发生异常");
                PublishError("ConsultationEventHandler", "发布看诊完成事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅看诊完成事件
        /// </summary>
        public void SubscribeToConsultationCompletion(Action<ConsultationCompletedEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ConsultationCompletedEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅看诊完成事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅看诊完成事件时发生异常");
            }
        }

        #endregion

        #region 处方相关事件

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        public void PublishPrescriptionSaved(PrescriptionInfo prescription)
        {
            try
            {
                _logger.LogInformation($"发布处方保存事件: 处方ID {prescription?.Id}, 包含 {prescription?.Items?.Count ?? 0} 味药材");
                
                var eventArgs = new PrescriptionSavedEventArgs(
                    prescription.Id,
                    prescription.PatientId,
                    prescription.PatientName ?? "未知患者",
                    prescription.TotalAmount
                );
                _eventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(eventArgs);
                
                // 同时发布状态消息
                PublishStatusMessage($"处方已保存，共{prescription?.Items?.Count ?? 0}味药材", StatusMessageType.Success);
                PublishDataRefreshRequest(DataRefreshType.Prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方保存事件时发生异常");
                PublishError("ConsultationEventHandler", "发布处方保存事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅处方保存事件
        /// </summary>
        public void SubscribeToPrescriptionSave(Action<PrescriptionSavedEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<PrescriptionSavedEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅处方保存事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅处方保存事件时发生异常");
            }
        }

        #endregion

        #region 数据刷新事件

        /// <summary>
        /// 发布数据刷新请求事件
        /// </summary>
        public void PublishDataRefreshRequest(DataRefreshType refreshType)
        {
            try
            {
                _logger.LogInformation($"发布数据刷新请求: {refreshType}");
                var eventArgs = new DataRefreshRequestEventArgs(refreshType);
                _eventAggregator.GetEvent<DataRefreshRequestEvent>().Publish(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发布数据刷新请求事件时发生异常: {refreshType}");
            }
        }

        /// <summary>
        /// 订阅数据刷新请求事件
        /// </summary>
        public void SubscribeToDataRefreshRequest(Action<DataRefreshRequestEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<DataRefreshRequestEvent>()
                    .Subscribe(handler, ThreadOption.BackgroundThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅数据刷新请求事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅数据刷新请求事件时发生异常");
            }
        }

        #endregion

        #region 错误处理事件

        /// <summary>
        /// 发布错误事件
        /// </summary>
        public void PublishError(string module, string message, Exception? exception = null)
        {
            try
            {
                var severity = exception != null ? ErrorSeverity.Error : ErrorSeverity.Warning;
                var errorArgs = exception != null 
                    ? new ErrorEventArgs(message, exception)
                    : new ErrorEventArgs(message, module, severity);
                errorArgs.Module = module;

                _logger.LogError(exception, $"[{module}] {message}");
                _eventAggregator.GetEvent<ErrorOccurredEvent>().Publish(errorArgs);
                
                // 同时发布错误状态消息
                PublishStatusMessage(message, StatusMessageType.Error);
            }
            catch (Exception ex)
            {
                // 避免递归，直接记录日志
                _logger.LogCritical(ex, "发布错误事件时发生严重异常");
            }
        }

        /// <summary>
        /// 订阅错误事件
        /// </summary>
        public void SubscribeToErrors(Action<ErrorEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ErrorOccurredEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅错误事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅错误事件时发生异常");
            }
        }

        #endregion

        #region 导航事件

        /// <summary>
        /// 发布导航请求事件
        /// </summary>
        public void PublishNavigationRequest(string viewName, object? parameters = null)
        {
            try
            {
                var navArgs = parameters != null 
                    ? new NavigationEventArgs(viewName, parameters)
                    : new NavigationEventArgs(viewName);

                _logger.LogInformation($"发布导航请求: {viewName}");
                _eventAggregator.GetEvent<NavigationRequestEvent>().Publish(navArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发布导航请求事件时发生异常: {viewName}");
                PublishError("ConsultationEventHandler", $"导航到{viewName}失败", ex);
            }
        }

        /// <summary>
        /// 订阅导航请求事件
        /// </summary>
        public void SubscribeToNavigationRequest(Action<NavigationEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<NavigationRequestEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅导航请求事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅导航请求事件时发生异常");
            }
        }

        #endregion

        #region 状态消息事件

        /// <summary>
        /// 发布状态消息
        /// </summary>
        public void PublishStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
        {
            try
            {
                var duration = type == StatusMessageType.Error ? 5000 : 3000; // 错误消息显示更久
                var statusArgs = new StatusMessageEventArgs(message, type, duration);

                _logger.LogInformation($"[{type}] {message}");
                _eventAggregator.GetEvent<StatusMessageEvent>().Publish(statusArgs);
            }
            catch (Exception ex)
            {
                // 避免递归，直接记录日志
                _logger.LogError(ex, "发布状态消息时发生异常");
            }
        }

        /// <summary>
        /// 订阅状态消息
        /// </summary>
        public void SubscribeToStatusMessages(Action<StatusMessageEventArgs> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<StatusMessageEvent>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅状态消息事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅状态消息事件时发生异常");
            }
        }

        #endregion

        #region 清理和释放

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        public void UnsubscribeAll()
        {
            try
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();
                _logger.LogInformation("已清理所有事件订阅");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理事件订阅时发生异常");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnsubscribeAll();
                }
                _disposed = true;
            }
        }

        #endregion
    }
}