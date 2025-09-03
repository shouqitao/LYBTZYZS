using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Prescriptions.ViewModels.Components;
using LYBT.Desktop.Prescriptions.Constants;
using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;
using IPrescriptionService = LYBT.Shared.Interfaces.Services.IPrescriptionService;
using IHerbService = LYBT.Shared.Interfaces.Services.IHerbService; // UltraThink: 使用共享服务接口
using Prism.Services.Dialogs;
// using Prism.Dialogs; // Removed for Prism 8.1.97 compatibility

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 重构后的处方ViewModel - UltraThink架构实现
    /// 职责单一：作为5个专门组件的协调器和UI绑定层
    /// 代码干净：简洁的组件组合和清晰的职责分离
    /// 性能出色：优化的组件协作和资源管理
    /// 
    /// 从原来的672行超大文件，重构为简洁的协调器模式：
    /// - PrescriptionDataManager: 数据管理
    /// - PrescriptionValidator: 验证逻辑
    /// - PrescriptionCalculator: 计算引擎
    /// - PrescriptionCommandHandler: 命令处理
    /// - PrescriptionEventCoordinator: 事件协调
    /// </summary>
    public class PrescriptionViewModelRefactored : BindableBase, IDisposable
    {
        #region UltraThink专门化组件

        private readonly PrescriptionDataManager _dataManager;
        private readonly PrescriptionValidator _validator;
        private readonly PrescriptionCalculator _calculator;
        private readonly PrescriptionCommandHandler _commandHandler;
        private readonly PrescriptionEventCoordinator _eventCoordinator;
        private readonly ILogger<PrescriptionViewModelRefactored> _logger;

        #endregion

        #region 计算属性（通过组件提供）

        private PrescriptionCalculator.CalculationResult _currentCalculation = new();

        /// <summary>
        /// 单剂价格
        /// </summary>
        public decimal SingleDosagePrice => _currentCalculation.SingleDosagePrice;

        /// <summary>
        /// 总价
        /// </summary>
        public decimal TotalPrice => _currentCalculation.TotalPrice;

        /// <summary>
        /// 优惠后价格
        /// </summary>
        public decimal DiscountedPrice => _currentCalculation.DiscountedPrice;

        /// <summary>
        /// 折扣文本
        /// </summary>
        public string DiscountText => _currentCalculation.DiscountText;

        #endregion

        #region UI绑定属性（委托给DataManager）

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId => _dataManager.MedicalCaseId;

        /// <summary>
        /// 处方编号
        /// </summary>
        public string PrescriptionNo
        {
            get => _dataManager.PrescriptionNo;
            set
            {
                _dataManager.PrescriptionNo = value;
                _dataManager.MarkAsChanged();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 处方项集合
        /// </summary>
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems => _dataManager.PrescriptionItems;

        /// <summary>
        /// 选中的处方项
        /// </summary>
        public PrescriptionItemViewModel? SelectedItem
        {
            get => _dataManager.SelectedItem;
            set
            {
                _dataManager.SelectedItem = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _dataManager.DosageCount;
            set
            {
                _dataManager.DosageCount = value;
                _dataManager.MarkAsChanged();
                RecalculatePrice();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 用法
        /// </summary>
        public string Usage
        {
            get => _dataManager.Usage;
            set
            {
                _dataManager.Usage = value;
                _dataManager.MarkAsChanged();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 医嘱
        /// </summary>
        public string MedicalAdvice
        {
            get => _dataManager.MedicalAdvice;
            set
            {
                _dataManager.MedicalAdvice = value;
                _dataManager.MarkAsChanged();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark
        {
            get => _dataManager.Remark;
            set
            {
                _dataManager.Remark = value;
                _dataManager.MarkAsChanged();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 折扣
        /// </summary>
        public decimal Discount
        {
            get => _dataManager.Discount;
            set
            {
                _dataManager.Discount = value;
                _dataManager.MarkAsChanged();
                RecalculatePrice();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DiscountText));
            }
        }

        /// <summary>
        /// 加载状态
        /// </summary>
        public bool IsLoading => _dataManager.IsLoading;

        /// <summary>
        /// 是否有变更
        /// </summary>
        public bool HasChanges => _dataManager.HasChanges;

        #endregion

        #region 常量和提示属性

        public string UsageHint => PrescriptionConstants.UsageHint;
        public string MedicalAdviceHint => PrescriptionConstants.MedicalAdviceHint;
        public string RemarkHint => PrescriptionConstants.RemarkHint;

        public ObservableCollection<int> CommonDosageCounts { get; } = new()
            { 1, 3, 5, 7, 10, 14, 21, 28 };

        public ObservableCollection<string> CommonUsages { get; } = new()
        {
            "水煎服，一日三次，饭后服用",
            "水煎服，一日二次，早晚空腹服用",
            "开水冲服，一日三次",
            "温开水送服，一日二次"
        };

        #endregion

        #region 命令属性（委托给CommandHandler）

        public ICommand SaveCommand => _commandHandler.SaveCommand;
        public ICommand ClearCommand => _commandHandler.ClearCommand;
        public ICommand AddHerbCommand => _commandHandler.AddHerbCommand;
        public ICommand RemoveHerbCommand => _commandHandler.RemoveHerbCommand;
        public ICommand ImportFormulaCommand => _commandHandler.ImportFormulaCommand;
        public ICommand ImportHistoryCommand => _commandHandler.ImportHistoryCommand;
        public ICommand SetDiscountCommand => _commandHandler.SetDiscountCommand;
        public ICommand SetDosageCommand => _commandHandler.SetDosageCommand;
        public ICommand GeneratePrescriptionNoCommand => _commandHandler.GeneratePrescriptionNoCommand;
        public ICommand PrintPreviewCommand => _commandHandler.PrintPreviewCommand;
        public ICommand ValidateCommand => _commandHandler.ValidateCommand;
        public ICommand RecalculateCommand => _commandHandler.RecalculateCommand;

        #endregion

        #region 构造函数

        public PrescriptionViewModelRefactored(
            IEventAggregator eventAggregator,
            IPrescriptionService prescriptionService,
            LYBT.Shared.Interfaces.Services.IHerbService herbService,
            IFormulaService formulaService,
            Prism.Services.Dialogs.IDialogService dialogService,
            ILogger<PrescriptionViewModelRefactored> logger,
            ILogger<PrescriptionDataManager> dataManagerLogger,
            ILogger<PrescriptionValidator> validatorLogger,
            ILogger<PrescriptionCalculator> calculatorLogger,
            ILogger<PrescriptionCommandHandler> commandHandlerLogger,
            ILogger<PrescriptionEventCoordinator> eventCoordinatorLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _logger.LogDebug("开始初始化重构后的PrescriptionViewModel");

                // 创建专门化组件
                _dataManager = new PrescriptionDataManager(prescriptionService, dataManagerLogger);
                _validator = new PrescriptionValidator(validatorLogger);
                _calculator = new PrescriptionCalculator(calculatorLogger);
                _commandHandler = new PrescriptionCommandHandler(herbService, formulaService, prescriptionService, dialogService, commandHandlerLogger);
                _eventCoordinator = new PrescriptionEventCoordinator(eventAggregator, eventCoordinatorLogger);

                // 建立组件间的依赖关系
                EstablishComponentDependencies();

                // 初始化事件订阅
                SubscribeToComponentEvents();

                _logger.LogInformation("PrescriptionViewModel重构完成，组件化架构已建立");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化PrescriptionViewModel失败");
                throw;
            }
        }

        #endregion

        #region 组件依赖建立

        /// <summary>
        /// 建立组件间的依赖关系
        /// </summary>
        private void EstablishComponentDependencies()
        {
            try
            {
                // CommandHandler需要DataManager、Validator和Calculator
                _commandHandler.SetDependencies(_dataManager, _validator, _calculator);

                // EventCoordinator需要DataManager和Calculator
                _eventCoordinator.SetDependencies(_dataManager, _calculator);

                _logger.LogDebug("组件依赖关系建立完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立组件依赖关系失败");
                throw;
            }
        }

        /// <summary>
        /// 订阅组件事件
        /// </summary>
        private void SubscribeToComponentEvents()
        {
            try
            {
                // 订阅命令处理器事件
                _commandHandler.OnPrescriptionSaved += OnPrescriptionSaved;
                _commandHandler.OnPrescriptionCleared += OnPrescriptionCleared;
                _commandHandler.OnPriceRecalculated += OnPriceRecalculated;

                // 订阅数据变更事件
                _dataManager.PrescriptionItems.CollectionChanged += (s, e) =>
                {
                    RecalculatePrice();
                    RaisePropertyChanged(nameof(PrescriptionItems));
                };

                _logger.LogDebug("组件事件订阅完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅组件事件失败");
                throw;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化处方数据
        /// </summary>
        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("开始初始化处方数据，医疗案例ID: {MedicalCaseId}", medicalCaseId);

                await _dataManager.InitializeAsync(medicalCaseId);
                
                // 初始计算
                RecalculatePrice();
                
                // 刷新UI绑定
                RefreshAllProperties();

                _logger.LogInformation("处方数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化处方数据失败");
                throw;
            }
        }

        /// <summary>
        /// 验证处方数据
        /// </summary>
        public PrescriptionValidator.ValidationResult ValidatePrescription()
        {
            try
            {
                return _validator.ValidatePrescription(
                    PrescriptionItems,
                    PrescriptionNo,
                    DosageCount,
                    Usage,
                    Discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方失败");
                return new PrescriptionValidator.ValidationResult { IsValid = false };
            }
        }

        /// <summary>
        /// 重新计算价格
        /// </summary>
        public void RecalculatePrice()
        {
            try
            {
                _currentCalculation = _calculator.CalculatePrescriptionPrice(
                    PrescriptionItems, DosageCount, Discount);

                // 刷新价格相关的UI绑定
                RaisePropertyChanged(nameof(SingleDosagePrice));
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(DiscountedPrice));
                RaisePropertyChanged(nameof(DiscountText));

                _logger.LogDebug("价格重新计算完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新计算价格失败");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处方保存完成事件
        /// </summary>
        private void OnPrescriptionSaved()
        {
            RefreshAllProperties();
            _eventCoordinator.PublishPrescriptionSaved();
        }

        /// <summary>
        /// 处方清空完成事件
        /// </summary>
        private void OnPrescriptionCleared()
        {
            RecalculatePrice();
            RefreshAllProperties();
            _eventCoordinator.PublishPrescriptionCleared();
        }

        /// <summary>
        /// 价格重新计算完成事件
        /// </summary>
        private void OnPriceRecalculated()
        {
            RecalculatePrice();
            _eventCoordinator.PublishPriceRecalculated();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 刷新所有属性通知
        /// </summary>
        private void RefreshAllProperties()
        {
            RaisePropertyChanged(nameof(MedicalCaseId));
            RaisePropertyChanged(nameof(PrescriptionNo));
            RaisePropertyChanged(nameof(DosageCount));
            RaisePropertyChanged(nameof(Usage));
            RaisePropertyChanged(nameof(MedicalAdvice));
            RaisePropertyChanged(nameof(Remark));
            RaisePropertyChanged(nameof(Discount));
            RaisePropertyChanged(nameof(IsLoading));
            RaisePropertyChanged(nameof(HasChanges));
            RaisePropertyChanged(nameof(SelectedItem));
            
            // 价格相关
            RaisePropertyChanged(nameof(SingleDosagePrice));
            RaisePropertyChanged(nameof(TotalPrice));
            RaisePropertyChanged(nameof(DiscountedPrice));
            RaisePropertyChanged(nameof(DiscountText));
        }

        #endregion

        #region IDisposable实现

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // 取消事件订阅
                        _commandHandler.OnPrescriptionSaved -= OnPrescriptionSaved;
                        _commandHandler.OnPrescriptionCleared -= OnPrescriptionCleared;
                        _commandHandler.OnPriceRecalculated -= OnPriceRecalculated;

                        // 清理事件协调器
                        _eventCoordinator.Unsubscribe();

                        _logger.LogDebug("PrescriptionViewModel资源清理完成");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理PrescriptionViewModel资源失败");
                    }
                }

                _disposed = true;
            }
        }

        #endregion
    }
}