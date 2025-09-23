using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Events;
using LYBT.Desktop.Core.Models.Navigation;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{

    /// <summary>
    /// 处方事件协调器 - UltraThink专门化组件
    /// 职责单一：专注处方相关事件的协调和工作流管理
    /// 代码干净：清晰的事件处理和状态同步
    /// 性能出色：高效的事件传播和内存管理
    /// </summary>
    public class PrescriptionEventCoordinator
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<PrescriptionEventCoordinator> _logger;

        // 关联的组件
        private PrescriptionDataManager? _dataManager;

        private PrescriptionCalculator? _calculator;

        public PrescriptionEventCoordinator(
            IEventAggregator eventAggregator,
            ILogger<PrescriptionEventCoordinator> logger)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 依赖注入

        /// <summary>
        /// 设置关联组件
        /// </summary>
        public void SetDependencies(
            PrescriptionDataManager dataManager,
            PrescriptionCalculator calculator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));

            // 初始化事件订阅
            SubscribeToEvents();
        }

        #endregion 依赖注入

        #region 事件订阅

        /// <summary>
        /// 订阅系统事件
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                // 订阅工作流步骤保存事件
                _eventAggregator.GetEvent<SaveStepDataEvent>()
                    .Subscribe(OnSaveStepData);

                // 订阅数据变更事件
                _eventAggregator.GetEvent<DataChangedEvent>()
                    .Subscribe(OnDataChanged);

                // 订阅导航事件
                _eventAggregator.GetEvent<NavigationEvent>()
                    .Subscribe(OnNavigation);

                // 订阅处方相关的专门事件
                _eventAggregator.GetEvent<PrescriptionChangedEvent>()
                    .Subscribe(OnPrescriptionChanged);

                _eventAggregator.GetEvent<HerbAddedEvent>()
                    .Subscribe(OnHerbAdded);

                _eventAggregator.GetEvent<FormulaImportedEvent>()
                    .Subscribe(OnFormulaImported);

                _logger.LogDebug("事件订阅初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化事件订阅失败");
            }
        }

        #endregion 事件订阅

        #region 事件处理方法

        /// <summary>
        /// 处理工作流步骤保存事件
        /// </summary>
        private async void OnSaveStepData(SaveStepDataEventArgs args)
        {
            // 使用适当的async void事件处理器模式
            try
            {
                if (args.StepName != "Prescription" || _dataManager == null)
                {
                    return;
                }

                _logger.LogDebug("处理处方步骤保存事件");

                // 自动保存当前处方数据
                if (_dataManager.HasChanges && _dataManager.PrescriptionItems.Count > 0)
                {
                    await _dataManager.SaveAsync();
                    _logger.LogInformation("工作流触发：处方数据已自动保存");
                }

                // 发布处方保存完成事件
                PublishPrescriptionSaved();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理工作流步骤保存事件失败");
            }
        }

        /// <summary>
        /// 处理数据变更事件
        /// </summary>
        private void OnDataChanged(DataChangedEventArgs changeInfo)
        {
            try
            {
                if (changeInfo.Source != "Prescription")
                {
                    return;
                }

                _logger.LogDebug("处理处方数据变更事件: {DataType}", changeInfo.DataType);

                // 根据变更类型执行相应操作
                switch (changeInfo.DataType)
                {
                    case "ItemAdded":
                    case "ItemRemoved":
                    case "ItemModified":
                        RecalculateAndNotify();
                        break;

                    case "DiscountChanged":
                    case "DosageChanged":
                        RecalculateAndNotify();
                        break;

                    case "PrescriptionCleared":
                        PublishPrescriptionCleared();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理数据变更事件失败");
            }
        }

        /// <summary>
        /// 处理导航事件
        /// </summary>
        private async void OnNavigation(NavigationInfo navInfo)
        {
            // 使用适当的async void事件处理器模式
            try
            {
                // 如果从处方步骤导航出去，检查是否需要保存
                if (navInfo.FromStep == "Prescription" && _dataManager != null)
                {
                    if (_dataManager.HasChanges)
                    {
                        var autoSave = ShouldAutoSave(navInfo);
                        if (autoSave)
                        {
                            await _dataManager.SaveAsync();
                            _logger.LogInformation("导航触发：处方数据已自动保存");
                        }
                    }
                }

                // 如果导航到处方步骤，触发初始化
                if (navInfo.ToStep == "Prescription" && _dataManager != null)
                {
                    await RefreshPrescriptionData(navInfo.MedicalCaseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理导航事件失败");
            }
        }

        /// <summary>
        /// 处理处方变更事件
        /// </summary>
        private void OnPrescriptionChanged(PrescriptionChangeInfo changeInfo)
        {
            try
            {
                _logger.LogDebug("处理处方变更事件: {Action}", changeInfo.Action);

                switch (changeInfo.Action)
                {
                    case "Recalculate":
                        RecalculateAndNotify();
                        break;

                    case "Validate":
                        PublishValidationRequest();
                        break;

                    case "Clear":
                        PublishPrescriptionCleared();
                        break;

                    case "Import":
                        PublishPrescriptionImported();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理处方变更事件失败");
            }
        }

        /// <summary>
        /// 处理药材添加事件
        /// </summary>
        private void OnHerbAdded(HerbAddedInfo herbInfo)
        {
            try
            {
                _logger.LogDebug("处理药材添加事件: {HerbName}", herbInfo.HerbName);

                // 发布药材添加完成事件
                PublishHerbAddedComplete(herbInfo);

                // 触发重新计算
                RecalculateAndNotify();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理药材添加事件失败");
            }
        }

        /// <summary>
        /// 处理验方导入事件
        /// </summary>
        private void OnFormulaImported(FormulaImportedInfo formulaInfo)
        {
            try
            {
                _logger.LogDebug("处理验方导入事件: {FormulaName}", formulaInfo.FormulaName);

                // 发布验方导入完成事件
                PublishFormulaImportedComplete(formulaInfo);

                // 触发重新计算
                RecalculateAndNotify();

                // 标记数据已变更
                if (_dataManager != null)
                {
                    _dataManager.MarkAsChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理验方导入事件失败");
            }
        }

        #endregion 事件处理方法

        #region 事件发布方法

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        public void PublishPrescriptionSaved()
        {
            try
            {
                _eventAggregator.GetEvent<PrescriptionSavedEvent>()
                    .Publish(new PrescriptionSavedInfo
                    {
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty,
                        PrescriptionNo = _dataManager?.PrescriptionNo ?? string.Empty,
                        SavedAt = DateTime.Now,
                        ItemCount = _dataManager?.PrescriptionItems.Count ?? 0
                    });

                _logger.LogDebug("发布处方保存事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方保存事件失败");
            }
        }

        /// <summary>
        /// 发布处方清空事件
        /// </summary>
        public void PublishPrescriptionCleared()
        {
            try
            {
                _eventAggregator.GetEvent<PrescriptionClearedEvent>()
                    .Publish(new PrescriptionClearedInfo
                    {
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty,
                        ClearedAt = DateTime.Now
                    });

                _logger.LogDebug("发布处方清空事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方清空事件失败");
            }
        }

        /// <summary>
        /// 发布价格重算事件
        /// </summary>
        public void PublishPriceRecalculated()
        {
            try
            {
                if (_dataManager == null || _calculator == null)
                {
                    return;
                }

                var calculation = _calculator.CalculatePrescriptionPrice(
                    _dataManager.PrescriptionItems, _dataManager.DosageCount, _dataManager.Discount);

                _eventAggregator.GetEvent<PriceRecalculatedEvent>()
                    .Publish(new PriceRecalculatedInfo
                    {
                        MedicalCaseId = _dataManager.MedicalCaseId,
                        SingleDosagePrice = calculation.SingleDosagePrice,
                        TotalPrice = calculation.TotalPrice,
                        DiscountedPrice = calculation.DiscountedPrice,
                        RecalculatedAt = DateTime.Now
                    });

                _logger.LogDebug("发布价格重算事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布价格重算事件失败");
            }
        }

        /// <summary>
        /// 发布验证请求事件
        /// </summary>
        public void PublishValidationRequest()
        {
            try
            {
                _eventAggregator.GetEvent<ValidationRequestEvent>()
                    .Publish(new ValidationRequestInfo
                    {
                        SourceType = "Prescription",
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty,
                        RequestedAt = DateTime.Now
                    });

                _logger.LogDebug("发布验证请求事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布验证请求事件失败");
            }
        }

        /// <summary>
        /// 发布药材添加完成事件
        /// </summary>
        private void PublishHerbAddedComplete(HerbAddedInfo herbInfo)
        {
            try
            {
                _eventAggregator.GetEvent<HerbAddedCompleteEvent>()
                    .Publish(new HerbAddedCompleteInfo
                    {
                        HerbId = herbInfo.HerbId,
                        HerbName = herbInfo.HerbName,
                        AddedAt = DateTime.Now,
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布药材添加完成事件失败");
            }
        }

        /// <summary>
        /// 发布验方导入完成事件
        /// </summary>
        private void PublishFormulaImportedComplete(FormulaImportedInfo formulaInfo)
        {
            try
            {
                _eventAggregator.GetEvent<FormulaImportedCompleteEvent>()
                    .Publish(new FormulaImportedCompleteInfo
                    {
                        FormulaId = formulaInfo.FormulaId,
                        FormulaName = formulaInfo.FormulaName,
                        ImportedAt = DateTime.Now,
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty,
                        ItemCount = formulaInfo.ItemCount
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布验方导入完成事件失败");
            }
        }

        /// <summary>
        /// 发布处方导入事件
        /// </summary>
        public void PublishPrescriptionImported()
        {
            try
            {
                _eventAggregator.GetEvent<PrescriptionImportedEvent>()
                    .Publish(new PrescriptionImportedInfo
                    {
                        MedicalCaseId = _dataManager?.MedicalCaseId ?? Guid.Empty,
                        ImportType = "Formula",
                        ImportedAt = DateTime.Now,
                        ItemCount = _dataManager?.PrescriptionItems.Count ?? 0
                    });

                _logger.LogDebug("发布处方导入事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布处方导入事件失败");
            }
        }

        #endregion 事件发布方法

        #region 辅助方法

        /// <summary>
        /// 重新计算并通知
        /// </summary>
        private void RecalculateAndNotify()
        {
            try
            {
                if (_dataManager == null || _calculator == null)
                {
                    return;
                }

                // 更新小计
                _calculator.UpdateItemSubtotals(_dataManager.PrescriptionItems);

                // 发布价格重算事件
                PublishPriceRecalculated();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新计算并通知失败");
            }
        }

        /// <summary>
        /// 判断是否应该自动保存
        /// </summary>
        private bool ShouldAutoSave(NavigationInfo navInfo)
        {
            // 根据导航目标决定是否自动保存
            var autoSaveSteps = new[] { "Summary", "Complete", "Print" };
            return Array.Exists(autoSaveSteps, step => step.Equals(navInfo.ToStep, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 刷新处方数据
        /// </summary>
        private async Task RefreshPrescriptionData(Guid medicalCaseId)
        {
            try
            {
                if (_dataManager != null && medicalCaseId != Guid.Empty)
                {
                    await _dataManager.InitializeAsync(medicalCaseId);
                    _logger.LogDebug("处方数据已刷新");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新处方数据失败");
            }
        }

        #endregion 辅助方法

        #region 清理资源

        /// <summary>
        /// 取消事件订阅
        /// </summary>
        public void Unsubscribe()
        {
            try
            {
                _eventAggregator.GetEvent<SaveStepDataEvent>().Unsubscribe(OnSaveStepData);
                _eventAggregator.GetEvent<DataChangedEvent>().Unsubscribe(OnDataChanged);
                _eventAggregator.GetEvent<NavigationEvent>().Unsubscribe(OnNavigation);
                _eventAggregator.GetEvent<PrescriptionChangedEvent>().Unsubscribe(OnPrescriptionChanged);
                _eventAggregator.GetEvent<HerbAddedEvent>().Unsubscribe(OnHerbAdded);
                _eventAggregator.GetEvent<FormulaImportedEvent>().Unsubscribe(OnFormulaImported);

                _logger.LogDebug("事件订阅已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消事件订阅失败");
            }
        }

        #endregion 清理资源
    }

    #region 事件信息类

    public class DataChangeInfo
    {
        public string SourceType { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    public class PrescriptionChangeInfo
    {
        public string Action { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    public class HerbAddedInfo
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class FormulaImportedInfo
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
    }

    public class PrescriptionSavedInfo
    {
        public Guid MedicalCaseId { get; set; }
        public string PrescriptionNo { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }

    public class PrescriptionClearedInfo
    {
        public Guid MedicalCaseId { get; set; }
        public DateTime ClearedAt { get; set; } = DateTime.Now;
    }

    public class PriceRecalculatedInfo
    {
        public Guid MedicalCaseId { get; set; }
        public decimal SingleDosagePrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public DateTime RecalculatedAt { get; set; } = DateTime.Now;
    }

    public class ValidationRequestInfo
    {
        public string SourceType { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }

    public class HerbAddedCompleteInfo
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }

    public class FormulaImportedCompleteInfo
    {
        public Guid FormulaId { get; set; }
        public string FormulaName { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; }
        public int ItemCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
    }

    public class PrescriptionImportedInfo
    {
        public Guid MedicalCaseId { get; set; }
        public string ImportType { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
    }

    #endregion 事件信息类

    #region 事件定义（需要在事件聚合器中注册）

    public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedInfo>
    {
    }

    public class PrescriptionClearedEvent : PubSubEvent<PrescriptionClearedInfo>
    {
    }

    public class PriceRecalculatedEvent : PubSubEvent<PriceRecalculatedInfo>
    {
    }

    public class ValidationRequestEvent : PubSubEvent<ValidationRequestInfo>
    {
    }

    public class PrescriptionChangedEvent : PubSubEvent<PrescriptionChangeInfo>
    {
    }

    public class HerbAddedEvent : PubSubEvent<HerbAddedInfo>
    {
    }

    public class HerbAddedCompleteEvent : PubSubEvent<HerbAddedCompleteInfo>
    {
    }

    public class FormulaImportedEvent : PubSubEvent<FormulaImportedInfo>
    {
    }

    public class FormulaImportedCompleteEvent : PubSubEvent<FormulaImportedCompleteInfo>
    {
    }

    public class PrescriptionImportedEvent : PubSubEvent<PrescriptionImportedInfo>
    {
    }

    #endregion 事件定义（需要在事件聚合器中注册）
}
