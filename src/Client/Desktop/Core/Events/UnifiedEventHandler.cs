using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// UltraThink重构: 统一事件处理器
    /// 
    /// 提供类型安全、统一的事件发布和订阅功能
    /// 取代原有的ConsultationEventHandler，支持所有业务模块
    /// </summary>
    public class UnifiedEventHandler : IDisposable
    {
        #region 依赖注入

        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<UnifiedEventHandler> _logger;
        private readonly List<SubscriptionToken> _subscriptions = new();
        private bool _disposed;

        #endregion

        #region 构造函数

        public UnifiedEventHandler(
            IEventAggregator eventAggregator,
            ILogger<UnifiedEventHandler> logger)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 患者相关事件

        /// <summary>
        /// 发布患者选择事件
        /// </summary>
        public void PublishPatientSelected(PatientSelectedData data)
        {
            try
            {
                _logger.LogInformation($"发布患者选择事件: {data.PatientName} (ID: {data.PatientId})");
                data.SourceModule = "Consultation";
                data.Message ??= "患者已选择";
                
                _eventAggregator.GetEvent<PatientSelectedEventNew>().Publish(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布患者选择事件时发生异常");
                PublishError("UnifiedEventHandler", "发布患者选择事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅患者选择事件
        /// </summary>
        public void SubscribeToPatientSelection(Action<PatientSelectedData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<PatientSelectedEventNew>()
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

        #region 诊疗相关事件

        /// <summary>
        /// 发布诊疗开始事件
        /// </summary>
        public void PublishConsultationStarted(ConsultationStartedData data)
        {
            try
            {
                _logger.LogInformation($"发布诊疗开始事件: 患者ID {data.PatientId}, 诊疗ID {data.ConsultationId}");
                data.SourceModule = "Consultation";
                data.Message ??= "诊疗已开始";
                
                _eventAggregator.GetEvent<ConsultationStartedEventNew>().Publish(data);
                
                // 同时发布状态消息
                PublishStatusMessage("诊疗已开始", StatusMessageType.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布诊疗开始事件时发生异常");
                PublishError("UnifiedEventHandler", "发布诊疗开始事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅诊疗开始事件
        /// </summary>
        public void SubscribeToConsultationStart(Action<ConsultationStartedData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ConsultationStartedEventNew>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅诊疗开始事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅诊疗开始事件时发生异常");
            }
        }

        /// <summary>
        /// 发布诊疗完成事件
        /// </summary>
        public void PublishConsultationCompleted(ConsultationCompletedDataNew data)
        {
            try
            {
                _logger.LogInformation($"发布诊疗完成事件: 诊疗ID {data.ConsultationId}");
                data.SourceModule = "Consultation";
                data.Message ??= "诊疗已完成";
                
                _eventAggregator.GetEvent<ConsultationCompletedEventNew>().Publish(data);
                
                // 同时发布状态消息和数据刷新请求
                PublishStatusMessage("诊疗已完成", StatusMessageType.Success);
                PublishDataRefreshRequest(DataRefreshScope.Consultations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布诊疗完成事件时发生异常");
                PublishError("UnifiedEventHandler", "发布诊疗完成事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅诊疗完成事件
        /// </summary>
        public void SubscribeToConsultationCompletion(Action<ConsultationCompletedDataNew> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ConsultationCompletedEventNew>()
                    .Subscribe(handler, ThreadOption.UIThread, true);
                _subscriptions.Add(token);
                _logger.LogDebug("成功订阅诊疗完成事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅诊疗完成事件时发生异常");
            }
        }

        #endregion

        #region 处方相关事件

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        public void PublishPrescriptionSaved(PrescriptionSavedData data)
        {
            try
            {
                _logger.LogInformation($"发布处方保存事件: 处方ID {data.PrescriptionId}, 包含 {data.HerbCount} 味药材");
                data.SourceModule = "Consultation";
                data.Message ??= "处方已保存";
                
                _eventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(data);
                
                // 同时发布状态消息
                PublishStatusMessage($"处方已保存，共{data.HerbCount}味药材", StatusMessageType.Success);
                PublishDataRefreshRequest(DataRefreshScope.Prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方保存事件时发生异常");
                PublishError("UnifiedEventHandler", "发布处方保存事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅处方保存事件
        /// </summary>
        public void SubscribeToPrescriptionSave(Action<PrescriptionSavedData> handler)
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
        public void PublishDataRefreshRequest(DataRefreshScope refreshScope, string? targetModule = null)
        {
            try
            {
                _logger.LogInformation($"发布数据刷新请求: {refreshScope}");
                
                var data = new DataRefreshRequestData
                {
                    RefreshScope = refreshScope,
                    TargetModule = targetModule,
                    SourceModule = "UnifiedEventHandler",
                    Message = $"请求刷新{refreshScope}数据"
                };
                
                _eventAggregator.GetEvent<DataRefreshRequestEventNew>().Publish(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发布数据刷新请求事件时发生异常: {refreshScope}");
            }
        }

        /// <summary>
        /// 订阅数据刷新请求事件
        /// </summary>
        public void SubscribeToDataRefreshRequest(Action<DataRefreshRequestData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<DataRefreshRequestEventNew>()
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

        #region 导航事件

        /// <summary>
        /// 发布导航请求事件
        /// </summary>
        public void PublishNavigationRequest(string viewName, object? parameters = null, string? regionName = null)
        {
            try
            {
                var data = new NavigationRequestData
                {
                    ViewName = viewName,
                    Parameters = parameters,
                    RegionName = regionName,
                    SourceModule = "UnifiedEventHandler",
                    Message = $"导航到{viewName}"
                };
                
                _logger.LogInformation($"发布导航请求: {viewName}");
                _eventAggregator.GetEvent<NavigationRequestEventNew>().Publish(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发布导航请求事件时发生异常: {viewName}");
                PublishError("UnifiedEventHandler", $"导航到{viewName}失败", ex);
            }
        }

        /// <summary>
        /// 订阅导航请求事件
        /// </summary>
        public void SubscribeToNavigationRequest(Action<NavigationRequestData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<NavigationRequestEventNew>()
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
        public void PublishStatusMessage(string message, StatusMessageType type = StatusMessageType.Info, int duration = 3000)
        {
            try
            {
                var data = new StatusMessageData
                {
                    Message = message,
                    MessageType = type,
                    DisplayDuration = type == StatusMessageType.Error ? 5000 : duration,
                    SourceModule = "UnifiedEventHandler"
                };
                
                _eventAggregator.GetEvent<StatusMessageEventNew>().Publish(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布状态消息事件时发生异常");
                // 避免递归，直接记录日志
                _logger.LogCritical(ex, "发布状态消息事件时发生严重异常");
            }
        }

        /// <summary>
        /// 订阅状态消息事件
        /// </summary>
        public void SubscribeToStatusMessage(Action<StatusMessageData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<StatusMessageEventNew>()
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

        #region 错误处理事件

        /// <summary>
        /// 发布错误事件
        /// </summary>
        public void PublishError(string module, string message, Exception? exception = null, ErrorSeverity severity = ErrorSeverity.Error)
        {
            try
            {
                var data = new ErrorEventData
                {
                    ErrorMessage = message,
                    Exception = exception,
                    Severity = severity,
                    SourceModule = module,
                    Message = message
                };
                
                _logger.LogError(exception, $"[{module}] {message}");
                _eventAggregator.GetEvent<ErrorOccurredEventNew>().Publish(data);
                
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
        public void SubscribeToErrors(Action<ErrorEventData> handler)
        {
            try
            {
                var token = _eventAggregator.GetEvent<ErrorOccurredEventNew>()
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

        #region 资源释放

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // 取消所有订阅
                foreach (var token in _subscriptions)
                {
                    token?.Dispose();
                }
                _subscriptions.Clear();
                
                _logger.LogInformation("UnifiedEventHandler已释放资源");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放UnifiedEventHandler资源时发生异常");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}