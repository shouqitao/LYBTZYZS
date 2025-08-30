using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.MedicalCase;
using LYBT.Desktop.Services;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Regions;
using Prism.Services.Dialogs;
// UltraThink四层架构重构：使用新的三层架构组件实现医疗案例管理

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的批量操作、复杂筛选、多选功能，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的医疗案例管理需求
    /// </summary>
    public class MedicalCaseListViewModel : NewBaseListViewModel<MedicalCaseDto>
    {
        #region Fields

        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ICustomDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装和筛选功能
        private MedicalCaseDto? _selectedMedicalCase;

        #endregion

        #region Properties

        /// <summary>选中的医疗案例 - UltraThink v2.0: 直接使用DTO</summary>
        public MedicalCaseDto? SelectedMedicalCase
        {
            get => _selectedMedicalCase;
            set
            {
                if (SetProperty(ref _selectedMedicalCase, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                    StartConsultationCommand.RaiseCanExecuteChanged();
                    CompleteCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // UltraThink v2.0: 删除复杂筛选功能 - 20人以下小诊所不需要复杂的状态筛选、日期筛选、紧急筛选
        // 删除批量选择功能 - 20人以下小诊所不需要复杂的多选和批量操作
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> EditCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> DeleteCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> ViewDetailsCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> StartConsultationCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> CompleteCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> CancelCommand { get; private set; }

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - BatchStartConsultationCommand/BatchCompleteCommand/BatchCancelCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计
        // - ClearFiltersCommand/ShowTodayCasesCommand/ShowUrgentCasesCommand: 复杂筛选过度设计
        // - ExportCommand: 导出功能过度设计

        #endregion

        #region Constructor

        public MedicalCaseListViewModel(
            IMedicalCaseService medicalCaseService,
            ICustomDialogService dialogService,
            IRegionManager regionManager,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<MedicalCaseListViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除InitializeStatusFilters(): 复杂筛选功能已移除
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除InitializeAsync(): 直接使用基类的数据加载机制
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddMedicalCaseAsync());
            EditCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await EditMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            DeleteCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await DeleteMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            ViewDetailsCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await ViewDetailsAsync(medicalCase), CanExecuteMedicalCaseCommand);
            StartConsultationCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await StartConsultationAsync(medicalCase), CanExecuteMedicalCaseCommand);
            CompleteCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await CompleteMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            CancelCommand = new DelegateCommand<MedicalCaseDto>(async medicalCase => await CancelMedicalCaseAsync(medicalCase), CanExecuteMedicalCaseCommand);
            
            // UltraThink v2.0: 删除批量操作和筛选命令初始化 - 20人以下小诊所不需要复杂的批量操作和筛选功能
        }

        private bool CanExecuteMedicalCaseCommand(MedicalCaseDto medicalCase)
        {
            return medicalCase != null && !IsLoading;
        }

        #endregion

        // UltraThink v2.0: 删除复杂初始化功能 - 20人以下小诊所不需要复杂的状态筛选初始化
        // 直接使用基类的数据加载机制，无需复杂的初始化逻辑

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<MedicalCaseDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 直接使用PagedQueryBaseDto进行医疗案例查询，删除复杂筛选
            var queryDto = new MedicalCaseQueryDto
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                Keyword = request.Keyword
            };
            return await _medicalCaseService.GetPagedAsync(queryDto);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion
        
        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<MedicalCaseDto>数据

        #region CRUD Operations

        private async Task AddMedicalCaseAsync()
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                var result = await _dialogService.ShowDialogAsync("CreateMedicalCaseDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync("医疗案例创建成功", "成功");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "添加医疗案例失败");
                await _dialogService.ShowErrorAsync($"添加医疗案例失败: {ex.Message}", "错误");
            }
        }

        private async Task EditMedicalCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = true,
                    ["MedicalCase"] = medicalCase
                };
                
                var result = await _dialogService.ShowDialogAsync("CreateMedicalCaseDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync($"医疗案例 {medicalCase.PatientName} 更新成功", "成功");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "编辑医疗案例失败: {MedicalCaseId}", medicalCase.Id);
                await _dialogService.ShowErrorAsync($"编辑医疗案例失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteMedicalCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除医疗案例吗？\n患者：{medicalCase.PatientName}\n医生：{medicalCase.DoctorName}\n此操作不可恢复。",
                "确认删除");

            if (confirm)
            {
                try
                {
                    var result = await _medicalCaseService.DeleteAsync(medicalCase.Id);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例删除成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "医疗案例删除失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "删除医疗案例失败: {MedicalCaseId}", medicalCase.Id);
                    await _dialogService.ShowErrorAsync($"医疗案例删除失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        #region Business Operations

        private async Task ViewDetailsAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;

            try
            {
                // 导航到详情界面
                _regionManager.RequestNavigate(RegionNames.SystemWorkbenchContentRegion, $"MedicalCaseDetailView?MedicalCaseId={medicalCase.Id}&ViewMode=Detail");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看医疗案例详情失败: {MedicalCaseId}", medicalCase.Id);
                await _dialogService.ShowErrorAsync($"查看医疗案例详情失败: {ex.Message}", "错误");
            }
        }

        private async Task StartConsultationAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;

            try
            {
                // 开始看诊 = 恢复医疗案例到诊断状态
                var result = await _medicalCaseService.ResumeAsync(medicalCase.Id);

                if (result.IsSuccess)
                {
                    // 导航到看诊界面
                    _regionManager.RequestNavigate(RegionNames.ConsultationWorkbenchContentRegion, 
                        $"ConsultationMainView?MedicalCaseId={medicalCase.Id}&PatientId={medicalCase.PatientId}&ConsultationMode=Start");
                    
                    await RefreshDataAsync();
                    await _dialogService.ShowInformationAsync("已成功开始看诊", "成功");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "开始看诊失败",
                        "错误");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊失败: {MedicalCaseId}", medicalCase.Id);
                await _dialogService.ShowErrorAsync($"开始看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task CompleteMedicalCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要完成医疗案例吗？\n患者：{medicalCase.PatientName}",
                "确认完成");

            if (confirm)
            {
                try
                {
                    // 完成看诊，需要提供完成原因
                    var result = await _medicalCaseService.CompleteAsync(medicalCase.Id, "诊断完成");

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例已完成", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "完成医疗案例失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "完成医疗案例失败: {MedicalCaseId}", medicalCase.Id);
                    await _dialogService.ShowErrorAsync($"完成医疗案例失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task CancelMedicalCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;

            // 获取取消原因
            var reason = await _dialogService.ShowInputAsync(
                "请输入取消医疗案例的原因：", 
                "取消原因", 
                "患者临时有事");

            if (string.IsNullOrEmpty(reason))
                return; // 用户取消了输入

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要取消医疗案例吗？\n患者：{medicalCase.PatientName}\n原因：{reason}",
                "确认取消");

            if (confirm)
            {
                try
                {
                    var result = await _medicalCaseService.CancelConsultationAsync(medicalCase.Id, reason);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例已取消", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "取消医疗案例失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "取消医疗案例失败: {MedicalCaseId}", medicalCase.Id);
                    await _dialogService.ShowErrorAsync($"取消医疗案例失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchStartConsultationAsync, BatchCompleteAsync, BatchCancelAsync 等功能

        // UltraThink v2.0: 删除所有筛选功能 - 20人以下小诊所不需要复杂的筛选功能
        // 包括: ClearFilters, ShowTodayCases, ShowUrgentCases, GetStatusFromFilter 等功能

        // UltraThink v2.0: 删除导出功能 - 20人以下小诊所不需要复杂的导出功能
        // 包括: ExportMedicalCasesAsync 等功能

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
    }
}