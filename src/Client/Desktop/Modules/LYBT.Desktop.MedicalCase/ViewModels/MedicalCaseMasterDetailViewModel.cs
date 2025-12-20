using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.Items.MedicalCases;
using LYBT.Desktop.Models.Items.Prescriptions;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医案Master-Detail视图模型
/// OpenSpec: refactor-medicalcase-management
/// OpenSpec: unify-herb-list-controls - 支持处方药材编辑
/// OpenSpec: optimize-entity-data-flow - 使用MedicalCaseListDto优化列表加载
///
/// 设计决策：
/// - 工具栏无新建按钮（新建医案通过看诊入口创建）
/// - 可编辑字段：诊断信息（现病史、舌诊、脉诊、中医诊断）、处方药材、备注
/// - 与其他模块（Formula/Herbs/Patients）保持布局一致
/// </summary>
public class MedicalCaseMasterDetailViewModel : MasterDetailViewModelBase<MedicalCaseListDto, MedicalCaseDetailModel>
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IHerbRepository _herbRepository;

    public MedicalCaseMasterDetailViewModel(
        IMedicalCaseRepository repository,
        IHerbRepository herbRepository,
        ICommonDialogService commonDialogService,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));

        PageTitle = "医案管理";

        // OpenSpec: refactor-medicalcase-management
        // 禁用新建按钮 - 医案仅通过看诊入口创建
        // AddCommand将被隐藏，不在工具栏显示

        // OpenSpec: unify-herb-list-controls - 处方编辑命令
        DeleteHerbCommand = new DelegateCommand<PrescriptionHerbItem>(OnDeleteHerb);
        DosageCompletedCommand = new DelegateCommand<PrescriptionHerbItem>(OnDosageCompleted);
        AddNewRowCommand = new DelegateCommand(OnAddNewRow);
    }

    #region 属性

    /// <summary>
    /// 详情标题
    /// </summary>
    public string DetailTitle => CurrentDetail == null ? "医案详情" :
        IsEditMode ? $"编辑医案 - {CurrentDetail.PatientName}" :
        $"医案详情 - {CurrentDetail.PatientName}";

    /// <summary>
    /// 选中项的患者姓名 - 用于列表显示
    /// </summary>
    public string SelectedPatientName => SelectedItem?.PatientName ?? string.Empty;

    #endregion

    #region OpenSpec: unify-herb-list-controls - 处方编辑属性

    private ObservableCollection<HerbDetailDto> _allHerbs = new();
    private ObservableCollection<PrescriptionHerbItem> _herbItems = new();

    /// <summary>
    /// 所有药材列表 - 用于拼音自动补全
    /// </summary>
    public ObservableCollection<HerbDetailDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    /// <summary>
    /// 处方药材编辑列表 - 用于HerbListEditor
    /// </summary>
    public ObservableCollection<PrescriptionHerbItem> HerbItems
    {
        get => _herbItems;
        private set => SetProperty(ref _herbItems, value);
    }

    /// <summary>
    /// 药材数量
    /// </summary>
    public int HerbCount => HerbItems?.Count(x => x.HerbId != Guid.Empty) ?? 0;

    /// <summary>
    /// 删除药材命令
    /// </summary>
    public DelegateCommand<PrescriptionHerbItem> DeleteHerbCommand { get; }

    /// <summary>
    /// 剂量输入完成命令
    /// </summary>
    public DelegateCommand<PrescriptionHerbItem> DosageCompletedCommand { get; }

    /// <summary>
    /// 添加新行命令
    /// </summary>
    public DelegateCommand AddNewRowCommand { get; }

    #endregion

    #region 基类抽象方法实现

    protected override async Task<IEnumerable<MedicalCaseListDto>> GetItemsAsync(int page, int pageSize, string? searchText)
    {
        // OpenSpec: optimize-entity-data-flow - 使用轻量级ListDto，不再为每个列表项加载完整详情
        var result = await _repository.GetPagedListAsync(page, pageSize, searchText);

        TotalCount = result.TotalCount;
        CurrentPage = result.CurrentPage;
        PageSize = result.PageSize;

        return result.Items ?? Enumerable.Empty<MedicalCaseListDto>();
    }

    protected override async Task<MedicalCaseDetailModel?> LoadDetailAsync(MedicalCaseListDto item)
    {
        try
        {
            // OpenSpec: unify-herb-list-controls - 确保药材列表已加载
            if (AllHerbs.Count == 0)
            {
                await LoadHerbsAsync();
            }

            var dto = await _repository.GetByIdWithDetailsAsync(item.Id);
            var detail = MedicalCaseDetailModel.FromDto(dto);

            // OpenSpec: unify-herb-list-controls - 初始化处方编辑列表
            InitializeHerbItems(detail);

            return detail;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载医案详情失败: {MedicalCaseId}", item.Id);
            await ShowErrorMessageAsync("加载医案详情失败");
            return null;
        }
    }

    protected override MedicalCaseDetailModel CreateNewDetail()
    {
        // OpenSpec: refactor-medicalcase-management
        // 医案管理模块不支持新建，此方法不应被调用
        throw new NotSupportedException("医案管理模块不支持新建医案，请通过看诊入口创建");
    }

    protected override MedicalCaseDetailModel CloneDetail(MedicalCaseDetailModel detail)
    {
        return detail.Clone();
    }

    protected override object? GetDetailId(MedicalCaseDetailModel detail)
    {
        return detail.Id;
    }

    protected override async Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail)
    {
        try
        {
            // OpenSpec: unify-herb-list-controls + refactor-medicalcase-management
            // 使用SaveAsync一次性保存诊断和处方
            // Bug Fix: 添加PatientId和UserId，修复保存时HTTP 400验证错误
            var aggregateDto = new MedicalCaseInputDto
            {
                Id = detail.Id,
                PatientId = detail.PatientId,
                UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                Remark = detail.Remark,
                Consultation = detail.ToConsultationInputDto(),
                Prescription = new PrescriptionInputDto
                {
                    // Bug Fix: 过滤条件必须同时检查HerbId和Dosage，避免空行或未完成填写的行触发验证错误
                    NeedsPrescription = HerbItems.Any(x => x.HerbId != Guid.Empty && x.Dosage > 0),
                    DosageCount = detail.DoseCount ?? 7,
                    ReferencedFormulas = detail.ReferencedFormulas,
                    Items = HerbItems
                        .Where(x => x.HerbId != Guid.Empty && x.Dosage > 0)
                        .Select(x => new PrescriptionItemInputDto
                        {
                            HerbId = x.HerbId,
                            HerbName = x.HerbName,
                            Dosage = (int)x.Dosage,
                            Unit = x.Unit,
                            UnitPrice = x.UnitPrice,
                            Subtotal = x.ItemAmount,
                            DecocteMethod = x.DecocteMethod
                        })
                        .ToList()
                }
            };

            await _repository.SaveAsync(detail.Id, aggregateDto);

            // 更新DetailModel的处方数据
            detail.HerbCount = aggregateDto.Prescription.Items.Count;

            Logger.LogInformation("医案保存成功: {MedicalCaseId}, 药材数量: {HerbCount}",
                detail.Id, detail.HerbCount);
            await ShowSuccessMessageAsync("保存成功");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案失败: {MedicalCaseId}", detail.Id);
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
            return false;
        }
    }

    protected override async Task<bool> DeleteDetailAsync(MedicalCaseDetailModel detail)
    {
        try
        {
            var confirmed = await ShowConfirmationAsync(
                $"确定要删除患者 '{detail.PatientName}' 的医案吗？\n此操作不可恢复。",
                "删除确认");
            if (!confirmed) return false;

            var success = await _repository.DeleteAsync(detail.Id);
            if (success)
            {
                Logger.LogInformation("医案删除成功: {MedicalCaseId}", detail.Id);
                await ShowSuccessMessageAsync("删除成功");
                return true;
            }

            await ShowErrorMessageAsync("删除失败");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除医案失败: {MedicalCaseId}", detail.Id);
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除", ex));
            return false;
        }
    }

    #endregion

    #region 重写新建/批量删除命令

    /// <summary>
    /// 重写新建操作 - 医案管理模块不支持新建
    /// </summary>
    protected override Task OnExecuteAddAsync()
    {
        // OpenSpec: refactor-medicalcase-management
        // 医案仅通过看诊入口创建，管理模块不提供新建功能
        Logger.LogWarning("尝试在医案管理模块新建医案，此操作不被支持");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量删除医案
    /// </summary>
    protected override async Task OnExecuteBatchDeleteAsync(List<MedicalCaseListDto> items)
    {
        if (items == null || items.Count == 0) return;

        var successCount = 0;
        var failureCount = 0;
        var failedItems = new List<string>();

        foreach (var item in items)
        {
            try
            {
                if (await _repository.DeleteAsync(item.Id))
                    successCount++;
                else
                {
                    failureCount++;
                    failedItems.Add(item.PatientName ?? item.CaseNumber ?? "未知");
                }
            }
            catch
            {
                failureCount++;
                failedItems.Add(item.PatientName ?? item.CaseNumber ?? "未知");
            }
        }

        var message = $"批量删除完成！\n\n成功：{successCount}个\n失败：{failureCount}个";
        if (failureCount > 0 && failedItems.Count > 0)
        {
            message += $"\n\n失败的医案：\n{string.Join("、", failedItems.Take(5))}";
            if (failedItems.Count > 5)
                message += $"...等{failedItems.Count}个";
        }

        if (failureCount > 0)
            await ShowErrorMessageAsync(message);
        else
            await ShowSuccessMessageAsync(message);
    }

    #endregion

    #region 命令状态刷新

    protected override void RefreshCanExecuteChanged()
    {
        base.RefreshCanExecuteChanged();
        RaisePropertyChanged(nameof(DetailTitle));
        RaisePropertyChanged(nameof(HerbCount));
    }

    #endregion

    #region OpenSpec: unify-herb-list-controls - 处方编辑命令实现

    /// <summary>
    /// 加载所有药材列表
    /// </summary>
    private async Task LoadHerbsAsync()
    {
        try
        {
            // 使用SearchAsync("")获取所有药材（无过滤条件）
            var herbs = await _herbRepository.SearchAsync(string.Empty);
            AllHerbs = new ObservableCollection<HerbDetailDto>(herbs);
            Logger.LogDebug("加载药材列表完成: {Count}个", AllHerbs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载药材列表失败");
        }
    }

    /// <summary>
    /// 初始化处方编辑列表
    /// </summary>
    private void InitializeHerbItems(MedicalCaseDetailModel detail)
    {
        HerbItems.Clear();

        // 从PrescriptionItems转换为PrescriptionHerbItem
        if (detail.PrescriptionItems != null)
        {
            foreach (var dto in detail.PrescriptionItems)
            {
                var vm = CreatePrescriptionHerbItem();
                vm.HerbId = dto.HerbId;
                vm.HerbName = dto.HerbName ?? string.Empty;
                vm.Dosage = dto.Dosage;
                vm.Unit = dto.Unit;  // 必须从DTO复制单位
                if (string.IsNullOrEmpty(dto.Unit))
                {
                    Logger.LogWarning("药材 {HerbName} 的单位为空，HerbId: {HerbId}", dto.HerbName, dto.HerbId);
                }
                vm.SetUnitPrice(dto.UnitPrice);
                vm.DecocteMethod = dto.DecocteMethod;
                HerbItems.Add(vm);
            }
        }

        // 添加一个空行用于新增
        AddEmptyRow();

        RaisePropertyChanged(nameof(HerbCount));
    }

    /// <summary>
    /// 创建处方条目ViewModel
    /// </summary>
    private PrescriptionHerbItem CreatePrescriptionHerbItem()
    {
        var vm = new PrescriptionHerbItem
        {
            AllHerbs = AllHerbs
        };
        return vm;
    }

    /// <summary>
    /// 添加空行
    /// </summary>
    private void AddEmptyRow()
    {
        var emptyVm = CreatePrescriptionHerbItem();
        HerbItems.Add(emptyVm);
    }

    /// <summary>
    /// 删除药材
    /// </summary>
    private void OnDeleteHerb(PrescriptionHerbItem? item)
    {
        if (item == null) return;

        // 不能删除最后一个空行
        if (HerbItems.Count == 1 && item.HerbId == Guid.Empty) return;

        HerbItems.Remove(item);

        // 如果删除后没有空行，添加一个
        if (!HerbItems.Any(x => x.HerbId == Guid.Empty))
        {
            AddEmptyRow();
        }

        RaisePropertyChanged(nameof(HerbCount));
        Logger.LogDebug("删除药材: {HerbName}", item.HerbName);
    }

    /// <summary>
    /// 剂量输入完成 - 自动添加新行
    /// </summary>
    private void OnDosageCompleted(PrescriptionHerbItem? item)
    {
        if (item == null || item.HerbId == Guid.Empty) return;

        // 如果当前行是最后一行且已有数据，添加新空行
        var index = HerbItems.IndexOf(item);
        if (index == HerbItems.Count - 1)
        {
            AddEmptyRow();
        }

        RaisePropertyChanged(nameof(HerbCount));
    }

    /// <summary>
    /// 添加新行
    /// </summary>
    private void OnAddNewRow()
    {
        // 确保只有一个空行
        if (!HerbItems.Any(x => x.HerbId == Guid.Empty))
        {
            AddEmptyRow();
        }

        RaisePropertyChanged(nameof(HerbCount));
    }

    /// <summary>
    /// 收集处方数据用于保存
    /// Bug Fix: 过滤条件必须同时检查HerbId和Dosage
    /// </summary>
    private List<PrescriptionItemDto> CollectPrescriptionItems()
    {
        return HerbItems
            .Where(x => x.HerbId != Guid.Empty && x.Dosage > 0)
            .Select(x => new PrescriptionItemDto
            {
                HerbId = x.HerbId,
                HerbName = x.HerbName,
                Dosage = x.Dosage,
                UnitPrice = x.UnitPrice,
                DecocteMethod = x.DecocteMethod,
                Subtotal = x.ItemAmount
            })
            .ToList();
    }

    #endregion
}
