using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Desktop.Prescriptions.Models;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方编写器视图模型 - UltraThink精简架构
    /// 核心处方编写界面，整合所有处方组件
    /// </summary>
    public class PrescriptionComposerViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IHerbRepository _herbRepository;

        #endregion

        #region 组件依赖

        private readonly PrescriptionDataManager _dataManager;
        private readonly PrescriptionCalculator _calculator;
        private readonly PrescriptionValidator _validator;
        private readonly PrescriptionCommandHandler _commandHandler;
        private readonly PrescriptionEventCoordinator _eventCoordinator;

        #endregion

        #region 数据属性

        private Guid _medicalCaseId;
        private MedicalCaseDto? _currentMedicalCase;
        private string _patientInfo = string.Empty;
        private string _doctorInfo = string.Empty;

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        /// <summary>
        /// 当前医疗案例
        /// </summary>
        public MedicalCaseDto? CurrentMedicalCase
        {
            get => _currentMedicalCase;
            set
            {
                if (SetProperty(ref _currentMedicalCase, value))
                {
                    UpdatePatientInfo();
                }
            }
        }

        /// <summary>
        /// 患者信息
        /// </summary>
        public string PatientInfo
        {
            get => _patientInfo;
            set => SetProperty(ref _patientInfo, value);
        }

        /// <summary>
        /// 医生信息
        /// </summary>
        public string DoctorInfo
        {
            get => _doctorInfo;
            set => SetProperty(ref _doctorInfo, value);
        }

        #endregion

        #region 处方数据绑定

        /// <summary>
        /// 处方项集合
        /// </summary>
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems => _dataManager.PrescriptionItems;

        private ObservableCollection<PrescriptionItemRow> _itemRows = new();

        /// <summary>
        /// 处方项行集合（用于8列表格布局）
        /// Issue #1360: [ENTRY-2] 实现Items→ItemRows转换逻辑
        /// </summary>
        public ObservableCollection<PrescriptionItemRow> ItemRows
        {
            get => _itemRows;
            set => SetProperty(ref _itemRows, value);
        }

        /// <summary>
        /// 选中的处方项
        /// </summary>
        public PrescriptionItemViewModel? SelectedItem
        {
            get => _dataManager.SelectedItem;
            set
            {
                if (_dataManager.SelectedItem != value)
                {
                    _dataManager.SelectedItem = value;
                    RaisePropertyChanged();
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 处方编号
        /// </summary>
        public string PrescriptionNo
        {
            get => _dataManager.PrescriptionNo;
            set
            {
                if (_dataManager.PrescriptionNo != value)
                {
                    _dataManager.PrescriptionNo = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                }
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
                if (_dataManager.DosageCount != value)
                {
                    _dataManager.DosageCount = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                    RecalculatePrice();
                }
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
                if (_dataManager.Usage != value)
                {
                    _dataManager.Usage = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                }
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
                if (_dataManager.MedicalAdvice != value)
                {
                    _dataManager.MedicalAdvice = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                }
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
                if (_dataManager.Remark != value)
                {
                    _dataManager.Remark = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                }
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
                if (_dataManager.Discount != value)
                {
                    _dataManager.Discount = value;
                    _dataManager.MarkAsChanged();
                    RaisePropertyChanged();
                    RecalculatePrice();
                }
            }
        }

        #endregion

        #region 药材过滤数据 (Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤)

        private List<HerbDto> _allHerbs = new();
        private ObservableCollection<HerbDto> _filteredHerbs = new();

        /// <summary>
        /// 所有药材列表（用于过滤）
        /// </summary>
        public List<HerbDto> AllHerbs
        {
            get => _allHerbs;
            set => SetProperty(ref _allHerbs, value);
        }

        /// <summary>
        /// 过滤后的药材列表（绑定到ComboBox）
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs
        {
            get => _filteredHerbs;
            set => SetProperty(ref _filteredHerbs, value);
        }

        #endregion

        #region 计算属性

        private PrescriptionCalculator.CalculationResult? _calculationResult;

        /// <summary>
        /// 计算结果
        /// </summary>
        public PrescriptionCalculator.CalculationResult? CalculationResult
        {
            get => _calculationResult;
            set => SetProperty(ref _calculationResult, value);
        }

        /// <summary>
        /// 单剂价格
        /// </summary>
        public decimal SingleDosagePrice => CalculationResult?.SingleDosagePrice ?? 0m;

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice => CalculationResult?.TotalPrice ?? 0m;

        /// <summary>
        /// 优惠后价格
        /// </summary>
        public decimal DiscountedPrice => CalculationResult?.DiscountedPrice ?? 0m;

        /// <summary>
        /// 节省金额
        /// </summary>
        public decimal TotalSaved => CalculationResult?.TotalSaved ?? 0m;

        /// <summary>
        /// 实际总价（等同于优惠后价格）
        /// </summary>
        public decimal ActualTotal => DiscountedPrice;

        /// <summary>
        /// 优惠金额
        /// </summary>
        public decimal DiscountAmount => TotalPrice - DiscountedPrice;

        /// <summary>
        /// 项目数量
        /// </summary>
        public int ItemCount => PrescriptionItems?.Count ?? 0;

        #endregion

        #region 命令

        /// <summary>
        /// 保存处方命令
        /// </summary>
        public DelegateCommand SaveCommand => _commandHandler.SaveCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 清空处方命令
        /// </summary>
        public DelegateCommand ClearCommand => _commandHandler.ClearCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 添加药材命令
        /// </summary>
        public DelegateCommand AddHerbCommand => _commandHandler.AddHerbCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 移除药材命令
        /// </summary>
        public DelegateCommand<PrescriptionItemViewModel> RemoveHerbCommand =>
            _commandHandler.RemoveHerbCommand as DelegateCommand<PrescriptionItemViewModel> ??
            new DelegateCommand<PrescriptionItemViewModel>(_ => { });

        /// <summary>
        /// 导入验方命令
        /// </summary>
        public DelegateCommand ImportFormulaCommand => _commandHandler.ImportFormulaCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 生成处方编号命令
        /// </summary>
        public DelegateCommand GeneratePrescriptionNoCommand => _commandHandler.GeneratePrescriptionNoCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 验证处方命令
        /// </summary>
        public DelegateCommand ValidateCommand => _commandHandler.ValidateCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 重新计算命令
        /// </summary>
        public DelegateCommand RecalculateCommand => _commandHandler.RecalculateCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 打印预览命令
        /// </summary>
        public DelegateCommand PrintPreviewCommand => _commandHandler.PrintPreviewCommand as DelegateCommand ?? new DelegateCommand(() => { });

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand BackCommand { get; }

        /// <summary>
        /// 清空所有命令（别名）
        /// </summary>
        public DelegateCommand ClearAllCommand { get; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public DelegateCommand CloseCommand { get; }

        /// <summary>
        /// 保存草稿命令
        /// </summary>
        public DelegateCommand SaveDraftCommand { get; }

        /// <summary>
        /// 保存处方命令（别名）
        /// </summary>
        public DelegateCommand SavePrescriptionCommand { get; }

        /// <summary>
        /// 编辑药材命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<PrescriptionItemViewModel> EditHerbCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionComposerViewModel(
            IPrescriptionRepository prescriptionRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IHerbRepository herbRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            PrescriptionDataManager dataManager,
            PrescriptionCalculator calculator,
            PrescriptionValidator validator,
            PrescriptionCommandHandler commandHandler,
            PrescriptionEventCoordinator eventCoordinator,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _eventCoordinator = eventCoordinator ?? throw new ArgumentNullException(nameof(eventCoordinator));

            // 设置命令处理器的依赖
            _commandHandler.SetDependencies(_dataManager, _validator, _calculator);

            // 初始化自有命令
            BackCommand = new DelegateCommand(Back);

            // 初始化别名和新增命令
            ClearAllCommand = ClearCommand; // 别名
            CloseCommand = BackCommand; // 别名
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft, CanSaveDraft);
            SavePrescriptionCommand = SaveCommand; // 别名
            EditHerbCommand = new DelegateCommand<PrescriptionItemViewModel>(ExecuteEditHerb, item => item != null && !IsBusy);

            // 订阅事件
            SubscribeToEvents();

            // 设置当前医生信息
            UpdateDoctorInfo();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面导航时调用
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            try
            {
                // 获取参数
                if (parameters.ContainsKey("MedicalCaseId"))
                {
                    MedicalCaseId = parameters.GetValue<Guid>("MedicalCaseId");
                }

                if (MedicalCaseId != Guid.Empty)
                {
                    await LoadPrescriptionDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("未提供有效的医疗案例ID");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化处方编写器时发生异常");
                await ShowErrorMessageAsync("初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 加载处方数据
        /// </summary>
        private async Task LoadPrescriptionDataAsync()
        {
            try
            {
                SetIsBusy(true, "正在初始化处方数据...");

                // 加载医疗案例信息
                await LoadMedicalCaseAsync();

                // 加载药材数据 (Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤)
                await LoadAllHerbsAsync();

                // 初始化处方数据管理器
                await _dataManager.InitializeAsync(MedicalCaseId);

                // 初始计算
                RecalculatePrice();

                // 初始化ItemRows（Issue #1360）
                RefreshItemRows();

                Logger.LogInformation("处方编写器初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化处方编写器失败");
                throw;
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 加载医疗案例信息
        /// </summary>
        private async Task LoadMedicalCaseAsync()
        {
            try
            {
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(MedicalCaseId);
                CurrentMedicalCase = medicalCase;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载医疗案例失败，ID: {MedicalCaseId}", MedicalCaseId);
            }
        }

        /// <summary>
        /// 加载所有药材数据
        /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
        /// </summary>
        private async Task LoadAllHerbsAsync()
        {
            try
            {
                // 使用SearchAsync获取所有药材（传入空字符串）
                var herbs = await _herbRepository.SearchAsync(string.Empty);
                AllHerbs = herbs ?? new List<HerbDto>();
                Logger.LogInformation($"已加载 {AllHerbs.Count} 个药材");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材数据失败");
                AllHerbs = new List<HerbDto>();
            }
        }

        /// <summary>
        /// 根据输入文本过滤药材
        /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
        /// </summary>
        /// <param name="searchText">搜索文本（药材名称或拼音码）</param>
        public void FilterHerbs(string searchText)
        {
            try
            {
                FilteredHerbs.Clear();

                // 如果搜索文本为空，不显示任何结果
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return;
                }

                // 过滤逻辑：匹配药材名称或拼音码（不区分大小写）
                var filtered = AllHerbs
                    .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                               (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Take(5) // 限制最多5个结果
                    .ToList();

                // 添加到过滤结果集合
                foreach (var herb in filtered)
                {
                    FilteredHerbs.Add(herb);
                }

                Logger.LogDebug($"过滤药材：输入='{searchText}'，结果数={filtered.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "过滤药材时发生异常");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 订阅事件
        /// </summary>
        protected override void SubscribeToEvents()
        {
            // 订阅价格重算事件
            _commandHandler.OnPriceRecalculated += OnPriceRecalculated;

            // 订阅保存成功事件
            _commandHandler.OnPrescriptionSaved += OnPrescriptionSaved;

            // 订阅清空事件
            _commandHandler.OnPrescriptionCleared += OnPrescriptionCleared;

            // 订阅处方项集合变化事件（Issue #1360）
            PrescriptionItems.CollectionChanged += (s, e) => RefreshItemRows();
        }

        private void OnPriceRecalculated()
        {
            RecalculatePrice();
        }

        private void OnPrescriptionSaved()
        {
            // 处方保存成功后的操作
            Logger.LogInformation("处方保存成功");
        }

        private void OnPrescriptionCleared()
        {
            // 处方清空后的操作
            RecalculatePrice();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 返回
        /// </summary>
        private void Back()
        {
            NavigateTo("MainRegion", "PrescriptionManagementView");
        }

        /// <summary>
        /// 重新计算价格
        /// </summary>
        private void RecalculatePrice()
        {
            try
            {
                CalculationResult = _calculator.CalculatePrescriptionPrice(
                    PrescriptionItems,
                    DosageCount,
                    Discount);

                // 通知价格相关属性变更
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(ActualTotal));
                RaisePropertyChanged(nameof(DiscountAmount));
                RaisePropertyChanged(nameof(ItemCount));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重新计算价格时发生异常");
            }
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private void ExecuteSaveDraft()
        {
            try
            {
                Logger.LogInformation("保存处方草稿");
                ShowInfoMessage("保存草稿功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿时发生异常");
                ShowErrorMessage("保存草稿失败");
            }
        }

        /// <summary>
        /// 检查是否可以保存草稿
        /// </summary>
        private bool CanSaveDraft()
        {
            return !IsBusy && PrescriptionItems.Count > 0;
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        private void ExecuteEditHerb(PrescriptionItemViewModel item)
        {
            if (item == null) return;

            try
            {
                Logger.LogInformation("编辑药材: {HerbName}", item.HerbName);
                ShowInfoMessage("编辑药材功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "编辑药材时发生异常");
                ShowErrorMessage("编辑药材失败");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新患者信息
        /// </summary>
        private void UpdatePatientInfo()
        {
            if (CurrentMedicalCase != null)
            {
                PatientInfo = $"患者: {CurrentMedicalCase.PatientName} | 性别: {CurrentMedicalCase.PatientGender} | 年龄: {CurrentMedicalCase.PatientAge}";
            }
            else
            {
                PatientInfo = "患者信息未加载";
            }
        }

        /// <summary>
        /// 更新医生信息
        /// </summary>
        private void UpdateDoctorInfo()
        {
            if (SessionManager?.CurrentUser != null)
            {
                DoctorInfo = $"医生: {SessionManager.CurrentUser.RealName} | 科室: {SessionManager.CurrentUser.Role}";
            }
            else
            {
                DoctorInfo = "医生信息未获取";
            }
        }

        /// <summary>
        /// 更新命令状态
        /// </summary>
        private void UpdateCommandStates()
        {
            // 命令状态由各自的CanExecute方法控制
            // 这里可以添加额外的状态更新逻辑
        }

        /// <summary>
        /// 刷新处方项行集合（Items → ItemRows转换）
        /// Issue #1360: [ENTRY-2] 实现Items→ItemRows转换逻辑
        /// </summary>
        private void RefreshItemRows()
        {
            ItemRows.Clear();

            var items = PrescriptionItems;
            if (items == null || items.Count == 0)
            {
                return;
            }

            // 每4个项目组成一行
            for (int i = 0; i < items.Count; i += 4)
            {
                var row = new PrescriptionItemRow
                {
                    Item1 = i < items.Count ? items[i] : null,
                    Item2 = i + 1 < items.Count ? items[i + 1] : null,
                    Item3 = i + 2 < items.Count ? items[i + 2] : null,
                    Item4 = i + 3 < items.Count ? items[i + 3] : null
                };
                ItemRows.Add(row);
            }

            Logger.LogDebug($"已刷新处方项行集合，共 {items.Count} 个项目，{ItemRows.Count} 行");
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 清理资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消事件订阅
                if (_commandHandler != null)
                {
                    _commandHandler.OnPriceRecalculated -= OnPriceRecalculated;
                    _commandHandler.OnPrescriptionSaved -= OnPrescriptionSaved;
                    _commandHandler.OnPrescriptionCleared -= OnPrescriptionCleared;
                }

                // 清理事件协调器
                _eventCoordinator?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
