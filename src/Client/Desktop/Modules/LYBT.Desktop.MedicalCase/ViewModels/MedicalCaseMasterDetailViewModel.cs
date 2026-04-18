using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.ViewModels.Workspace;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
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
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IHerbSearchProvider _herbSearchProvider;
    private readonly MedicalCaseDetailModelMapper _mapper;
    private readonly IDesktopCacheManager _cacheManager;
    private readonly ILoggerFactory _loggerFactory;

    #region Child VMs

    public ConsultationEditorViewModel ConsultationEditor { get; }
    public PrescriptionEditorViewModel PrescriptionEditor { get; }

    #endregion

    #region 扩展属性

    /// <inheritdoc/>
    protected override string EntityDisplayName => "医案";

    /// <inheritdoc/>
    protected override string? GetDetailDisplayName() => CurrentDetail?.PatientName;

    /// <summary>选中项的患者姓名</summary>
    public string SelectedPatientName => SelectedItem?.PatientName ?? string.Empty;

    #endregion

    /// <summary>
    /// 构造函数
    /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
    /// Wave 2: Replace IMedicalCaseRepository with IMedicalCaseService, add child VMs
    /// </summary>
    public MedicalCaseMasterDetailViewModel(
        IViewModelServices viewModelServices,
        IMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel> masterDetailServices,
        IMedicalCaseService medicalCaseService,
        IHerbSearchProvider herbSearchProvider,
        IDesktopCacheManager cacheManager,
        MedicalCaseDetailModelMapper mapper,
        ILoggerFactory loggerFactory)
        : base(viewModelServices, masterDetailServices)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _herbSearchProvider = herbSearchProvider ?? throw new ArgumentNullException(nameof(herbSearchProvider));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        // Create child VMs with minimal IWorkspaceHost adapter
        var host = new MasterDetailWorkspaceHost(this);
        ConsultationEditor = new ConsultationEditorViewModel(new MasterDetailWorkspaceContext(this), host, loggerFactory);
        PrescriptionEditor = new PrescriptionEditorViewModel(new MasterDetailWorkspaceContext(this), host, loggerFactory);

        PageTitle = "医案管理";
        // DetailTitle 已由基类自动通知
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
                var pagedData = await _medicalCaseService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                MasterDetailServices.Pagination.TotalCount = pagedData?.TotalCount ?? 0;

                Items.Clear();
                foreach (var item in pagedData?.Items ?? Enumerable.Empty<MedicalCaseListDto>())
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
    protected override async Task LoadDetailAsync(MedicalCaseListDto item)
    {
        try
        {
            var (success, detail, errorMessage) = await _medicalCaseService.LoadDetailsAsync(item.Id);
            if (!success || detail == null)
            {
                Logger.LogWarning("医案详情不存在: {MedicalCaseId}", item.Id);
                return;
            }

            // Map to display model
            var displayModel = _mapper.ToItem(detail);
            MasterDetailServices.DetailEditor.LoadDetail(displayModel);

            // Initialize child VMs from cached DTOs
            if (_medicalCaseService.CachedConsultation != null)
                ConsultationEditor.InitializeFromDto(_medicalCaseService.CachedConsultation);

            if (_medicalCaseService.CachedPrescription != null)
                PrescriptionEditor.InitializeFromDto(_medicalCaseService.CachedPrescription);
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
    protected override async Task<bool> SaveDetailAsync(MedicalCaseDetailModel detail)
    {
        try
        {
            var consultationData = ConsultationEditor.GetConsultationData();
            var prescriptionData = PrescriptionEditor.GetPrescriptionData();

            var result = await _medicalCaseService.AggregateSaveAsync(
                detail.Id,
                consultationData,
                prescriptionData,
                detail.Remark);

            if (result.Success)
            {
                Logger.LogInformation("医案保存成功: {MedicalCaseId}", detail.Id);
                _cacheManager.InvalidateMedicalCaseCaches();
                return true;
            }

            Logger.LogError("医案保存失败: {MedicalCaseId}, 错误: {Error}", detail.Id, result.Error);
            MasterDetailServices.ErrorHandler.SetError("Save", result.Error ?? "保存医案失败");
            return false;
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
        var (success, errorMessage) = await _medicalCaseService.CancelMedicalCaseAsync(item.Id);
        if (!success)
        {
            MasterDetailServices.ErrorHandler.SetError("Delete", errorMessage ?? "删除医案失败");
        }
        else
        {
            Logger.LogInformation("医案删除成功: {MedicalCaseId}", item.Id);
            _cacheManager.InvalidateMedicalCaseCaches();
        }
        return success;
    }

    #endregion

    #region 辅助方法

    /// <summary>所有药材列表 - 用于拼音自动补全</summary>
    public ObservableCollection<HerbListDto> AllHerbs { get; } = new();

    /// <summary>加载所有药材列表</summary>
    private async Task LoadHerbsAsync()
    {
        try
        {
            var herbs = await _herbSearchProvider.SearchHerbsAsync(string.Empty);
            AllHerbs.Clear();
            foreach (var herb in herbs)
            {
                AllHerbs.Add(herb);
            }
            Logger.LogDebug("加载药材列表完成: {Count}个", AllHerbs.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载药材列表失败");
        }
    }

    #endregion

    #region 导航

    protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
    {
        await base.OnNavigatedToAsync(navigationContext);

        // 预加载药材列表
        if (AllHerbs.Count == 0)
            await LoadHerbsAsync();
    }

    #endregion

    #region Adapter classes for child VMs

    /// <summary>
    /// Minimal IWorkspaceHost adapter for MasterDetail VM.
    /// Delegates to ErrorHandler/DialogService from IViewModelServices.
    /// </summary>
    private sealed class MasterDetailWorkspaceHost : IWorkspaceHost
    {
        private readonly MedicalCaseMasterDetailViewModel _parent;

        public MasterDetailWorkspaceHost(MedicalCaseMasterDetailViewModel parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public ICommonDialogService? CommonDialogService => _parent.CommonDialogService;

        public void NotifyStateChanged()
        {
            // MasterDetail doesn't have WorkspaceState recalculation; no-op for now.
        }

        public void SetBusy(bool isBusy, string? message = null)
        {
            // Delegate to MasterDetailServices.Loading
            // No direct isBusy property, but we could use Loading
        }

        public Task ShowErrorAsync(string message)
        {
            _parent.MasterDetailServices.ErrorHandler.SetError("Error", message);
            return Task.CompletedTask;
        }

        public Task ShowSuccessAsync(string message)
        {
            // No toast available in MasterDetail context
            _parent.MasterDetailServices.ErrorHandler.ClearError("Save");
            return Task.CompletedTask;
        }

        public async Task<bool> ShowConfirmAsync(string message, string title = "确认")
        {
            return await _parent.MasterDetailServices.Dialog.ShowConfirmAsync(title, message);
        }

        /// <summary>
        /// P1-2 FIX: RequestEnterEditMode is not applicable to MasterDetailWorkspaceHost
        /// (no edit mode state machine in this context)
        /// </summary>
        public void RequestEnterEditMode()
        {
            // No-op: MasterDetail doesn't have an edit mode state machine
        }
    }

    /// <summary>
    /// Minimal IMedicalCaseWorkspaceContext adapter for MasterDetail VM.
    /// </summary>
    private sealed class MasterDetailWorkspaceContext : IMedicalCaseWorkspaceContext
    {
        private readonly MedicalCaseMasterDetailViewModel _parent;

        public MasterDetailWorkspaceContext(MedicalCaseMasterDetailViewModel parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public WorkspaceState State => new();
        public Guid MedicalCaseId => _parent.CurrentDetail?.Id ?? Guid.Empty;
        public PatientDetailDto? CurrentPatient => null;
        public ISessionManager? SessionManager => _parent.SessionManager;
    }

    #endregion
}
