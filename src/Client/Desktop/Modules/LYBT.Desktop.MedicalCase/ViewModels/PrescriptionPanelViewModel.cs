using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// Epic #2210 Phase 4: 处方面板ViewModel
    /// 用于MedicalCaseWorkspaceView的右侧60%区域
    /// 实现ISaveable接口
    /// 重构: 复用HerbCardControl和PrescriptionItemViewModel模式（参考经验方编辑）
    /// </summary>
    public class PrescriptionPanelViewModel : UnifiedViewModelBase, ISaveable
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IHerbRepository _herbRepository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoggerFactory _loggerFactory;
        private Guid _medicalCaseId;
        private Guid? _prescriptionId;
        private Guid _patientId;
        private string _patientName = string.Empty;

        /// <summary>
        /// 加载数据标志 - 用于在LoadFromDto期间跳过EnsureMinimumBlankRows
        /// Bug Fix: 防止PropertyChanged事件在数据加载完成前触发空槽位添加
        /// </summary>
        private bool _isLoadingData;

        /// <summary>
        /// 所有药材列表（用于注入到每个PrescriptionItemViewModel）
        /// </summary>
        private ObservableCollection<HerbDto> _allHerbs = new();

        #endregion

        #region 处方属性

        private string _treatmentMethod = string.Empty;
        /// <summary>
        /// 治法方案
        /// </summary>
        public string TreatmentMethod
        {
            get => _treatmentMethod;
            set => SetProperty(ref _treatmentMethod, value);
        }

        private string _treatmentPrinciple = string.Empty;
        /// <summary>
        /// 治疗原则
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        private int _dosageCount = 7;
        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    CalculatePrices();
                }
            }
        }

        private string _usage = "水煎服，一日一剂，分早晚两次温服";
        /// <summary>
        /// 用法
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private decimal _singleDosagePrice;
        /// <summary>
        /// 单剂价格
        /// </summary>
        public decimal SingleDosagePrice
        {
            get => _singleDosagePrice;
            set => SetProperty(ref _singleDosagePrice, value);
        }

        private decimal _totalPrice;
        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        private int _itemCount;
        /// <summary>
        /// 药材总数
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            set => SetProperty(ref _itemCount, value);
        }

        #endregion

        #region 药材列表

        /// <summary>
        /// 药材项列表（使用PrescriptionHerbItemViewModel，继承自HerbItemViewModelBase）
        /// 重构: 使用扁平列表替代行模型，与共享HerbCardControl配合
        /// Issue: unify-herb-card-control - 统一处方和经验方的药材编辑体验
        /// </summary>
        public ObservableCollection<PrescriptionHerbItemViewModel> HerbItems { get; } = new();

        #endregion

        #region 警告属性

        private bool _isDuplicateHerbsWarningVisible = false;
        /// <summary>
        /// 重复药材警告是否可见 (Issue #2248: 使用bool+Converter替代Visibility)
        /// </summary>
        public bool IsDuplicateHerbsWarningVisible
        {
            get => _isDuplicateHerbsWarningVisible;
            set => SetProperty(ref _isDuplicateHerbsWarningVisible, value);
        }

        private string _duplicateHerbsWarningText = string.Empty;
        /// <summary>
        /// 重复药材警告文本
        /// </summary>
        public string DuplicateHerbsWarningText
        {
            get => _duplicateHerbsWarningText;
            set => SetProperty(ref _duplicateHerbsWarningText, value);
        }

        #endregion

        #region 验方导入属性 (Issue #2246)

        private string _formulaSearchText = string.Empty;
        /// <summary>
        /// 验方搜索文本
        /// </summary>
        public string FormulaSearchText
        {
            get => _formulaSearchText;
            set
            {
                if (SetProperty(ref _formulaSearchText, value))
                {
                    _ = FilterFormulasAsync();
                }
            }
        }

        private ObservableCollection<FormulaDto> _filteredFormulas = new();
        /// <summary>
        /// 过滤后的验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> FilteredFormulas
        {
            get => _filteredFormulas;
            set => SetProperty(ref _filteredFormulas, value);
        }

        private FormulaDto? _selectedFormula;
        /// <summary>
        /// 选中的验方
        /// </summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    UpdateFormulaPreview();
                    ImportFormulaCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _formulaPreviewText = string.Empty;
        /// <summary>
        /// 验方预览文本
        /// </summary>
        public string FormulaPreviewText
        {
            get => _formulaPreviewText;
            set => SetProperty(ref _formulaPreviewText, value);
        }

        #endregion

        #region 历史复制属性 (Issue #2246)

        private ObservableCollection<MedicalCaseDto> _prescriptionHistory = new();
        /// <summary>
        /// 患者历史医案列表
        /// </summary>
        public ObservableCollection<MedicalCaseDto> PrescriptionHistory
        {
            get => _prescriptionHistory;
            set => SetProperty(ref _prescriptionHistory, value);
        }

        private MedicalCaseDto? _selectedHistoryCase;
        /// <summary>
        /// 选中的历史医案
        /// </summary>
        public MedicalCaseDto? SelectedHistoryCase
        {
            get => _selectedHistoryCase;
            set
            {
                if (SetProperty(ref _selectedHistoryCase, value))
                {
                    _ = UpdateHistoryPreviewAsync();
                    CopyHistoryCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _historyPreviewText = string.Empty;
        /// <summary>
        /// 历史处方预览文本
        /// </summary>
        public string HistoryPreviewText
        {
            get => _historyPreviewText;
            set => SetProperty(ref _historyPreviewText, value);
        }

        #endregion

        #region Tab选择属性 (Issue #2246)

        private int _selectedTabIndex = 0;
        /// <summary>
        /// 选中的Tab索引（0=手工录入, 1=验方导入, 2=历史复制）
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnTabChanged(value);
                }
            }
        }

        #endregion

        #region 命令

        public DelegateCommand AddRowCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand DeletePrescriptionCommand { get; }

        /// <summary>
        /// 删除药材命令（HerbCardControl绑定）
        /// Issue: unify-herb-card-control - 使用HerbItemViewModelBase作为参数类型
        /// </summary>
        public DelegateCommand<PrescriptionHerbItemViewModel> DeleteHerbCommand { get; }

        /// <summary>
        /// 剂量完成命令（HerbCardControl绑定，用于重复检测）
        /// Issue: unify-herb-card-control - 使用HerbItemViewModelBase作为参数类型
        /// </summary>
        public DelegateCommand<PrescriptionHerbItemViewModel> DosageCompletedCommand { get; }

        /// <summary>
        /// 添加新行命令（HerbCardControl绑定，到达末尾时触发）
        /// </summary>
        public DelegateCommand AddNewRowCommand { get; }

        /// <summary>
        /// 打开验方导入弹窗命令 (Issue #2246 - 弹窗模式)
        /// </summary>
        public DelegateCommand OpenFormulaImportDialogCommand { get; }

        /// <summary>
        /// 打开历史复制弹窗命令 (Issue #2246 - 弹窗模式)
        /// </summary>
        public DelegateCommand OpenHistoryCopyDialogCommand { get; }

        /// <summary>
        /// 导入验方命令 (Issue #2246 - 保留用于Tab模式兼容)
        /// </summary>
        public DelegateCommand ImportFormulaCommand { get; }

        /// <summary>
        /// 复制历史处方命令 (Issue #2246 - 保留用于Tab模式兼容)
        /// </summary>
        public DelegateCommand CopyHistoryCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionPanelViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            IHerbRepository herbRepository,
            IFormulaRepository formulaRepository,
            IDialogService dialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator;
            _loggerFactory = loggerFactory;

            // 基础命令
            AddRowCommand = new DelegateCommand(ExecuteAddRow);
            SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
            DeletePrescriptionCommand = new DelegateCommand(ExecuteDeletePrescription);

            // HerbCardControl绑定的命令 (Issue: unify-herb-card-control)
            DeleteHerbCommand = new DelegateCommand<PrescriptionHerbItemViewModel>(ExecuteDeleteHerb);
            DosageCompletedCommand = new DelegateCommand<PrescriptionHerbItemViewModel>(ExecuteDosageCompleted);
            AddNewRowCommand = new DelegateCommand(ExecuteAddNewRow);

            // Issue #2246: 弹窗模式命令
            OpenFormulaImportDialogCommand = new DelegateCommand(ExecuteOpenFormulaImportDialog);
            OpenHistoryCopyDialogCommand = new DelegateCommand(ExecuteOpenHistoryCopyDialog);

            // Issue #2246: 验方导入和历史复制命令（保留用于Tab模式兼容）
            ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula, CanImportFormula);
            CopyHistoryCommand = new DelegateCommand(ExecuteCopyHistory, CanCopyHistory);

            // 初始化默认药材项（12个空槽位，对应3行4列）
            AddDefaultHerbItems();

            Logger.LogInformation("PrescriptionPanelViewModel已初始化");
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化面板（由父ViewModel调用）
        /// Issue #2246: 添加patientId和patientName参数用于历史复制弹窗
        /// </summary>
        public async Task InitializeAsync(Guid medicalCaseId, Guid patientId, string patientName = "", PrescriptionDto? existingPrescription = null)
        {
            _medicalCaseId = medicalCaseId;
            _patientId = patientId;
            _patientName = patientName;

            // Bug Fix: ViewModel可能被复用，每次初始化时先重置HerbItems
            // 避免上一次会话的数据残留
            HerbItems.Clear();
            _prescriptionId = null;

            // 加载药材列表
            await LoadHerbsAsync();

            if (existingPrescription != null)
            {
                // 有已保存的处方，加载数据
                LoadFromDto(existingPrescription);
            }
            else
            {
                // 新处方，添加默认空槽位
                AddDefaultHerbItems();
                UpdateItemCount();
            }

            Logger.LogInformation("PrescriptionPanel初始化完成，MedicalCaseId: {MedicalCaseId}, PatientId: {PatientId}, HasPrescription: {HasPrescription}",
                medicalCaseId, patientId, existingPrescription != null);
        }

        /// <summary>
        /// 加载药材列表（注入到每个PrescriptionItemViewModel）
        /// 后端限制每页最多100条，使用循环分页获取所有药材
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                _allHerbs.Clear();
                const int pageSize = 100; // 后端限制最大100
                int page = 1;
                int totalLoaded = 0;

                while (true)
                {
                    var result = await _herbRepository.GetPagedAsync(page: page, pageSize: pageSize);
                    if (result.Items == null || !result.Items.Any())
                        break;

                    foreach (var herb in result.Items)
                    {
                        _allHerbs.Add(herb);
                    }
                    totalLoaded += result.Items.Count;

                    // 如果返回的数量少于请求的数量，说明已经是最后一页
                    if (result.Items.Count < pageSize || totalLoaded >= result.TotalCount)
                        break;

                    page++;
                }

                // 注入到所有现有的药材项
                foreach (var item in HerbItems)
                {
                    item.AllHerbs = _allHerbs;
                }

                Logger.LogInformation("加载药材列表完成，共{Count}种", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表失败");
            }
        }

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        private void LoadFromDto(PrescriptionDto dto)
        {
            // Bug Fix: 设置加载标志，防止PropertyChanged事件在加载过程中触发EnsureMinimumBlankRows
            _isLoadingData = true;

            try
            {
                _prescriptionId = dto.Id;
                TreatmentMethod = dto.Indication ?? string.Empty;
                TreatmentPrinciple = dto.Advice ?? string.Empty;
                DosageCount = dto.DosageCount;
                Usage = dto.Usage ?? string.Empty;
                SingleDosagePrice = dto.SingleDosePrice;
                TotalPrice = dto.TotalPrice;

                // 加载药材项
                // Issue: unify-herb-card-control - 使用SetLoadedUnitPrice替代直接赋值
                HerbItems.Clear();
                if (dto.Items != null && dto.Items.Any())
                {
                    foreach (var item in dto.Items)
                    {
                        var herbItem = CreateHerbItem();
                        herbItem.HerbId = item.HerbId;
                        herbItem.HerbName = item.HerbName ?? string.Empty;
                        herbItem.Dosage = item.Dosage;
                        herbItem.SetLoadedUnitPrice(item.UnitPrice); // 使用方法设置价格
                        HerbItems.Add(herbItem);
                    }
                }

                // N+1行原则：确保至少有4个空槽位（药材已添加，此时添加的空槽位会在药材后面）
                EnsureMinimumBlankRows();

                UpdateItemCount();
                CalculatePrices();
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        /// <summary>
        /// 创建新的药材项ViewModel
        /// Issue: unify-herb-card-control - 使用PrescriptionHerbItemViewModel继承自HerbItemViewModelBase
        /// </summary>
        private PrescriptionHerbItemViewModel CreateHerbItem()
        {
            var item = new PrescriptionHerbItemViewModel();
            item.AllHerbs = _allHerbs;

            // 订阅属性变化以触发价格计算和N+1行原则
            // Issue: unify-herb-card-control - ItemAmount改为ItemTotal
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PrescriptionHerbItemViewModel.ItemTotal) ||
                    e.PropertyName == nameof(PrescriptionHerbItemViewModel.HerbId))
                {
                    // Bug Fix: 加载过程中跳过这些操作，避免在数据加载完成前触发副作用
                    if (_isLoadingData) return;

                    CalculatePrices();
                    UpdateItemCount();
                    CheckDuplicateHerbs();

                    // N+1行原则：药材选择后确保至少有4个空槽位
                    if (e.PropertyName == nameof(PrescriptionHerbItemViewModel.HerbId))
                    {
                        EnsureMinimumBlankRows();
                    }
                }
            };

            return item;
        }

        /// <summary>
        /// 添加默认药材项（初始化时调用）
        /// N+1行原则：确保至少有4个空槽位（一整行）
        /// </summary>
        private void AddDefaultHerbItems()
        {
            // 初始化8个空槽位（2行4列）
            for (int i = 0; i < 8; i++)
            {
                HerbItems.Add(CreateHerbItem());
            }
        }

        /// <summary>
        /// 确保至少有4个空槽位（N+1行原则）
        /// Issue: unify-herb-card-control - 与经验方编辑器保持一致的逻辑
        /// 逻辑：
        /// - 0~4种药材 → 8个槽位（2行）
        /// - 5~8种药材 → 12个槽位（3行）
        /// - 9~12种药材 → 16个槽位（4行）
        /// - 以此类推...
        /// </summary>
        private void EnsureMinimumBlankRows()
        {
            const int minBlankSlots = 4; // 至少保留4个空槽位（一整行）

            // 统计空槽位数量（未选择药材的槽位）
            var blankSlots = HerbItems.Count(h => h.HerbId == Guid.Empty);

            // 如果空槽位不足4个，补充到4个
            while (blankSlots < minBlankSlots)
            {
                HerbItems.Add(CreateHerbItem());
                blankSlots++;
            }
        }

        /// <summary>
        /// 紧凑药材列表：将所有非空药材移到前面，空槽位移到后面
        /// Bug Fix: 解决用户在中间槽位输入药材后，空槽位显示在药材前面的问题
        /// </summary>
        private void CompactHerbItems()
        {
            // 提取所有非空药材项（保持相对顺序）
            var nonEmptyItems = HerbItems.Where(h => h.HerbId != Guid.Empty).ToList();

            // 清空集合
            HerbItems.Clear();

            // 先添加非空药材
            foreach (var item in nonEmptyItems)
            {
                HerbItems.Add(item);
            }

            // 再添加空槽位
            EnsureMinimumBlankRows();

            // 更新计数
            UpdateItemCount();
        }

        #endregion

        #region ISaveable

        public async Task<bool> SaveAsync()
        {
            try
            {
                // 收集药材项
                var items = CollectPrescriptionItems();

                if (!items.Any())
                {
                    Logger.LogWarning("没有药材项，跳过保存");
                    return true; // 空处方也算保存成功
                }

                PrescriptionDto? result;
                if (_prescriptionId.HasValue)
                {
                    // 更新现有处方
                    var updateRequest = new PrescriptionUpdateDto
                    {
                        DosageCount = DosageCount,
                        Usage = Usage,
                        Items = items
                    };
                    result = await _medicalCaseRepository.UpdatePrescriptionAsync(_medicalCaseId, updateRequest);
                }
                else
                {
                    // 创建新处方
                    // Bug Fix: 添加必填字段PatientId和DoctorId，否则服务端验证会返回400
                    var createRequest = new PrescriptionCreateDto
                    {
                        PatientId = _patientId,
                        DoctorId = SessionManager?.CurrentUserId ?? Guid.Empty,
                        DosageCount = DosageCount,
                        Usage = Usage,
                        Items = items
                    };
                    result = await _medicalCaseRepository.CreatePrescriptionAsync(_medicalCaseId, createRequest);
                    if (result != null)
                    {
                        _prescriptionId = result.Id;
                    }
                }

                if (result != null)
                {
                    Logger.LogInformation("处方数据保存成功");

                    // Bug Fix: 紧凑药材列表，将空槽位移到后面
                    CompactHerbItems();

                    // 发布处方完成事件
                    EventAggregator.GetEvent<PrescriptionCompletedEvent>()
                        .Publish(new PrescriptionCompletedPayload
                        {
                            PrescriptionId = _prescriptionId ?? Guid.Empty,
                            TotalItems = items.Count,
                            TotalAmount = TotalPrice
                        });

                    return true;
                }

                Logger.LogWarning("处方数据保存失败");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方数据异常");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 静默保存（不显示错误对话框，不发布事件）
        /// OpenSpec: clarify-cancel-consultation-logic - 取消前保存使用
        /// </summary>
        public async Task<bool> SaveSilentlyAsync()
        {
            try
            {
                // Bug诊断：记录调用信息
                Logger.LogInformation("[处方诊断] SaveSilentlyAsync被调用，MedicalCaseId: {MedicalCaseId}, HerbItems.Count: {Count}",
                    _medicalCaseId, HerbItems.Count);

                // 收集药材项
                var items = CollectPrescriptionItems();

                Logger.LogInformation("[处方诊断] CollectPrescriptionItems返回: {Count}个有效药材项", items.Count);

                if (!items.Any())
                {
                    Logger.LogWarning("[处方诊断] 没有有效药材项，静默保存跳过。HerbItems详情:");
                    foreach (var h in HerbItems.Take(5))
                    {
                        Logger.LogWarning("[处方诊断]   - HerbId={HerbId}, HerbName={HerbName}, Dosage={Dosage}",
                            h.HerbId, h.HerbName ?? "(null)", h.Dosage);
                    }
                    return true; // 空处方也算保存成功
                }

                PrescriptionDto? result;
                if (_prescriptionId.HasValue)
                {
                    // 更新现有处方
                    var updateRequest = new PrescriptionUpdateDto
                    {
                        DosageCount = DosageCount,
                        Usage = Usage,
                        Items = items
                    };
                    result = await _medicalCaseRepository.UpdatePrescriptionAsync(_medicalCaseId, updateRequest);
                }
                else
                {
                    // 创建新处方
                    // Bug Fix: 添加必填字段PatientId和DoctorId，否则服务端验证会返回400
                    var createRequest = new PrescriptionCreateDto
                    {
                        PatientId = _patientId,
                        DoctorId = SessionManager?.CurrentUserId ?? Guid.Empty,
                        DosageCount = DosageCount,
                        Usage = Usage,
                        Items = items
                    };
                    Logger.LogInformation("[处方诊断] 准备调用CreatePrescriptionAsync，MedicalCaseId: {MedicalCaseId}, PatientId: {PatientId}, Items: {Count}",
                        _medicalCaseId, _patientId, items.Count);
                    result = await _medicalCaseRepository.CreatePrescriptionAsync(_medicalCaseId, createRequest);
                    Logger.LogInformation("[处方诊断] CreatePrescriptionAsync返回: {Result}", result != null ? $"成功,Id={result.Id}" : "null");
                    if (result != null)
                    {
                        _prescriptionId = result.Id;
                    }
                }

                if (result != null)
                {
                    Logger.LogInformation("[处方诊断] 处方数据静默保存成功，PrescriptionId: {PrescriptionId}", result.Id);

                    // Bug Fix: 紧凑药材列表，将空槽位移到后面
                    CompactHerbItems();

                    // 不发布PrescriptionCompletedEvent，因为这是取消流程的保存
                    return true;
                }

                Logger.LogWarning("[处方诊断] 处方数据静默保存失败：API返回null");
                return false;
            }
            catch (Exception ex)
            {
                // 静默保存不显示错误，只记录日志
                Logger.LogWarning(ex, "静默保存处方数据异常（不阻止后续操作）");
                return false;
            }
        }

        /// <summary>
        /// 收集处方药材项（从HerbItems扁平列表中收集有效项）
        /// </summary>
        private List<PrescriptionItemInputDto> CollectPrescriptionItems()
        {
            var items = new List<PrescriptionItemInputDto>();

            foreach (var herbItem in HerbItems)
            {
                if (herbItem.HerbId != Guid.Empty && herbItem.Dosage > 0)
                {
                    items.Add(new PrescriptionItemInputDto
                    {
                        HerbId = herbItem.HerbId,
                        HerbName = herbItem.HerbName,
                        Quantity = herbItem.Dosage,
                        Dosage = herbItem.Dosage,
                        Unit = "g"
                    });
                }
            }

            return items;
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 添加行（N+1原则：强制添加一整行4个空槽位）
        /// </summary>
        private void ExecuteAddRow()
        {
            // 强制添加4个空槽位（用户主动添加行）
            for (int i = 0; i < 4; i++)
            {
                HerbItems.Add(CreateHerbItem());
            }
            Logger.LogInformation("手动添加新行，当前共{Count}个槽位", HerbItems.Count);
        }

        /// <summary>
        /// 添加新槽位（由HerbCardControl到达末尾时触发）
        /// N+1行原则：确保至少有4个空槽位
        /// </summary>
        private void ExecuteAddNewRow()
        {
            // N+1行原则：确保至少有4个空槽位
            EnsureMinimumBlankRows();
            Logger.LogInformation("添加新槽位，当前共{Count}个", HerbItems.Count);
        }

        /// <summary>
        /// 删除药材项（带自动前移）
        /// Issue: unify-herb-card-control - 与经验方编辑器保持一致的逻辑
        /// </summary>
        private void ExecuteDeleteHerb(PrescriptionHerbItemViewModel? item)
        {
            if (item == null) return;

            // 删除指定药材项（与经验方编辑器一致）
            HerbItems.Remove(item);

            // N+1行原则：确保至少有4个空槽位
            EnsureMinimumBlankRows();

            Logger.LogInformation("删除药材: {HerbName}", item.HerbName);
            UpdateItemCount();
            CalculatePrices();
            CheckDuplicateHerbs();
        }

        /// <summary>
        /// 剂量完成命令（触发重复检测）
        /// Issue: unify-herb-card-control - 使用PrescriptionHerbItemViewModel
        /// </summary>
        private void ExecuteDosageCompleted(PrescriptionHerbItemViewModel? item)
        {
            if (item == null) return;

            // 触发重复检测和价格计算
            CheckDuplicateHerbs();
            CalculatePrices();
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private async void ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存...");

                var success = await SaveAsync();

                if (success)
                {
                    await ShowSuccessMessageAsync("处方草稿已保存");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存草稿失败");
                await ShowErrorMessageAsync($"保存失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        private async void ExecuteDeletePrescription()
        {
            try
            {
                if (!_prescriptionId.HasValue)
                {
                    await ShowErrorMessageAsync("当前没有处方可删除");
                    return;
                }

                var confirmed = await ShowConfirmationAsync(
                    "确定要删除当前处方吗？此操作不可恢复！",
                    "删除处方");

                if (!confirmed)
                {
                    return;
                }

                SetIsBusy(true, "正在删除...");

                await _medicalCaseRepository.DeletePrescriptionAsync(_medicalCaseId);

                _prescriptionId = null;
                HerbItems.Clear();
                AddDefaultHerbItems();
                TreatmentMethod = string.Empty;
                TreatmentPrinciple = string.Empty;
                SingleDosagePrice = 0;
                TotalPrice = 0;
                ItemCount = 0;

                await ShowSuccessMessageAsync("处方已删除");
                Logger.LogInformation("处方删除成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除处方失败");
                await ShowErrorMessageAsync($"删除失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新药材总数
        /// </summary>
        private void UpdateItemCount()
        {
            ItemCount = HerbItems.Count(h => h.HerbId != Guid.Empty);
        }

        /// <summary>
        /// 计算价格（单剂价格 = 所有药材小计之和，总价格 = 单剂价格 × 剂数）
        /// 这是处方特有的功能，经验方不需要
        /// Issue: unify-herb-card-control - ItemAmount改为ItemTotal
        /// </summary>
        private void CalculatePrices()
        {
            SingleDosagePrice = HerbItems
                .Where(h => h.HerbId != Guid.Empty)
                .Sum(h => h.ItemTotal);

            TotalPrice = SingleDosagePrice * DosageCount;
        }

        /// <summary>
        /// 检查重复药材
        /// </summary>
        private void CheckDuplicateHerbs()
        {
            var herbIds = new List<Guid>();
            var duplicates = new List<string>();

            foreach (var item in HerbItems)
            {
                if (item.HerbId != Guid.Empty)
                {
                    if (herbIds.Contains(item.HerbId))
                    {
                        duplicates.Add(item.HerbName);
                    }
                    else
                    {
                        herbIds.Add(item.HerbId);
                    }
                }
            }

            if (duplicates.Any())
            {
                DuplicateHerbsWarningText = $"发现重复药材：{string.Join("、", duplicates.Distinct())}";
                IsDuplicateHerbsWarningVisible = true;
            }
            else
            {
                IsDuplicateHerbsWarningVisible = false;
            }
        }

        #endregion

        #region 验方导入方法 (Issue #2246)

        /// <summary>
        /// Tab切换事件处理
        /// </summary>
        private async void OnTabChanged(int tabIndex)
        {
            switch (tabIndex)
            {
                case 1: // 验方导入
                    await LoadFormulasAsync();
                    break;
                case 2: // 历史复制
                    await LoadPrescriptionHistoryAsync();
                    break;
            }
        }

        /// <summary>
        /// 加载验方列表
        /// </summary>
        private async Task LoadFormulasAsync()
        {
            try
            {
                var result = await _formulaRepository.GetPagedAsync(page: 1, pageSize: 100);
                FilteredFormulas.Clear();
                foreach (var formula in result.Items)
                {
                    FilteredFormulas.Add(formula);
                }
                Logger.LogInformation("加载验方列表完成，共{Count}个", FilteredFormulas.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方列表失败");
            }
        }

        /// <summary>
        /// 过滤验方列表
        /// </summary>
        private async Task FilterFormulasAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FormulaSearchText))
                {
                    await LoadFormulasAsync();
                    return;
                }

                var results = await _formulaRepository.SearchAsync(FormulaSearchText);
                FilteredFormulas.Clear();
                foreach (var formula in results)
                {
                    FilteredFormulas.Add(formula);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "过滤验方列表失败");
            }
        }

        /// <summary>
        /// 更新验方预览
        /// </summary>
        private void UpdateFormulaPreview()
        {
            if (SelectedFormula == null)
            {
                FormulaPreviewText = string.Empty;
                return;
            }

            FormulaPreviewText = $"药材组成: {SelectedFormula.HerbNames}";
        }

        /// <summary>
        /// 是否可以导入验方
        /// </summary>
        private bool CanImportFormula()
        {
            return SelectedFormula != null && !IsBusy;
        }

        /// <summary>
        /// 执行导入验方
        /// </summary>
        private async void ExecuteImportFormula()
        {
            if (SelectedFormula == null) return;

            try
            {
                SetIsBusy(true, "正在导入验方...");

                // 使用已有的API：ImportFormulaIntoPrescriptionAsync
                var result = await _medicalCaseRepository.ImportFormulaIntoPrescriptionAsync(
                    _medicalCaseId, SelectedFormula.Id);

                if (result != null)
                {
                    // 重新加载处方数据
                    LoadFromDto(result);
                    await ShowSuccessMessageAsync($"已导入验方「{SelectedFormula.Name}」，共{result.Items?.Count ?? 0}味药材");
                    Logger.LogInformation("验方导入成功: {FormulaName}", SelectedFormula.Name);

                    // 切换回手工录入Tab
                    SelectedTabIndex = 0;
                }
                else
                {
                    await ShowErrorMessageAsync("导入验方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入验方异常");
                await ShowErrorMessageAsync($"导入失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 历史复制方法 (Issue #2246)

        /// <summary>
        /// 加载患者历史处方列表
        /// </summary>
        private async Task LoadPrescriptionHistoryAsync()
        {
            try
            {
                if (_patientId == Guid.Empty)
                {
                    Logger.LogWarning("PatientId为空，无法加载历史处方");
                    return;
                }

                var cases = await _medicalCaseRepository.GetByPatientIdAsync(_patientId);
                PrescriptionHistory.Clear();

                // 过滤掉当前医案，只显示其他历史医案
                foreach (var caseItem in cases.Where(c => c.Id != _medicalCaseId).OrderByDescending(c => c.ConsultationDate))
                {
                    PrescriptionHistory.Add(caseItem);
                }

                Logger.LogInformation("加载历史处方完成，共{Count}条", PrescriptionHistory.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载历史处方失败");
            }
        }

        /// <summary>
        /// 更新历史处方预览
        /// </summary>
        private async Task UpdateHistoryPreviewAsync()
        {
            if (SelectedHistoryCase == null)
            {
                HistoryPreviewText = string.Empty;
                return;
            }

            try
            {
                // 获取历史医案详情（包含处方）
                var detail = await _medicalCaseRepository.GetByIdWithDetailsAsync(SelectedHistoryCase.Id);
                if (detail?.Prescription?.Items != null && detail.Prescription.Items.Any())
                {
                    var herbNames = string.Join("、", detail.Prescription.Items.Select(i => $"{i.HerbName}({i.Dosage}g)"));
                    HistoryPreviewText = $"药材组成: {herbNames}";
                }
                else
                {
                    HistoryPreviewText = "该医案无处方记录";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取历史处方详情失败");
                HistoryPreviewText = "加载失败";
            }
        }

        /// <summary>
        /// 是否可以复制历史处方
        /// </summary>
        private bool CanCopyHistory()
        {
            return SelectedHistoryCase != null && !IsBusy;
        }

        /// <summary>
        /// 执行复制历史处方
        /// </summary>
        private async void ExecuteCopyHistory()
        {
            if (SelectedHistoryCase == null) return;

            try
            {
                SetIsBusy(true, "正在复制处方...");

                // 获取历史医案详情
                var detail = await _medicalCaseRepository.GetByIdWithDetailsAsync(SelectedHistoryCase.Id);
                if (detail?.Prescription?.Items == null || !detail.Prescription.Items.Any())
                {
                    await ShowErrorMessageAsync("该医案无处方记录");
                    return;
                }

                // 检查重复药材
                var existingHerbIds = HerbItems.Where(h => h.HerbId != Guid.Empty).Select(h => h.HerbId).ToHashSet();
                var duplicates = detail.Prescription.Items
                    .Where(i => existingHerbIds.Contains(i.HerbId))
                    .Select(i => i.HerbName)
                    .ToList();

                if (duplicates.Any())
                {
                    DuplicateHerbsWarningText = $"发现重复药材：{string.Join("、", duplicates)}";
                    IsDuplicateHerbsWarningVisible = true;
                }

                // 添加药材到当前处方（追加模式）
                // Issue: unify-herb-card-control - 使用SetLoadedUnitPrice
                int addedCount = 0;
                foreach (var item in detail.Prescription.Items)
                {
                    // 跳过已存在的药材
                    if (existingHerbIds.Contains(item.HerbId))
                    {
                        continue;
                    }

                    // 找一个空槽位或添加新槽位
                    var emptySlot = HerbItems.FirstOrDefault(h => h.HerbId == Guid.Empty);
                    if (emptySlot == null)
                    {
                        emptySlot = CreateHerbItem();
                        HerbItems.Add(emptySlot);
                    }

                    emptySlot.HerbId = item.HerbId;
                    emptySlot.HerbName = item.HerbName ?? string.Empty;
                    emptySlot.Dosage = item.Dosage;
                    emptySlot.SetLoadedUnitPrice(item.UnitPrice);
                    addedCount++;
                }

                // N+1行原则：确保至少有4个空槽位
                EnsureMinimumBlankRows();

                // 重新计算价格
                UpdateItemCount();
                CalculatePrices();

                await ShowSuccessMessageAsync($"已复制 {addedCount} 味药材");
                Logger.LogInformation("历史处方复制成功，复制{Count}味药材", addedCount);

                // 切换回手工录入Tab
                SelectedTabIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制历史处方异常");
                await ShowErrorMessageAsync($"复制失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 弹窗方法 (Issue #2246)

        /// <summary>
        /// 打开验方导入弹窗
        /// </summary>
        private void ExecuteOpenFormulaImportDialog()
        {
            _dialogService.ShowDialog(
                nameof(Dialogs.FormulaImportDialog),
                null,
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        await HandleFormulaImportResultAsync(result.Parameters);
                    }
                });
        }

        /// <summary>
        /// 处理验方导入弹窗结果
        /// </summary>
        private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
        {
            try
            {
                SetIsBusy(true, "正在导入验方...");

                if (!parameters.TryGetValue<FormulaDto>("SelectedFormula", out var formula) || formula == null)
                {
                    return;
                }

                if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs == null || !herbs.Any())
                {
                    await ShowErrorMessageAsync("验方无药材信息");
                    return;
                }

                // 检查重复药材（过滤掉HerbId为null的药材）
                var existingHerbIds = HerbItems.Where(h => h.HerbId != Guid.Empty).Select(h => h.HerbId).ToHashSet();
                var duplicates = herbs
                    .Where(h => h.HerbId.HasValue && existingHerbIds.Contains(h.HerbId.Value))
                    .Select(h => h.HerbName)
                    .ToList();

                if (duplicates.Any())
                {
                    DuplicateHerbsWarningText = $"发现重复药材：{string.Join("、", duplicates)}";
                    IsDuplicateHerbsWarningVisible = true;
                }

                // 添加药材到当前处方（追加模式）
                int addedCount = 0;
                foreach (var herb in herbs)
                {
                    // 跳过没有HerbId的药材
                    if (!herb.HerbId.HasValue)
                    {
                        continue;
                    }

                    // 跳过已存在的药材
                    if (existingHerbIds.Contains(herb.HerbId.Value))
                    {
                        continue;
                    }

                    // 找一个空槽位或添加新槽位
                    var emptySlot = HerbItems.FirstOrDefault(h => h.HerbId == Guid.Empty);
                    if (emptySlot == null)
                    {
                        emptySlot = CreateHerbItem();
                        HerbItems.Add(emptySlot);
                    }

                    emptySlot.HerbId = herb.HerbId.Value;
                    emptySlot.HerbName = herb.HerbName ?? string.Empty;
                    emptySlot.Dosage = herb.Quantity;
                    // 尝试从药材列表获取单价 (Issue: unify-herb-card-control)
                    var herbInfo = _allHerbs.FirstOrDefault(h => h.Id == herb.HerbId.Value);
                    if (herbInfo != null)
                    {
                        emptySlot.SetLoadedUnitPrice(herbInfo.Price);
                    }
                    addedCount++;
                }

                // N+1行原则：确保至少有4个空槽位
                EnsureMinimumBlankRows();

                // 重新计算价格
                UpdateItemCount();
                CalculatePrices();

                await ShowSuccessMessageAsync($"已导入验方「{formula.Name}」，添加 {addedCount} 味药材");
                Logger.LogInformation("验方导入成功: {FormulaName}, 添加{Count}味药材", formula.Name, addedCount);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理验方导入结果异常");
                await ShowErrorMessageAsync($"导入失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 打开历史复制弹窗
        /// </summary>
        private void ExecuteOpenHistoryCopyDialog()
        {
            var parameters = new DialogParameters
            {
                { "PatientId", _patientId },
                { "PatientName", _patientName }
            };

            _dialogService.ShowDialog(
                nameof(Dialogs.HistoryCopyDialog),
                parameters,
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        await HandleHistoryCopyResultAsync(result.Parameters);
                    }
                });
        }

        /// <summary>
        /// 处理历史复制弹窗结果
        /// </summary>
        private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
        {
            try
            {
                SetIsBusy(true, "正在复制处方...");

                if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items == null || !items.Any())
                {
                    await ShowErrorMessageAsync("历史处方无药材记录");
                    return;
                }

                // 检查重复药材
                var existingHerbIds = HerbItems.Where(h => h.HerbId != Guid.Empty).Select(h => h.HerbId).ToHashSet();
                var duplicates = items
                    .Where(i => existingHerbIds.Contains(i.HerbId))
                    .Select(i => i.HerbName)
                    .ToList();

                if (duplicates.Any())
                {
                    DuplicateHerbsWarningText = $"发现重复药材：{string.Join("、", duplicates)}";
                    IsDuplicateHerbsWarningVisible = true;
                }

                // 添加药材到当前处方（追加模式）
                // Issue: unify-herb-card-control - 使用SetLoadedUnitPrice
                int addedCount = 0;
                foreach (var item in items)
                {
                    // 跳过已存在的药材
                    if (existingHerbIds.Contains(item.HerbId))
                    {
                        continue;
                    }

                    // 找一个空槽位或添加新槽位
                    var emptySlot = HerbItems.FirstOrDefault(h => h.HerbId == Guid.Empty);
                    if (emptySlot == null)
                    {
                        emptySlot = CreateHerbItem();
                        HerbItems.Add(emptySlot);
                    }

                    emptySlot.HerbId = item.HerbId;
                    emptySlot.HerbName = item.HerbName ?? string.Empty;
                    emptySlot.Dosage = item.Dosage;
                    emptySlot.SetLoadedUnitPrice(item.UnitPrice);
                    addedCount++;
                }

                // N+1行原则：确保至少有4个空槽位
                EnsureMinimumBlankRows();

                // 重新计算价格
                UpdateItemCount();
                CalculatePrices();

                await ShowSuccessMessageAsync($"已复制 {addedCount} 味药材");
                Logger.LogInformation("历史处方复制成功，复制{Count}味药材", addedCount);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理历史复制结果异常");
                await ShowErrorMessageAsync($"复制失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
