using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Infrastructure.Controls.HerbList;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.MedicalCase.ViewModels.Events;
using LYBT.Desktop.Prescriptions.Models.Items;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 处方面板ViewModel
/// OpenSpec: refactor-oversized-viewmodels - 重构后 < 500行
/// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 移除ISaveable，使用IDataProvider
/// </summary>
public class PrescriptionPanelViewModel : UnifiedViewModelBase, IDataProvider
{
    #region 字段

    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IHerbRepository _herbRepository;
    private readonly IDialogService _dialogService;
    private readonly IEventAggregator _eventAggregator;
    private readonly PrescriptionCalculator _calculator;
    private readonly PrescriptionValidator _validator;
    private readonly PrescriptionSaveHandler _saveHandler;
    private readonly PrescriptionImportHandler _importHandler;
    private readonly PrescriptionDataLoader _dataLoader;
    private Guid _medicalCaseId;
    private Guid? _prescriptionId;
    private Guid _patientId;
    private string _patientName = string.Empty;
    private bool _isLoadingData;
    private bool _isInitialized;
    private ObservableCollection<HerbListDto> _allHerbs = new();
    private DuplicateDosageStrategy _duplicateStrategy = DuplicateDosageStrategy.Max;
    private IReadOnlyList<HerbItemDto>? _currentHerbList;

    /// <summary>
    /// 待添加的药材（用于导入场景，View处理后自动清空）
    /// OpenSpec: simplify-workspace-event-architecture
    /// </summary>
    private IReadOnlyList<HerbItemDto>? _pendingAddHerbs;

    #endregion

    #region 处方属性

    private string _treatmentMethod = string.Empty;
    /// <summary>
    /// 主治/适应症 (映射到 Indication)
    /// </summary>
    public string TreatmentMethod { get => _treatmentMethod; set => SetProperty(ref _treatmentMethod, value); }

    private string _treatmentPrinciple = string.Empty;
    /// <summary>
    /// 医嘱/用药建议 (映射到 Advice)
    /// </summary>
    public string TreatmentPrinciple { get => _treatmentPrinciple; set => SetProperty(ref _treatmentPrinciple, value); }

    private string _referencedFormulas = string.Empty;
    /// <summary>
    /// 引用的验方名称列表，逗号分隔
    /// OpenSpec: refactor-medicalcase-aggregate-crud
    /// </summary>
    public string ReferencedFormulas { get => _referencedFormulas; set => SetProperty(ref _referencedFormulas, value); }

    private string _remark = string.Empty;
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

    private int _dosageCount = 7;
    public int DosageCount
    {
        get => _dosageCount;
        set { if (SetProperty(ref _dosageCount, value)) CalculatePricesFromDto(); }
    }

    private string _usage = "水煎服，一日一剂，分早晚两次温服";
    public string Usage { get => _usage; set => SetProperty(ref _usage, value); }

    private decimal _singleDosagePrice;
    public decimal SingleDosagePrice { get => _singleDosagePrice; set => SetProperty(ref _singleDosagePrice, value); }

    private decimal _totalPrice;
    public decimal TotalPrice { get => _totalPrice; set => SetProperty(ref _totalPrice, value); }

    private int _itemCount;
    public int ItemCount { get => _itemCount; set => SetProperty(ref _itemCount, value); }

    #endregion

    #region 药材和警告属性

    /// <summary>
    /// 药材库数据 - 供HerbListControl绑定
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    /// <summary>
    /// 重复剂量取值策略 - 供HerbListControl绑定
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public DuplicateDosageStrategy DuplicateStrategy
    {
        get => _duplicateStrategy;
        set => SetProperty(ref _duplicateStrategy, value);
    }

    /// <summary>
    /// 药材列表（TwoWay绑定到HerbListControl.HerbItems）
    /// OpenSpec: simplify-workspace-event-architecture
    /// </summary>
    private IList<HerbItemDto>? _herbItemsToLoad;
    public IList<HerbItemDto>? HerbItemsToLoad
    {
        get => _herbItemsToLoad;
        set
        {
            if (SetProperty(ref _herbItemsToLoad, value))
            {
                // 同步到_currentHerbList供保存使用
                _currentHerbList = value?.ToList().AsReadOnly();
                // OpenSpec: simplify-workspace-event-architecture - 使用新方法替代已删除的过期方法
                CalculatePricesFromDto();
                UpdateDuplicateWarning();
            }
        }
    }

    /// <summary>
    /// 待添加的药材（导入场景：View处理后自动清空）
    /// OpenSpec: simplify-workspace-event-architecture
    /// </summary>
    public IReadOnlyList<HerbItemDto>? PendingAddHerbs
    {
        get => _pendingAddHerbs;
        set => SetProperty(ref _pendingAddHerbs, value);
    }

    /// <summary>
    /// 清空待添加药材（View处理后调用）
    /// </summary>
    public void ClearPendingAddHerbs()
    {
        PendingAddHerbs = null;
    }

    private bool _isDuplicateHerbsWarningVisible;
    public bool IsDuplicateHerbsWarningVisible { get => _isDuplicateHerbsWarningVisible; set => SetProperty(ref _isDuplicateHerbsWarningVisible, value); }

    private string _duplicateHerbsWarningText = string.Empty;
    public string DuplicateHerbsWarningText { get => _duplicateHerbsWarningText; set => SetProperty(ref _duplicateHerbsWarningText, value); }

    #endregion

    #region 控件状态

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; private set => SetProperty(ref _hasUnsavedChanges, value); }

    private bool _isReadOnly;
    public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }

    #endregion

    #region 命令

    public DelegateCommand AddRowCommand { get; }
    public DelegateCommand SaveDraftCommand { get; }
    public DelegateCommand DeletePrescriptionCommand { get; }
    public DelegateCommand<PrescriptionHerbItem> DeleteHerbCommand { get; }
    public DelegateCommand<PrescriptionHerbItem> DosageCompletedCommand { get; }
    public DelegateCommand AddNewRowCommand { get; }
    public DelegateCommand OpenFormulaImportDialogCommand { get; }
    public DelegateCommand OpenHistoryCopyDialogCommand { get; }
    public DelegateCommand ClearHerbItemsCommand { get; }

    #endregion

    #region 构造函数

    public PrescriptionPanelViewModel(
        IMedicalCaseRepository medicalCaseRepository, IHerbRepository herbRepository,
        IDialogService dialogService, IEventAggregator eventAggregator, ILoggerFactory loggerFactory,
        IRegionManager regionManager, PrescriptionCalculator calculator,
        PrescriptionValidator validator,
        PrescriptionSaveHandler saveHandler, PrescriptionImportHandler importHandler,
        PrescriptionDataLoader dataLoader, ISessionManager? sessionManager = null,
        ICommonDialogService? commonDialogService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, null, commonDialogService)
    {
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventAggregator = eventAggregator;
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _saveHandler = saveHandler ?? throw new ArgumentNullException(nameof(saveHandler));
        _importHandler = importHandler ?? throw new ArgumentNullException(nameof(importHandler));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

        // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 移除旧Handler依赖
        AddRowCommand = new DelegateCommand(() => { /* 由控件内部处理 */ });
        SaveDraftCommand = new DelegateCommand(ExecuteSaveDraft);
        DeletePrescriptionCommand = new DelegateCommand(ExecuteDeletePrescription);
        DeleteHerbCommand = new DelegateCommand<PrescriptionHerbItem>(ExecuteDeleteHerb);
        DosageCompletedCommand = new DelegateCommand<PrescriptionHerbItem>(ExecuteDosageCompleted);
        AddNewRowCommand = new DelegateCommand(() => { /* 由控件内部处理 */ });
        OpenFormulaImportDialogCommand = new DelegateCommand(ExecuteOpenFormulaImportDialog);
        OpenHistoryCopyDialogCommand = new DelegateCommand(ExecuteOpenHistoryCopyDialog);
        ClearHerbItemsCommand = new DelegateCommand(ExecuteClearHerbItems);

        EventAggregator.GetEvent<SaveAllRequestedEvent>().Subscribe(OnSaveAllRequested, ThreadOption.UIThread);
    }

    #endregion

    #region 初始化

    public async Task InitializeAsync(Guid medicalCaseId, Guid patientId, string patientName = "", PrescriptionDetailDto? existingPrescription = null)
    {
        _medicalCaseId = medicalCaseId;
        _patientId = patientId;
        _patientName = patientName;
        _prescriptionId = null;

        await _dataLoader.LoadAllHerbsAsync(_allHerbs);

        // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 通过事件请求View加载数据
        if (existingPrescription != null)
        {
            LoadFromDto(existingPrescription);
        }
        else
        {
            // 初始状态：空列表，控件会自动添加空槽位
            HerbItemsToLoad = null;
        }

        _isInitialized = true;
        HasUnsavedChanges = false;
    }

    private void LoadFromDto(PrescriptionDetailDto dto)
    {
        _isLoadingData = true;
        try
        {
            _prescriptionId = dto.Id;
            TreatmentMethod = string.Empty; // Indication已删除，打印时从Consultation.TcmDiagnosis获取
            TreatmentPrinciple = dto.Advice ?? string.Empty;
            ReferencedFormulas = dto.ReferencedFormulas ?? string.Empty;
            Remark = dto.Remark ?? string.Empty;
            DosageCount = dto.DosageCount;
            Usage = dto.Usage ?? string.Empty;
            SingleDosagePrice = dto.SingleDosePrice;
            TotalPrice = dto.TotalPrice;

            // OpenSpec: simplify-workspace-event-architecture - 通过属性绑定加载药材
            if (dto.Items?.Any() == true)
            {
                var herbItems = dto.Items.Select(item => new HerbItemDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName ?? string.Empty,
                    Dosage = item.Dosage,
                    DecocteMethod = item.DecocteMethod,
                    UnitPrice = item.UnitPrice
                }).ToList();

                HerbItemsToLoad = herbItems;
            }
            else
            {
                HerbItemsToLoad = null;
            }
        }
        finally { _isLoadingData = false; }
    }

    /// <summary>
    /// 旧版药材项变更回调 - 已弃用
    /// OpenSpec: simplify-workspace-event-architecture - 使用HerbItemsToLoad属性TwoWay绑定替代
    /// </summary>
    [Obsolete("使用HerbItemsToLoad属性的TwoWay绑定替代。")]
    private void OnHerbItemChanged(PrescriptionHerbItem item, string propertyName)
    {
        // 已废弃：使用HerbItemsToLoad属性的TwoWay绑定
    }

    #endregion

    #region 内部保存（供命令使用）

    /// <summary>
    /// 保存处方数据到服务器
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.3) - 内部方法，供命令使用
    /// 注意：主要的保存流程已迁移到聚合保存模式，此方法仅供内部命令使用
    /// </summary>
    private async Task<bool> SaveAsync()
    {
        try
        {
            var context = CreateSaveContext();
            var result = await _saveHandler.SaveAsync(context);
            if (result.IsEmpty) return true;
            if (result.IsSuccess)
            {
                _prescriptionId = result.PrescriptionId;
                // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 控件内部自动处理紧凑
                HasUnsavedChanges = false;
                EventAggregator.GetEvent<PrescriptionSavedEvent>().Publish(new PrescriptionSavedPayload
                {
                    MedicalCaseId = _medicalCaseId,
                    PrescriptionId = result.PrescriptionId ?? Guid.Empty,
                    IsAutoSave = false
                });
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方数据异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
            return false;
        }
    }

    private PrescriptionSaveContext CreateSaveContext() => new()
    {
        MedicalCaseId = _medicalCaseId,
        PrescriptionId = _prescriptionId,
        PatientId = _patientId,
        UserId = SessionManager?.CurrentUserId ?? Guid.Empty,
        DosageCount = DosageCount,
        Usage = Usage,
        // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 从_currentHerbList获取
        Items = CollectPrescriptionItemsFromDto(),
        TotalPrice = TotalPrice
    };

    /// <summary>
    /// 从当前药材列表收集处方项(用于保存)
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
    /// </summary>
    private List<PrescriptionItemInputDto> CollectPrescriptionItemsFromDto()
    {
        if (_currentHerbList == null)
            return new List<PrescriptionItemInputDto>();

        return _currentHerbList
            .Where(h => h.IsValid)
            .Select(h => new PrescriptionItemInputDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Unit = h.Unit ?? "g",
                Dosage = h.Dosage,
                DecocteMethod = h.DecocteMethod,
                UnitPrice = h.UnitPrice,
                Subtotal = h.Dosage * h.UnitPrice
            })
            .ToList();
    }

    #endregion

    #region IDataProvider

    /// <summary>
    /// 获取诊断数据
    /// PrescriptionPanel不提供诊断数据，返回null
    /// </summary>
    /// <returns>null（诊断数据由ConsultationPanel提供）</returns>
    public ConsultationInputDto? GetConsultationData() => null;

    /// <summary>
    /// 获取处方数据
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.3)
    /// OpenSpec: herb-editor-control-refactoring - 使用HerbItemDto
    /// </summary>
    /// <returns>处方聚合输入DTO</returns>
    public PrescriptionInputDto? GetPrescriptionData()
    {
        // OpenSpec: herb-editor-control-refactoring - 从HerbItemDto转换
        var items = ConvertHerbItemsToInput();

        // 如果没有有效药材项，返回表示不需要处方的DTO
        if (items.Count == 0)
        {
            return new PrescriptionInputDto
            {
                NeedsPrescription = false,
                DosageCount = DosageCount,
                Usage = Usage,
                Advice = TreatmentPrinciple,
                ReferencedFormulas = ReferencedFormulas,
                Remark = Remark,
                Id = _prescriptionId
            };
        }

        return new PrescriptionInputDto
        {
            NeedsPrescription = true,
            DosageCount = DosageCount,
            Usage = Usage,
            Advice = TreatmentPrinciple,
            ReferencedFormulas = ReferencedFormulas,
            Remark = Remark,
            Items = items,
            Id = _prescriptionId
        };
    }

    /// <summary>
    /// 将HerbItemDto转换为PrescriptionItemInputDto列表
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    private List<PrescriptionItemInputDto> ConvertHerbItemsToInput()
    {
        if (_currentHerbList == null)
            return new List<PrescriptionItemInputDto>();

        return _currentHerbList
            .Where(h => h.IsValid)
            .Select(h => new PrescriptionItemInputDto
            {
                HerbId = h.HerbId,
                Dosage = h.Dosage,
                DecocteMethod = h.DecocteMethod,
                UnitPrice = h.UnitPrice
            })
            .ToList();
    }

    #endregion

    #region 命令实现

    /// <summary>
    /// 添加新行 - 已由控件内部处理
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
    /// </summary>
    private void ExecuteAddRow()
    {
        // 控件内部自动管理空槽位，无需外部触发
    }

    /// <summary>
    /// 删除药材 - 已由控件处理
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
    /// </summary>
    private void ExecuteDeleteHerb(PrescriptionHerbItem? item)
    {
        // 控件通过HerbListChanged事件通知，此处保留空实现以保持命令绑定兼容
    }

    private void ExecuteDosageCompleted(PrescriptionHerbItem? item)
    {
        if (item == null) return;
        // OpenSpec: simplify-workspace-event-architecture - 使用新方法替代已删除的过期方法
        UpdateDuplicateWarning();
        CalculatePricesFromDto();
    }

    private async void ExecuteSaveDraft()
    {
        try
        {
            SetIsBusy(true, "正在保存...");
            if (await SaveAsync()) await ShowSuccessMessageAsync("处方草稿已保存");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存草稿失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
        finally { SetIsBusy(false); }
    }

    private async void ExecuteDeletePrescription()
    {
        try
        {
            if (!_prescriptionId.HasValue) { await ShowErrorMessageAsync("当前没有处方可删除"); return; }
            if (!await ShowConfirmationAsync("确定要删除当前处方吗？此操作不可恢复！", "删除处方")) return;
            SetIsBusy(true, "正在删除...");

            // OpenSpec: simplify-medicalcase-api - 通过聚合保存设置NeedsPrescription=false触发删除
            var inputDto = new MedicalCaseInputDto
            {
                Id = _medicalCaseId,
                NeedsPrescription = false,
                Prescription = null
            };
            await _medicalCaseRepository.SaveAsync(_medicalCaseId, inputDto);

            ResetPrescription();
            await ShowSuccessMessageAsync("处方已删除");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除处方失败");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex));
        }
        finally { SetIsBusy(false); }
    }

    private void ResetPrescription()
    {
        _prescriptionId = null;
        // OpenSpec: simplify-workspace-event-architecture - 通过属性绑定清空控件
        HerbItemsToLoad = null;
        TreatmentMethod = TreatmentPrinciple = string.Empty;
        ReferencedFormulas = Remark = string.Empty;
        SingleDosagePrice = TotalPrice = 0;
        ItemCount = 0;
    }

    /// <summary>
    /// 清空当前处方药材
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5)
    /// </summary>
    private async void ExecuteClearHerbItems()
    {
        // 检查是否有有效药材
        var validItemCount = _currentHerbList?.Count(h => h.IsValid) ?? 0;
        if (validItemCount == 0)
        {
            await ShowSuccessMessageAsync("当前没有可清空的药材");
            return;
        }

        // 确认对话框
        if (!await ShowConfirmationAsync($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材"))
            return;

        // 通过属性绑定清空控件
        HerbItemsToLoad = null;
        NotifyDataChanged();

        Logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        await ShowSuccessMessageAsync($"已清空{validItemCount}项药材");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 处理HerbListControl的变更事件
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    [Obsolete("使用HerbItemsToLoad属性的TwoWay绑定替代。控件变更自动同步到属性。")]
    public void OnHerbListChanged(HerbListChangedEventArgs e)
    {
        if (_isLoadingData) return;

        // 更新当前药材列表缓存（从控件获取）
        // 注：实际生产中应通过View获取控件的HerbList
        // 此处使用ItemCount作为临时过渡
        ItemCount = e.ItemCount;

        // 计算价格
        CalculatePricesFromDto();

        // 检查重复（由控件内部处理，这里更新警告显示）
        UpdateDuplicateWarning();

        // 通知数据变更
        NotifyDataChanged();
    }

    /// <summary>
    /// 设置当前药材列表（供View调用）
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    [Obsolete("使用HerbItemsToLoad属性的TwoWay绑定替代。设置HerbItemsToLoad即可。")]
    public void SetCurrentHerbList(IReadOnlyList<HerbItemDto> herbList)
    {
        _currentHerbList = herbList;
        ItemCount = herbList.Count(h => !h.IsEmpty);
        CalculatePricesFromDto();
        UpdateDuplicateWarning();
    }

    /// <summary>
    /// 基于HerbItemDto计算价格
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    private void CalculatePricesFromDto()
    {
        if (_currentHerbList == null || !_currentHerbList.Any())
        {
            SingleDosagePrice = 0;
            TotalPrice = 0;
            return;
        }

        var validItems = _currentHerbList.Where(h => h.IsValid);
        var singlePrice = validItems.Sum(h => h.CalculatePrice());
        SingleDosagePrice = singlePrice;
        TotalPrice = singlePrice * DosageCount;
    }

    /// <summary>
    /// 更新重复药材警告
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    private void UpdateDuplicateWarning()
    {
        if (_currentHerbList == null)
        {
            IsDuplicateHerbsWarningVisible = false;
            DuplicateHerbsWarningText = string.Empty;
            return;
        }

        var validItems = _currentHerbList.Where(h => !h.IsEmpty).ToList();
        var duplicateGroups = validItems
            .GroupBy(h => h.HerbId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Any())
        {
            IsDuplicateHerbsWarningVisible = true;
            var names = duplicateGroups.Select(g => g.First().HerbName);
            DuplicateHerbsWarningText = string.Join("、", names);
        }
        else
        {
            IsDuplicateHerbsWarningVisible = false;
            DuplicateHerbsWarningText = string.Empty;
        }
    }

    // OpenSpec: simplify-workspace-event-architecture - 已删除过期方法
    // - UpdateItemCount() - HerbItems已移除，使用CalculatePricesFromDto替代
    // - CalculatePrices() - HerbItems已移除，使用CalculatePricesFromDto替代
    // - CheckDuplicateHerbs() - HerbItems已移除，使用UpdateDuplicateWarning替代

    #endregion

    #region 弹窗处理

    private void ExecuteOpenFormulaImportDialog() =>
        _dialogService.ShowDialog(nameof(Dialogs.FormulaImportDialog), null, async r =>
        { if (r.Result == ButtonResult.OK) await HandleFormulaImportResultAsync(r.Parameters); });

    private void ExecuteOpenHistoryCopyDialog() =>
        _dialogService.ShowDialog(nameof(Dialogs.HistoryCopyDialog),
            new DialogParameters { { "PatientId", _patientId }, { "PatientName", _patientName } },
            async r => { if (r.Result == ButtonResult.OK) await HandleHistoryCopyResultAsync(r.Parameters); });

    /// <summary>
    /// 处理验方导入结果
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 使用事件机制
    /// </summary>
    private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在导入验方...");
            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null) return;
            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            { await ShowErrorMessageAsync("验方无药材信息"); return; }

            // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 简化导入，直接转换为HerbItemDto
            var herbItems = _importHandler.ToHerbItemDtos(formula, herbs);
            if (!herbItems.Any())
            { await ShowErrorMessageAsync("验方无有效药材"); return; }

            // OpenSpec: simplify-workspace-event-architecture - 通过属性触发添加
            // View处理PendingAddHerbs，调用控件的AddHerbs方法
            PendingAddHerbs = herbItems;

            // 记录引用的验方名称
            if (!string.IsNullOrEmpty(formula.Name))
            {
                if (string.IsNullOrEmpty(ReferencedFormulas))
                    ReferencedFormulas = formula.Name;
                else if (!ReferencedFormulas.Contains(formula.Name))
                    ReferencedFormulas = $"{ReferencedFormulas}, {formula.Name}";
            }

            await ShowSuccessMessageAsync($"已导入验方「{formula.Name}」，共 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex));
        }
        finally { SetIsBusy(false); }
    }

    /// <summary>
    /// 处理历史处方复制结果
    /// OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 使用事件机制
    /// </summary>
    private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetIsBusy(true, "正在复制处方...");
            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            { await ShowErrorMessageAsync("历史处方无药材记录"); return; }

            // OpenSpec: slim-medicalcase-workspace-viewmodel (Phase 5) - 简化复制，直接转换为HerbItemDto
            var herbItems = _importHandler.ToHerbItemDtos(items);
            if (!herbItems.Any())
            { await ShowErrorMessageAsync("历史处方无有效药材"); return; }

            // OpenSpec: simplify-workspace-event-architecture - 通过属性触发添加
            // View处理PendingAddHerbs，调用控件的AddHerbs方法
            PendingAddHerbs = herbItems;

            await ShowSuccessMessageAsync($"已复制 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex));
        }
        finally { SetIsBusy(false); }
    }

    // OpenSpec: simplify-workspace-event-architecture - 已删除死代码
    // - ShowDuplicateHerbDialogsAsync: 从未被调用
    // - MergeDuplicateHerbs: HerbItems已移除，重复药材合并由HerbListControl内部处理

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理全局保存请求事件
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4) - 使用SaveAsync替代SaveSilentlyAsync
    /// 注意：此事件处理逻辑将在聚合保存模式下逐步废弃
    /// </summary>
    private async void OnSaveAllRequested(Guid medicalCaseId)
    {
        if (medicalCaseId == _medicalCaseId)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "SaveAllRequested保存失败，静默处理");
            }
        }
    }

    private void NotifyDataChanged()
    {
        if (!_isInitialized || HasUnsavedChanges) return;
        HasUnsavedChanges = true;
        EventAggregator.GetEvent<PrescriptionDataChangedEvent>().Publish(_medicalCaseId);
    }

    public void Cleanup() => EventAggregator.GetEvent<SaveAllRequestedEvent>().Unsubscribe(OnSaveAllRequested);

    #endregion
}
