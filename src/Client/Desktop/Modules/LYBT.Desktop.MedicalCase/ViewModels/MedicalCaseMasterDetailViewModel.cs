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
using LYBT.Shared.Models.Contracts.Consultation;
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

    #region 数据模型属性 - OpenSpec: unify-medicalcase-item-editmodel

    private ConsultationItem? _consultation;
    private PrescriptionItem? _prescription;
    private ObservableCollection<HerbListDto> _allHerbs = new();

    /// <summary>
    /// 诊断数据模型
    /// OpenSpec: unify-medicalcase-item-editmodel - 统一使用 ConsultationItem
    /// </summary>
    public ConsultationItem? Consultation
    {
        get => _consultation;
        private set => SetProperty(ref _consultation, value);
    }

    /// <summary>
    /// 处方数据模型
    /// OpenSpec: unify-medicalcase-item-editmodel - 统一使用 PrescriptionItem
    /// </summary>
    public PrescriptionItem? Prescription
    {
        get => _prescription;
        private set => SetProperty(ref _prescription, value);
    }

    /// <summary>所有药材列表 - 用于拼音自动补全</summary>
    public ObservableCollection<HerbListDto> AllHerbs
    {
        get => _allHerbs;
        private set => SetProperty(ref _allHerbs, value);
    }

    #endregion

    /// <summary>
    /// 构造函数
    /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
    /// </summary>
    public MedicalCaseMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> masterDetailServices,
        IMedicalCaseRepository repository,
        IHerbRepository herbRepository)
        : base(viewModelServices, masterDetailServices)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));

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
            await MasterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
            {
                var pagedData = await _repository.GetPagedAsync(CurrentPage, PageSize, SearchText);
                MasterDetailServices.Pagination.TotalCount = pagedData.TotalCount;

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
            MasterDetailServices.ErrorHandler.HandleException(ex, "获取医案列表");
        }
    }

    /// <summary>加载详情数据</summary>
    /// <remarks>OpenSpec: unify-control-data-binding - 使用对象化编辑模型</remarks>
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

            // OpenSpec: unify-control-data-binding - 初始化对象化编辑模型
            InitializeEditModels(detail);

            MasterDetailServices.DetailEditor.LoadDetail(detail);
            OnPropertyChanged(nameof(DetailTitle));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载医案详情失败: {MedicalCaseId}", item.Id);
            MasterDetailServices.ErrorHandler.HandleException(ex, "加载医案详情");
        }
    }

    /// <summary>创建新详情实例 - 医案不支持新建</summary>
    protected override MedicalCaseDetailModel CreateNewDetail()
    {
        // 医案管理模块不支持新建，此方法不应被调用
        throw new NotSupportedException("医案管理模块不支持新建医案，请通过看诊入口创建");
    }

    /// <summary>保存详情</summary>
    /// <remarks>OpenSpec: unify-control-data-binding - 使用对象化编辑模型</remarks>
    protected override async Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail)
    {
        try
        {
            // 从编辑模型构建输入DTO
            var prescriptionItems = Prescription?.Items
                .Where(x => x.HerbId != Guid.Empty && x.Dosage > 0)
                .Select(x => new PrescriptionItemInputDto
                {
                    HerbId = x.HerbId,
                    HerbName = x.HerbName ?? string.Empty,
                    Dosage = x.Dosage,
                    Unit = x.Unit ?? "g",
                    UnitPrice = x.UnitPrice,
                    Subtotal = x.Dosage * x.UnitPrice,
                    DecocteMethod = x.DecocteMethod
                })
                .ToList() ?? [];

            var aggregateDto = new MedicalCaseInputDto
            {
                Id = detail.Id,
                PatientId = detail.PatientId,
                UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                Remark = Prescription?.Remark,
                Consultation = new ConsultationInputDto
                {
                    PresentIllness = Consultation?.PresentIllness,
                    TongueDiagnosis = Consultation?.TongueDiagnosis,
                    PulseDiagnosis = Consultation?.PulseDiagnosis,
                    TcmDiagnosis = Consultation?.TcmDiagnosis
                },
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = prescriptionItems.Count > 0,
                    DosageCount = Prescription?.DosageCount ?? 7,
                    ReferencedFormulas = detail.ReferencedFormulas,
                    Items = prescriptionItems
                }
            };

            await _repository.SaveAsync(detail.Id, aggregateDto);

            Logger.LogInformation("医案保存成功: {MedicalCaseId}, 药材数量: {HerbCount}",
                detail.Id, prescriptionItems.Count);

            OnPropertyChanged(nameof(DetailTitle));
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案失败: {MedicalCaseId}", detail.Id);
            var errorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存医案", ex);
            MasterDetailServices.ErrorHandler.SetError("Save", errorMessage);
            return false;
        }
    }

    /// <summary>删除项</summary>
    protected override async Task<bool> DeleteItemAsync(MedicalCaseListDto item)
    {
        var success = await _repository.DeleteAsync(item.Id);
        if (!success)
        {
            MasterDetailServices.ErrorHandler.SetError("Delete", $"删除医案失败");
        }
        else
        {
            Logger.LogInformation("医案删除成功: {MedicalCaseId}", item.Id);
        }
        return success;
    }

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

    /// <summary>
    /// 初始化数据模型
    /// OpenSpec: unify-medicalcase-item-editmodel - 统一使用 Item 类
    /// </summary>
    private void InitializeEditModels(MedicalCaseDetailModel detail)
    {
        // 初始化诊断数据模型
        // OpenSpec: unify-medicalcase-item-editmodel - 统一使用 ConsultationItem
        Consultation = new ConsultationItem
        {
            PresentIllness = detail.PresentIllness,
            TongueDiagnosis = detail.TongueDiagnosis,
            PulseDiagnosis = detail.PulseDiagnosis,
            TcmDiagnosis = detail.TcmDiagnosis
        };

        // 初始化处方数据模型
        // OpenSpec: unify-medicalcase-item-editmodel - 统一使用 PrescriptionItem
        Prescription = new PrescriptionItem
        {
            DosageCount = detail.DoseCount ?? 7,
            Remark = detail.Remark
        };

        // 加载处方药材列表（直接使用PrescriptionItemDto）
        // OpenSpec: unify-control-data-binding - 统一类型，无需转换
        if (detail.PrescriptionItems != null)
        {
            foreach (var prescriptionItem in detail.PrescriptionItems)
            {
                Prescription.Items.Add(prescriptionItem);
            }
        }

        // 计算单帖价格
        Prescription.SingleDosePrice = Prescription.Items.Sum(x => x.Dosage * x.UnitPrice);

        Prescription.NotifyItemsChanged();
        Logger.LogDebug("编辑模型初始化完成: 诊断={HasConsultation}, 处方药材数={ItemCount}",
            !string.IsNullOrEmpty(Consultation.TcmDiagnosis),
            Prescription.Items.Count);
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
