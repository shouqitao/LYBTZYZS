using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医案Master-Detail视图模型（组合模式）
/// OpenSpec: refactor-viewmodel-composition
///
/// 使用IMasterDetailServices实现组合模式
/// 注意：医案不支持新建，新建仅通过看诊入口创建
/// </summary>
public partial class MedicalCaseMasterDetailViewModel : MasterDetailViewModelBase<MedicalCaseListDto, MedicalCaseDetailModel>
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IHerbRepository _herbRepository;
    private readonly ISessionManager? _sessionManager;
    private readonly MedicalCaseDetailModelMapper _mapper = new();

    #region 扩展属性

    /// <summary>详情标题</summary>
    public string DetailTitle
    {
        get
        {
            if (CurrentDetail == null) return "医案详情";
            return IsEditMode ? $"编辑医案 - {CurrentDetail.PatientName}" : $"医案详情 - {CurrentDetail.PatientName}";
        }
    }

    /// <summary>选中项的患者姓名</summary>
    public string SelectedPatientName => SelectedItem?.PatientName ?? string.Empty;

    #endregion

    #region 处方编辑属性

    private ObservableCollection<HerbListDto> _allHerbs = new();
    private ObservableCollection<PrescriptionHerbItem> _herbItems = new();

    /// <summary>所有药材列表 - 用于拼音自动补全</summary>
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    /// <summary>处方药材编辑列表</summary>
    public ObservableCollection<PrescriptionHerbItem> HerbItems
    {
        get => _herbItems;
        private set => SetProperty(ref _herbItems, value);
    }

    /// <summary>药材数量</summary>
    public int HerbCount => HerbItems?.Count(x => x.HerbId != Guid.Empty) ?? 0;

    #endregion

    public MedicalCaseMasterDetailViewModel(
        IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> services,
        IMedicalCaseRepository repository,
        IHerbRepository herbRepository,
        
        ILoggerFactory loggerFactory,
        ISessionManager? sessionManager = null)
        : base(services, loggerFactory)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
        // OpenSpec: standardize-api-architecture - 使用直接Mapper实例替代MappingService
        _sessionManager = sessionManager;

        PageTitle = "医案管理";

        // 监听属性变化
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(CurrentDetail) or nameof(IsEditMode))
            {
                OnPropertyChanged(nameof(DetailTitle));
            }
        };
    }

    #region 基类抽象方法实现

    /// <summary>加载列表数据</summary>
    protected override async Task LoadListAsync()
    {
        Logger.LogInformation("医案搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'",
            CurrentPage, PageSize, SearchText);

        try
        {
            await Services.Loading.ExecuteWithLoadingAsync(async () =>
            {
                var pagedData = await _repository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                Services.Pagination.TotalCount = pagedData.TotalCount;

                Items.Clear();
                foreach (var item in pagedData.Items ?? Enumerable.Empty<MedicalCaseListDto>())
                {
                    Items.Add(item);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取医案列表时发生异常");
            Services.ErrorHandler.HandleException(ex, "获取医案列表");
        }
    }

    /// <summary>加载详情数据</summary>
    protected override async Task LoadDetailAsync(MedicalCaseListDto item)
    {
        try
        {
            // 确保药材列表已加载
            if (AllHerbs.Count == 0)
            {
                await LoadHerbsAsync();
            }

            var dto = await _repository.GetByIdAsync(item.Id);
            if (dto == null)
            {
                Logger.LogWarning("医案详情不存在: {MedicalCaseId}", item.Id);
                return;
            }

            var detail = _mapper.ToItem(dto);

            // 初始化处方编辑列表
            InitializeHerbItems(detail);

            Services.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(DetailTitle));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载医案详情失败: {MedicalCaseId}", item.Id);
            Services.ErrorHandler.HandleException(ex, "加载医案详情");
        }
    }

    /// <summary>创建新详情实例 - 医案不支持新建</summary>
    protected override MedicalCaseDetailModel CreateNewDetail()
    {
        // 医案管理模块不支持新建，此方法不应被调用
        throw new NotSupportedException("医案管理模块不支持新建医案，请通过看诊入口创建");
    }

    /// <summary>保存详情</summary>
    protected override async Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail)
    {
        try
        {
            var aggregateDto = new MedicalCaseInputDto
            {
                Id = detail.Id,
                PatientId = detail.PatientId,
                UserId = _sessionManager?.CurrentUser?.Id ?? Guid.Empty,
                Remark = detail.Remark,
                Consultation = detail.ToConsultationInputDto(),
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = HerbItems.Any(x => x.HerbId != Guid.Empty && x.Dosage > 0),
                    DosageCount = detail.DoseCount ?? 7,
                    ReferencedFormulas = detail.ReferencedFormulas,
                    Items = HerbItems
                        .Where(x => x.HerbId != Guid.Empty && x.Dosage > 0)
                        .Select(x => new PrescriptionItemInputDto
                        {
                            HerbId = x.HerbId,
                            HerbName = x.HerbName,
                            Dosage = x.Dosage,
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

            OnPropertyChanged(nameof(DetailTitle));
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案失败: {MedicalCaseId}", detail.Id);
            var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存医案", ex);
            Services.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
    }

    /// <summary>删除项</summary>
    protected override async Task<bool> DeleteItemAsync(MedicalCaseListDto item)
    {
        var success = await _repository.DeleteAsync(item.Id);
        if (!success)
        {
            Services.ErrorHandler.SetError("Delete", $"删除医案失败");
        }
        else
        {
            Logger.LogInformation("医案删除成功: {MedicalCaseId}", item.Id);
        }
        return success;
    }

    #endregion

    #region 处方编辑命令

    /// <summary>删除药材</summary>
    [RelayCommand(CanExecute = nameof(CanDeleteHerb))]
    private void DeleteHerb(PrescriptionHerbItem? item)
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

        OnPropertyChanged(nameof(HerbCount));
        Logger.LogDebug("删除药材: {HerbName}", item.HerbName);
    }

    private bool CanDeleteHerb(PrescriptionHerbItem? item) => item != null && IsEditMode;

    /// <summary>剂量输入完成 - 自动添加新行</summary>
    [RelayCommand]
    private void DosageCompleted(PrescriptionHerbItem? item)
    {
        if (item == null || item.HerbId == Guid.Empty) return;

        // 如果当前行是最后一行且已有数据，添加新空行
        var index = HerbItems.IndexOf(item);
        if (index == HerbItems.Count - 1)
        {
            AddEmptyRow();
        }

        OnPropertyChanged(nameof(HerbCount));
    }

    /// <summary>添加新行</summary>
    [RelayCommand(CanExecute = nameof(CanAddNewRow))]
    private void AddNewRow()
    {
        // 确保只有一个空行
        if (!HerbItems.Any(x => x.HerbId == Guid.Empty))
        {
            AddEmptyRow();
        }

        OnPropertyChanged(nameof(HerbCount));
    }

    private bool CanAddNewRow() => IsEditMode;

    #endregion

    #region 辅助方法

    /// <summary>加载所有药材列表</summary>
    private async Task LoadHerbsAsync()
    {
        try
        {
            var herbs = await _herbRepository.SearchAsync(string.Empty);
            AllHerbs = new ObservableCollection<HerbListDto>(herbs);
            Logger.LogDebug("加载药材列表完成: {Count}个", AllHerbs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载药材列表失败");
        }
    }

    /// <summary>初始化处方编辑列表</summary>
    private void InitializeHerbItems(MedicalCaseDetailModel detail)
    {
        HerbItems.Clear();

        if (detail.PrescriptionItems != null)
        {
            foreach (var dto in detail.PrescriptionItems)
            {
                var vm = CreatePrescriptionHerbItem();
                vm.HerbId = dto.HerbId;
                vm.HerbName = dto.HerbName ?? string.Empty;
                vm.Dosage = dto.Dosage;
                vm.Unit = dto.Unit;
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

        OnPropertyChanged(nameof(HerbCount));
    }

    /// <summary>创建处方条目ViewModel</summary>
    private PrescriptionHerbItem CreatePrescriptionHerbItem()
    {
        return new PrescriptionHerbItem
        {
            AllHerbs = AllHerbs
        };
    }

    /// <summary>添加空行</summary>
    private void AddEmptyRow()
    {
        var emptyVm = CreatePrescriptionHerbItem();
        HerbItems.Add(emptyVm);
    }

    #endregion

    #region 导航

    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 预加载药材列表
        if (AllHerbs.Count == 0)
        {
            await LoadHerbsAsync();
        }
    }

    #endregion
}
