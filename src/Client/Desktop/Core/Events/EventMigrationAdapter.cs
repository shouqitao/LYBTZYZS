using System;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// UltraThink重构: 事件迁移适配器
    /// 
    /// 提供旧事件系统到新统一架构的平滑迁移
    /// 保持向后兼容性，同时逐步引导使用新架构
    /// </summary>
    public class EventMigrationAdapter
    {
        private readonly UnifiedEventHandler _unifiedEventHandler;
        private readonly ILogger<EventMigrationAdapter> _logger;

        public EventMigrationAdapter(
            UnifiedEventHandler unifiedEventHandler,
            ILogger<EventMigrationAdapter> logger)
        {
            _unifiedEventHandler = unifiedEventHandler ?? throw new ArgumentNullException(nameof(unifiedEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 患者相关事件适配

        /// <summary>
        /// 适配旧的患者选择事件
        /// 兼容 PatientDto 和 PatientSelectedEventArgs
        /// </summary>
        public void PublishPatientSelected(PatientDto patient)
        {
            try
            {
                if (patient == null) return;

                var data = new PatientSelectedData
                {
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    PatientIdNumber = patient.IdNumber,
                    PatientAge = patient.Age,
                    Gender = patient.Gender.ToString(),
                    SourceModule = "EventMigrationAdapter",
                    Message = "患者已选择"
                };

                _unifiedEventHandler.PublishPatientSelected(data);
                _logger.LogDebug($"已适配患者选择事件: {patient.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配患者选择事件时发生异常");
                _unifiedEventHandler.PublishError("EventMigrationAdapter", "适配患者选择事件失败", ex);
            }
        }

        /// <summary>
        /// 向后兼容的患者选择事件订阅
        /// 将新事件数据转换为旧的PatientDto格式
        /// </summary>
        public void SubscribeToPatientSelection(Action<PatientDto> handler)
        {
            _unifiedEventHandler.SubscribeToPatientSelection(data =>
            {
                try
                {
                    var patientInfo = new PatientDto
                    {
                        Id = data.PatientId,
                        Name = data.PatientName,
                        IdNumber = data.PatientIdNumber,
                        BirthDate = data.PatientAge > 0 ? DateTime.Today.AddYears(-data.PatientAge) : null
                        // 转换其他必要字段
                    };

                    handler?.Invoke(patientInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "转换患者选择事件数据时发生异常");
                }
            });
        }

        #endregion

        #region 诊疗相关事件适配

        /// <summary>
        /// 适配诊疗开始事件
        /// </summary>
        public void PublishConsultationStarted(ConsultationDto consultation)
        {
            try
            {
                if (consultation == null) return;

                var data = new ConsultationStartedData
                {
                    ConsultationId = consultation.Id,
                    PatientId = consultation.PatientId,
                    PatientName = "患者", // ConsultationDto没有PatientName属性，使用固定值
                    DoctorId = Guid.Empty, // ConsultationDto没有DoctorId字段，使用默认值
                    DoctorName = "未指定医生", // ConsultationDto没有DoctorName字段，使用默认值
                    MedicalCaseId = consultation.MedicalCaseId,
                    SourceModule = "EventMigrationAdapter",
                    Message = "诊疗已开始"
                };

                _unifiedEventHandler.PublishConsultationStarted(data);
                _logger.LogDebug($"已适配诊疗开始事件: 诊疗ID {consultation.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配诊疗开始事件时发生异常");
                _unifiedEventHandler.PublishError("EventMigrationAdapter", "适配诊疗开始事件失败", ex);
            }
        }

        /// <summary>
        /// 适配诊疗完成事件
        /// </summary>
        public void PublishConsultationCompleted(ConsultationDto consultation)
        {
            try
            {
                if (consultation == null) return;

                var data = new ConsultationCompletedDataNew
                {
                    ConsultationId = consultation.Id,
                    PatientId = consultation.PatientId,
                    PatientName = "患者", // ConsultationDto没有PatientName属性，使用固定值
                    IsSuccessful = true, // 根据业务逻辑判断
                    SourceModule = "EventMigrationAdapter",
                    Message = "诊疗已完成"
                };

                _unifiedEventHandler.PublishConsultationCompleted(data);
                _logger.LogDebug($"已适配诊疗完成事件: 诊疗ID {consultation.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配诊疗完成事件时发生异常");
                _unifiedEventHandler.PublishError("EventMigrationAdapter", "适配诊疗完成事件失败", ex);
            }
        }

        #endregion

        #region 处方相关事件适配

        /// <summary>
        /// 适配处方保存事件
        /// </summary>
        public void PublishPrescriptionSaved(PrescriptionDto prescription)
        {
            try
            {
                if (prescription == null) return;

                var data = new PrescriptionSavedData
                {
                    PrescriptionId = prescription.Id,
                    PatientId = prescription.PatientId,
                    PatientName = "患者", // PrescriptionDto没有PatientName属性，使用固定值
                    ConsultationId = Guid.Empty, // PrescriptionDto没有ConsultationId字段，使用默认值
                    TotalAmount = prescription.TotalPrice,
                    HerbCount = prescription.Items?.Count ?? 0,
                    PrescriptionNumber = prescription.Id.ToString(),
                    SourceModule = "EventMigrationAdapter",
                    Message = "处方已保存"
                };

                _unifiedEventHandler.PublishPrescriptionSaved(data);
                _logger.LogDebug($"已适配处方保存事件: 处方ID {prescription.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配处方保存事件时发生异常");
                _unifiedEventHandler.PublishError("EventMigrationAdapter", "适配处方保存事件失败", ex);
            }
        }

        #endregion

        #region 数据刷新事件适配

        /// <summary>
        /// 适配旧的DataRefreshType到新的DataRefreshScope
        /// </summary>
        public void PublishDataRefreshRequest(DataRefreshType oldRefreshType)
        {
            try
            {
                // 将旧枚举映射到新枚举
                var newRefreshScope = MapDataRefreshType(oldRefreshType);
                _unifiedEventHandler.PublishDataRefreshRequest(newRefreshScope);
                
                _logger.LogDebug($"已适配数据刷新请求: {oldRefreshType} -> {newRefreshScope}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配数据刷新请求时发生异常");
                _unifiedEventHandler.PublishError("EventMigrationAdapter", "适配数据刷新请求失败", ex);
            }
        }

        /// <summary>
        /// 映射旧的DataRefreshType到新的DataRefreshScope
        /// </summary>
        private DataRefreshScope MapDataRefreshType(DataRefreshType oldType)
        {
            return oldType switch
            {
                DataRefreshType.Full => DataRefreshScope.All,
                DataRefreshType.Partial => DataRefreshScope.Consultations,
                DataRefreshType.Incremental => DataRefreshScope.Patients,
                _ => DataRefreshScope.All
            };
        }

        #endregion

        #region 向后兼容的简化方法

        /// <summary>
        /// 简化的错误发布方法，兼容旧代码
        /// </summary>
        public void PublishError(string module, string message, Exception? exception = null)
        {
            _unifiedEventHandler.PublishError(module, message, exception);
        }

        /// <summary>
        /// 简化的状态消息发布方法，兼容旧代码
        /// </summary>
        public void PublishStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
        {
            _unifiedEventHandler.PublishStatusMessage(message, type);
        }

        /// <summary>
        /// 简化的导航请求发布方法，兼容旧代码
        /// </summary>
        public void PublishNavigationRequest(string viewName, object? parameters = null)
        {
            _unifiedEventHandler.PublishNavigationRequest(viewName, parameters);
        }

        #endregion
    }
}