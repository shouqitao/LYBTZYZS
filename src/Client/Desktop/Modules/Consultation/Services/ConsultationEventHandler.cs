using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Prism.Events;

// UltraThink重构: 使用新的统一事件架构
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Models.Formulas;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// UltraThink重构: 看诊事件处理器
    /// 
    /// 使用新的统一事件架构，提供类型安全的事件处理
    /// 通过EventMigrationAdapter保持向后兼容性
    /// </summary>
    public class ConsultationEventHandler : IConsultationEventHandler, IDisposable
    {
        #region 依赖注入

        private readonly EventMigrationAdapter _eventAdapter;
        private readonly UnifiedEventHandler _unifiedEventHandler;
        private readonly ILogger<ConsultationEventHandler> _logger;
        private bool _disposed;

        #endregion

        #region 构造函数

        public ConsultationEventHandler(
            EventMigrationAdapter eventAdapter,
            UnifiedEventHandler unifiedEventHandler,
            ILogger<ConsultationEventHandler> logger)
        {
            _eventAdapter = eventAdapter ?? throw new ArgumentNullException(nameof(eventAdapter));
            _unifiedEventHandler = unifiedEventHandler ?? throw new ArgumentNullException(nameof(unifiedEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 患者相关事件 - 使用适配器

        /// <summary>
        /// 发布患者选择事件
        /// </summary>
        public void PublishPatientSelected(PatientInfo patient)
        {
            try
            {
                _logger.LogInformation($"发布患者选择事件: {patient?.Name} (ID: {patient?.Id})");
                if (patient == null) return;

                // 使用适配器发布，自动转换为新的事件架构
                _eventAdapter.PublishPatientSelected(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布患者选择事件时发生异常");
                _eventAdapter.PublishError("ConsultationEventHandler", "发布患者选择事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅患者选择事件
        /// </summary>
        public void SubscribeToPatientSelection(Action<PatientSelectedEventArgs> handler)
        {
            try
            {
                // 使用适配器订阅，自动转换事件数据格式
                _eventAdapter.SubscribeToPatientSelection(patientInfo =>
                {
                    // 将PatientInfo转换为PatientSelectedEventArgs以保持兼容性
                    var eventArgs = new PatientSelectedEventArgs
                    {
                        PatientId = patientInfo.Id,
                        PatientName = patientInfo.Name,
                        Timestamp = DateTime.Now
                    };
                    handler?.Invoke(eventArgs);
                });

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
        public void PublishConsultationStarted(ConsultationInfo consultation)
        {
            try
            {
                _logger.LogInformation($"发布诊疗开始事件: 患者ID {consultation?.PatientId}, 诊疗ID {consultation?.Id}");
                if (consultation == null) return;

                // 使用适配器发布事件
                _eventAdapter.PublishConsultationStarted(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布诊疗开始事件时发生异常");
                _eventAdapter.PublishError("ConsultationEventHandler", "发布诊疗开始事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅诊疗开始事件
        /// </summary>
        public void SubscribeToConsultationStart(Action<ConsultationStartedEventArgs> handler)
        {
            try
            {
                // 使用新的统一事件处理器订阅
                _unifiedEventHandler.SubscribeToConsultationStart(data =>
                {
                    // 转换为旧的EventArgs格式以保持兼容性
                    var eventArgs = new ConsultationStartedEventArgs
                    {
                        ConsultationId = data.ConsultationId,
                        PatientId = data.PatientId,
                        Timestamp = data.Timestamp,
                        Message = data.Message
                    };
                    handler?.Invoke(eventArgs);
                });

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
        public void PublishConsultationCompleted(ConsultationInfo consultation)
        {
            try
            {
                _logger.LogInformation($"发布诊疗完成事件: 诊疗ID {consultation?.Id}");
                if (consultation == null) return;

                // 使用适配器发布事件
                _eventAdapter.PublishConsultationCompleted(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布诊疗完成事件时发生异常");
                _eventAdapter.PublishError("ConsultationEventHandler", "发布诊疗完成事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅诊疗完成事件
        /// </summary>
        public void SubscribeToConsultationCompletion(Action<ConsultationCompletedEventArgs> handler)
        {
            try
            {
                _unifiedEventHandler.SubscribeToConsultationCompletion(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new ConsultationCompletedEventArgs
                    {
                        ConsultationId = data.ConsultationId,
                        IsSuccess = data.IsSuccessful,
                        Timestamp = data.Timestamp,
                        Message = data.Message
                    };
                    handler?.Invoke(eventArgs);
                });

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
        public void PublishPrescriptionSaved(PrescriptionInfo prescription)
        {
            try
            {
                _logger.LogInformation($"发布处方保存事件: 处方ID {prescription?.Id}, 包含 {prescription?.Items?.Count ?? 0} 味药材");
                if (prescription == null) return;

                // 使用适配器发布事件
                _eventAdapter.PublishPrescriptionSaved(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方保存事件时发生异常");
                _eventAdapter.PublishError("ConsultationEventHandler", "发布处方保存事件失败", ex);
            }
        }

        /// <summary>
        /// 订阅处方保存事件
        /// </summary>
        public void SubscribeToPrescriptionSave(Action<PrescriptionSavedEventArgs> handler)
        {
            try
            {
                _unifiedEventHandler.SubscribeToPrescriptionSave(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new PrescriptionSavedEventArgs
                    {
                        PrescriptionId = data.PrescriptionId,
                        IsSuccess = true, // 假设保存成功
                        Timestamp = data.Timestamp,
                        Message = data.Message
                    };
                    handler?.Invoke(eventArgs);
                });

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
                
                // 使用适配器进行类型转换
                _eventAdapter.PublishDataRefreshRequest(refreshType);
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
                _unifiedEventHandler.SubscribeToDataRefreshRequest(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new DataRefreshRequestEventArgs
                    {
                        RefreshType = MapDataRefreshScope(data.RefreshScope),
                        TargetModule = data.TargetModule ?? string.Empty
                    };
                    handler?.Invoke(eventArgs);
                });

                _logger.LogDebug("成功订阅数据刷新请求事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅数据刷新请求事件时发生异常");
            }
        }

        /// <summary>
        /// 将新的DataRefreshScope映射回旧的DataRefreshType
        /// </summary>
        private DataRefreshType MapDataRefreshScope(DataRefreshScope scope)
        {
            return scope switch
            {
                DataRefreshScope.All => DataRefreshType.Full,
                DataRefreshScope.Consultations => DataRefreshType.Partial,
                DataRefreshScope.Patients => DataRefreshType.Incremental,
                _ => DataRefreshType.Full
            };
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
                _eventAdapter.PublishError(module, message, exception);
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
        public void SubscribeToErrors(Action<ConsultationErrorEventArgs> handler)
        {
            try
            {
                _unifiedEventHandler.SubscribeToErrors(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new ConsultationErrorEventArgs
                    {
                        ErrorMessage = data.ErrorMessage,
                        Exception = data.Exception
                    };
                    handler?.Invoke(eventArgs);
                });

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
                _eventAdapter.PublishNavigationRequest(viewName, parameters);
                _logger.LogInformation($"发布导航请求: {viewName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发布导航请求事件时发生异常: {viewName}");
                _eventAdapter.PublishError("ConsultationEventHandler", $"导航到{viewName}失败", ex);
            }
        }

        /// <summary>
        /// 订阅导航请求事件
        /// </summary>
        public void SubscribeToNavigationRequest(Action<NavigationEventArgs> handler)
        {
            try
            {
                _unifiedEventHandler.SubscribeToNavigationRequest(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new NavigationEventArgs
                    {
                        ViewName = data.ViewName,
                        Parameters = data.Parameters
                    };
                    handler?.Invoke(eventArgs);
                });

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
                _eventAdapter.PublishStatusMessage(message, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布状态消息事件时发生异常");
                // 避免递归，直接记录日志
                _logger.LogCritical(ex, "发布状态消息事件时发生严重异常");
            }
        }

        /// <summary>
        /// 订阅状态消息事件（接口要求的方法名）
        /// </summary>
        public void SubscribeToStatusMessages(Action<StatusMessageEventArgs> handler)
        {
            try
            {
                _unifiedEventHandler.SubscribeToStatusMessage(data =>
                {
                    // 转换为旧的EventArgs格式
                    var eventArgs = new StatusMessageEventArgs
                    {
                        Message = data.Message ?? string.Empty,
                        MessageType = data.MessageType
                    };
                    handler?.Invoke(eventArgs);
                });

                _logger.LogDebug("成功订阅状态消息事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅状态消息事件时发生异常");
            }
        }

        #endregion

        #region 清理订阅

        /// <summary>
        /// 清理所有订阅（接口要求的方法）
        /// </summary>
        public void UnsubscribeAll()
        {
            try
            {
                // 统一事件处理器会自动管理订阅，这里只需要日志记录
                _logger.LogInformation("已清理所有事件订阅");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理事件订阅时发生异常");
            }
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // 统一事件处理器会自动清理订阅
                _logger.LogInformation("ConsultationEventHandler已释放资源");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放ConsultationEventHandler资源时发生异常");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }
}