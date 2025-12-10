using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 病案事件协调器 - 负责事件发布/订阅协调
    /// Issue #1778: MedicalCase模块组件化改造
    ///
    /// 职责:
    /// - 发布MedicalCaseSavedEvent(病案已保存)
    /// - 发布MedicalCaseCompletedEvent(病案已完成)
    /// - 发布ConsultationStepChangedEvent(诊疗步骤已变更)
    /// - 订阅PatientSelectedEvent(患者已选择)
    /// - 协调跨模块通信
    ///
    /// 新组件类型: IEventCoordinator(Epic #1773首次引入)
    /// </summary>
    public class MedicalCaseEventCoordinator : IDisposable
    {
        #region 字段

        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<MedicalCaseEventCoordinator> _logger;
        private readonly List<SubscriptionToken> _subscriptionTokens;

        #endregion

        #region 构造函数

        public MedicalCaseEventCoordinator(
            IEventAggregator eventAggregator,
            ILogger<MedicalCaseEventCoordinator> logger)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionTokens = new List<SubscriptionToken>();
        }

        #endregion

        #region 事件发布

        /// <summary>
        /// 发布病案已保存事件
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        public void PublishMedicalCaseSaved(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("发布MedicalCaseSavedEvent: {MedicalCaseId}", medicalCaseId);
                // TODO: 定义MedicalCaseSavedEvent
                // _eventAggregator.GetEvent<MedicalCaseSavedEvent>().Publish(medicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布MedicalCaseSavedEvent失败: {MedicalCaseId}", medicalCaseId);
            }
        }

        /// <summary>
        /// 发布病案已完成事件
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        public void PublishMedicalCaseCompleted(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("发布MedicalCaseCompletedEvent: {MedicalCaseId}", medicalCaseId);
                // TODO: 定义MedicalCaseCompletedEvent
                // _eventAggregator.GetEvent<MedicalCaseCompletedEvent>().Publish(medicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布MedicalCaseCompletedEvent失败: {MedicalCaseId}", medicalCaseId);
            }
        }

        // [已移除] PublishConsultationStepChanged - 三步流程已取消

        /// <summary>
        /// 发布处方已创建事件
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        public void PublishPrescriptionCreated(Guid medicalCaseId, Guid prescriptionId)
        {
            try
            {
                _logger.LogInformation("发布PrescriptionCreatedEvent: MedicalCase={MedicalCaseId}, Prescription={PrescriptionId}",
                    medicalCaseId, prescriptionId);
                // TODO: 定义PrescriptionCreatedEvent
                // _eventAggregator.GetEvent<PrescriptionCreatedEvent>().Publish(
                //     new PrescriptionCreatedPayload { MedicalCaseId = medicalCaseId, PrescriptionId = prescriptionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布PrescriptionCreatedEvent失败");
            }
        }

        #endregion

        #region 事件订阅

        /// <summary>
        /// 订阅患者已选择事件
        /// </summary>
        /// <param name="action">事件处理委托</param>
        public void SubscribeToPatientSelected(Action<Guid> action)
        {
            try
            {
                _logger.LogDebug("订阅PatientSelectedEvent");
                // TODO: 定义PatientSelectedEvent
                // var token = _eventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(action);
                // _subscriptionTokens.Add(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅PatientSelectedEvent失败");
            }
        }

        /// <summary>
        /// 订阅病案已保存事件
        /// </summary>
        /// <param name="action">事件处理委托</param>
        public void SubscribeToMedicalCaseSaved(Action<Guid> action)
        {
            try
            {
                _logger.LogDebug("订阅MedicalCaseSavedEvent");
                // TODO: 定义MedicalCaseSavedEvent
                // var token = _eventAggregator.GetEvent<MedicalCaseSavedEvent>().Subscribe(action);
                // _subscriptionTokens.Add(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅MedicalCaseSavedEvent失败");
            }
        }

        // [已移除] SubscribeToConsultationStepChanged - 三步流程已取消

        #endregion

        #region 取消订阅

        /// <summary>
        /// 取消所有事件订阅
        /// </summary>
        public void UnsubscribeAll()
        {
            _logger.LogDebug("取消所有事件订阅, 订阅数: {Count}", _subscriptionTokens.Count);

            foreach (var token in _subscriptionTokens)
            {
                try
                {
                    token.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "取消订阅失败");
                }
            }

            _subscriptionTokens.Clear();
        }

        #endregion

        #region IDisposable实现

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                UnsubscribeAll();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
