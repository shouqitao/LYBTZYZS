using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方事件协调器 - UltraThink专门化组件
    /// 职责单一：专注处方模块内部事件的协调和通信
    /// 代码干净：清晰的事件发布订阅模式
    /// 性能出色：优化的事件处理和内存管理
    /// </summary>
    public class PrescriptionEventCoordinator
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<PrescriptionEventCoordinator> _logger;
        private readonly List<SubscriptionToken> _subscriptions = new();

        public PrescriptionEventCoordinator(
            IEventAggregator eventAggregator,
            ILogger<PrescriptionEventCoordinator> logger)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeToEvents();
        }

        #region 事件定义

        /// <summary>
        /// 处方项添加事件
        /// </summary>
        public class PrescriptionItemAddedEvent : PubSubEvent<PrescriptionItemEventArgs> { }

        /// <summary>
        /// 处方项移除事件
        /// </summary>
        public class PrescriptionItemRemovedEvent : PubSubEvent<PrescriptionItemEventArgs> { }

        /// <summary>
        /// 处方项更新事件
        /// </summary>
        public class PrescriptionItemUpdatedEvent : PubSubEvent<PrescriptionItemEventArgs> { }

        /// <summary>
        /// 处方保存事件
        /// </summary>
        public class PrescriptionSavedEvent : PubSubEvent<PrescriptionEventArgs> { }

        /// <summary>
        /// 处方删除事件
        /// </summary>
        public class PrescriptionDeletedEvent : PubSubEvent<PrescriptionEventArgs> { }

        /// <summary>
        /// 处方状态变更事件
        /// </summary>
        public class PrescriptionStatusChangedEvent : PubSubEvent<PrescriptionStatusEventArgs> { }

        /// <summary>
        /// 价格重算事件
        /// </summary>
        public class PriceRecalculatedEvent : PubSubEvent<PriceEventArgs> { }

        /// <summary>
        /// 验证结果事件
        /// </summary>
        public class ValidationResultEvent : PubSubEvent<ValidationEventArgs> { }

        /// <summary>
        /// 数据同步事件
        /// </summary>
        public class DataSyncEvent : PubSubEvent<DataSyncEventArgs> { }

        #endregion

        #region 事件参数类

        public class PrescriptionItemEventArgs
        {
            public PrescriptionItemViewModel Item { get; set; } = null!;
            public string Action { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Source { get; set; } = string.Empty;
        }

        public class PrescriptionEventArgs
        {
            public Guid PrescriptionId { get; set; }
            public string Action { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Source { get; set; } = string.Empty;
            public object? Data { get; set; }
        }

        public class PrescriptionStatusEventArgs
        {
            public Guid PrescriptionId { get; set; }
            public string OldStatus { get; set; } = string.Empty;
            public string NewStatus { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Reason { get; set; } = string.Empty;
        }

        public class PriceEventArgs
        {
            public decimal SingleDosagePrice { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal DiscountedPrice { get; set; }
            public int ItemCount { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        public class ValidationEventArgs
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Source { get; set; } = string.Empty;
        }

        public class DataSyncEventArgs
        {
            public string SyncType { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public int AffectedCount { get; set; }
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            try
            {
                // 订阅处方项事件
                var itemAddedToken = _eventAggregator.GetEvent<PrescriptionItemAddedEvent>().Subscribe(OnPrescriptionItemAdded);
                var itemRemovedToken = _eventAggregator.GetEvent<PrescriptionItemRemovedEvent>().Subscribe(OnPrescriptionItemRemoved);
                var itemUpdatedToken = _eventAggregator.GetEvent<PrescriptionItemUpdatedEvent>().Subscribe(OnPrescriptionItemUpdated);

                // 订阅处方事件
                var savedToken = _eventAggregator.GetEvent<PrescriptionSavedEvent>().Subscribe(OnPrescriptionSaved);
                var deletedToken = _eventAggregator.GetEvent<PrescriptionDeletedEvent>().Subscribe(OnPrescriptionDeleted);
                var statusChangedToken = _eventAggregator.GetEvent<PrescriptionStatusChangedEvent>().Subscribe(OnPrescriptionStatusChanged);

                // 订阅计算事件
                var priceRecalculatedToken = _eventAggregator.GetEvent<PriceRecalculatedEvent>().Subscribe(OnPriceRecalculated);

                // 订阅验证事件
                var validationToken = _eventAggregator.GetEvent<ValidationResultEvent>().Subscribe(OnValidationResult);

                // 订阅数据同步事件
                var dataSyncToken = _eventAggregator.GetEvent<DataSyncEvent>().Subscribe(OnDataSync);

                // 保存订阅令牌以便后续取消订阅
                _subscriptions.AddRange(new[]
                {
                    itemAddedToken, itemRemovedToken, itemUpdatedToken,
                    savedToken, deletedToken, statusChangedToken,
                    priceRecalculatedToken, validationToken, dataSyncToken
                });

                _logger.LogDebug("处方事件订阅完成，共订阅 {Count} 个事件", _subscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅处方事件失败");
            }
        }

        #endregion

        #region 事件处理

        private void OnPrescriptionItemAdded(PrescriptionItemEventArgs args)
        {
            try
            {
                _logger.LogDebug("处方项已添加: {HerbName}", args.Item.HerbName);

                // 触发价格重算
                PublishPriceRecalculationRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方项添加事件失败");
            }
        }

        private void OnPrescriptionItemRemoved(PrescriptionItemEventArgs args)
        {
            try
            {
                _logger.LogDebug("处方项已移除: {HerbName}", args.Item.HerbName);

                // 触发价格重算
                PublishPriceRecalculationRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方项移除事件失败");
            }
        }

        private void OnPrescriptionItemUpdated(PrescriptionItemEventArgs args)
        {
            try
            {
                _logger.LogDebug("处方项已更新: {HerbName}", args.Item.HerbName);

                // 触发价格重算
                PublishPriceRecalculationRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方项更新事件失败");
            }
        }

        private void OnPrescriptionSaved(PrescriptionEventArgs args)
        {
            try
            {
                _logger.LogInformation("处方已保存: {PrescriptionId}", args.PrescriptionId);

                // 可以触发UI刷新或其他相关操作
                PublishDataSyncEvent("PrescriptionSaved", true, "处方保存成功", 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方保存事件失败");
            }
        }

        private void OnPrescriptionDeleted(PrescriptionEventArgs args)
        {
            try
            {
                _logger.LogInformation("处方已删除: {PrescriptionId}", args.PrescriptionId);

                // 触发数据同步事件
                PublishDataSyncEvent("PrescriptionDeleted", true, "处方删除成功", 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方删除事件失败");
            }
        }

        private void OnPrescriptionStatusChanged(PrescriptionStatusEventArgs args)
        {
            try
            {
                _logger.LogDebug("处方状态变更: {PrescriptionId} 从 {OldStatus} 到 {NewStatus}",
                    args.PrescriptionId, args.OldStatus, args.NewStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方状态变更事件失败");
            }
        }

        private void OnPriceRecalculated(PriceEventArgs args)
        {
            try
            {
                _logger.LogDebug("价格已重算: 总价 {TotalPrice}, 优惠后 {DiscountedPrice}",
                    args.TotalPrice, args.DiscountedPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理价格重算事件失败");
            }
        }

        private void OnValidationResult(ValidationEventArgs args)
        {
            try
            {
                if (args.IsValid)
                {
                    _logger.LogDebug("验证通过");
                }
                else
                {
                    _logger.LogWarning("验证失败: {Errors}", string.Join("; ", args.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理验证结果事件失败");
            }
        }

        private void OnDataSync(DataSyncEventArgs args)
        {
            try
            {
                _logger.LogDebug("数据同步: {SyncType} - {Message}", args.SyncType, args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理数据同步事件失败");
            }
        }

        #endregion

        #region 事件发布方法

        /// <summary>
        /// 发布处方项添加事件
        /// </summary>
        public void PublishPrescriptionItemAdded(PrescriptionItemViewModel item, string source = "")
        {
            try
            {
                var args = new PrescriptionItemEventArgs
                {
                    Item = item,
                    Action = "Added",
                    Source = source
                };

                _eventAggregator.GetEvent<PrescriptionItemAddedEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方项添加事件失败");
            }
        }

        /// <summary>
        /// 发布处方项移除事件
        /// </summary>
        public void PublishPrescriptionItemRemoved(PrescriptionItemViewModel item, string source = "")
        {
            try
            {
                var args = new PrescriptionItemEventArgs
                {
                    Item = item,
                    Action = "Removed",
                    Source = source
                };

                _eventAggregator.GetEvent<PrescriptionItemRemovedEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方项移除事件失败");
            }
        }

        /// <summary>
        /// 发布处方项更新事件
        /// </summary>
        public void PublishPrescriptionItemUpdated(PrescriptionItemViewModel item, string source = "")
        {
            try
            {
                var args = new PrescriptionItemEventArgs
                {
                    Item = item,
                    Action = "Updated",
                    Source = source
                };

                _eventAggregator.GetEvent<PrescriptionItemUpdatedEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方项更新事件失败");
            }
        }

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        public void PublishPrescriptionSaved(Guid prescriptionId, string source = "", object? data = null)
        {
            try
            {
                var args = new PrescriptionEventArgs
                {
                    PrescriptionId = prescriptionId,
                    Action = "Saved",
                    Source = source,
                    Data = data
                };

                _eventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方保存事件失败");
            }
        }

        /// <summary>
        /// 发布价格重算请求
        /// </summary>
        public void PublishPriceRecalculationRequest()
        {
            try
            {
                // 这是一个通知事件，具体的计算由PrescriptionCalculator处理
                var args = new PriceEventArgs();
                _eventAggregator.GetEvent<PriceRecalculatedEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布价格重算请求失败");
            }
        }

        /// <summary>
        /// 发布验证结果事件
        /// </summary>
        public void PublishValidationResult(bool isValid, List<string> errors, List<string> warnings, string source = "")
        {
            try
            {
                var args = new ValidationEventArgs
                {
                    IsValid = isValid,
                    Errors = errors,
                    Warnings = warnings,
                    Source = source
                };

                _eventAggregator.GetEvent<ValidationResultEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布验证结果事件失败");
            }
        }

        /// <summary>
        /// 发布数据同步事件
        /// </summary>
        public void PublishDataSyncEvent(string syncType, bool isSuccess, string message, int affectedCount)
        {
            try
            {
                var args = new DataSyncEventArgs
                {
                    SyncType = syncType,
                    IsSuccess = isSuccess,
                    Message = message,
                    AffectedCount = affectedCount
                };

                _eventAggregator.GetEvent<DataSyncEvent>().Publish(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布数据同步事件失败");
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 取消所有事件订阅
                foreach (var token in _subscriptions)
                {
                    try
                    {
                        token.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "取消事件订阅失败");
                    }
                }

                _subscriptions.Clear();
                _logger.LogDebug("处方事件协调器资源已清理");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理处方事件协调器资源失败");
            }
        }

        #endregion
    }
}
