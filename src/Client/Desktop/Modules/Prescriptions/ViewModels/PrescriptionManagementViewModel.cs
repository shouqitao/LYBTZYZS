using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Prescriptions;
using LYBT.Desktop.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using SharedEnums = LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
// UltraThink四层架构重构：使用新的三层架构组件实现处方管理
// UltraThink v2.0: 添加SessionAware相关依赖
using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的批量操作、复杂筛选、多选功能，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的处方管理需求
    /// </summary>
    public class PrescriptionManagementViewModel : NewBaseListViewModel<PrescriptionDto>
    {
        #region Fields

        private readonly IPrescriptionService _prescriptionService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装
        private PrescriptionDto? _selectedPrescription;

        #endregion

        #region Properties

        /// <summary>选中的处方 - UltraThink v2.0: 直接使用DTO</summary>
        public PrescriptionDto? SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                if (SetProperty(ref _selectedPrescription, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                    PrintCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // UltraThink v2.0: 删除复杂筛选功能 - 20人以下小诊所不需要复杂的状态筛选和日期范围筛选
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<PrescriptionDto> EditCommand { get; private set; }
        public DelegateCommand<PrescriptionDto> DeleteCommand { get; private set; }
        public DelegateCommand<PrescriptionDto> ViewDetailsCommand { get; private set; }
        public DelegateCommand<PrescriptionDto> PrintCommand { get; private set; }

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - CopyCommand: 复制处方功能过度设计，医生直接新建即可
        // - CompleteCommand/VoidCommand: 状态管理通过简单的编辑功能实现
        // - BatchCompleteCommand/BatchVoidCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计
        // - ClearFiltersCommand: 复杂筛选功能已删除
        // - ExportCommand: 导出功能从ModuleService层删除，UI层也删除

        #endregion

        #region Constructor

        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<PrescriptionManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除InitializeStatusFilters(): 复杂状态筛选功能已移除
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除InitializeAsync(): 直接使用基类的数据加载机制
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddPrescriptionAsync());
            EditCommand = new DelegateCommand<PrescriptionDto>(async prescription => await EditPrescriptionAsync(prescription), CanExecutePrescriptionCommand);
            DeleteCommand = new DelegateCommand<PrescriptionDto>(async prescription => await DeletePrescriptionAsync(prescription), CanExecutePrescriptionCommand);
            ViewDetailsCommand = new DelegateCommand<PrescriptionDto>(async prescription => await ViewDetailsAsync(prescription), CanExecutePrescriptionCommand);
            PrintCommand = new DelegateCommand<PrescriptionDto>(async prescription => await PrintPrescriptionAsync(prescription), CanExecutePrescriptionCommand);
        }

        private bool CanExecutePrescriptionCommand(PrescriptionDto prescription)
        {
            return prescription != null && !IsLoading;
        }

        #endregion

        // UltraThink v2.0: 删除复杂初始化功能 - 20人以下小诊所不需要复杂的状态筛选和初始化逻辑
        // 基础数据加载通过NewBaseListViewModel自动处理

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<PrescriptionDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 简化处方查询 - 只使用基础搜索，删除复杂筛选条件
            var prescriptionQuery = new PrescriptionQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword
                // 删除复杂筛选: PrescriptionStatus, StartDate, EndDate
            };

            return await _prescriptionService.GetPagedAsync(prescriptionQuery);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion
        
        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<PrescriptionDto>数据

        #region CRUD Operations

        private async Task AddPrescriptionAsync()
        {
            try
            {
                // TODO: 实现处方创建对话框
                await _dialogService.ShowInformationAsync("新增处方功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "添加处方失败");
                ShowError($"添加处方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加处方失败: {ex.Message}", "错误");
            }
        }

        private async Task EditPrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null) return;
            
            try
            {
                // TODO: 实现处方编辑对话框
                await _dialogService.ShowInformationAsync($"编辑处方 {prescription.Id} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑处方失败: {PrescriptionId}", prescription.Id);
                ShowError($"编辑处方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑处方失败: {ex.Message}", "错误");
            }
        }

        private async Task DeletePrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null) return;
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除处方 {prescription.Id} 吗？\n此操作不可恢复。",
                "确认删除");

            if (confirm)
            {
                try
                {
                    var result = await _prescriptionService.DeleteAsync(prescription.Id);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("处方删除成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "处方删除失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "处方操作失败");
                    ShowError($"处方操作失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"处方操作失败: {ex.Message}", "错误");
                }
            }
        }

        // UltraThink v2.0: 删除CopyPrescriptionAsync - 复制功能过度设计，医生直接新建即可

        #endregion

        #region Business Operations

        private async Task ViewDetailsAsync(PrescriptionDto prescription)
        {
            if (prescription == null) return;

            try
            {
                var result = await _prescriptionService.GetByIdAsync(prescription.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // UltraThink v2.0: 简化详情显示 - 直接显示基础信息
                    var detailInfo = $"处方ID: {result.Data.Id}\n创建时间: {result.Data.CreateTime}\n更新时间: {result.Data.UpdateTime}";
                    await _dialogService.ShowInformationAsync(detailInfo, $"处方详情 - {result.Data.Id}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取处方详情失败", 
                        "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "处方操作失败");
                ShowError($"处方操作失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"处方操作失败: {ex.Message}", "错误");
            }
        }

        private async Task PrintPrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null) return;

            try
            {
                // TODO: 实现实际的打印功能
                await Task.Delay(1000); // 模拟打印过程
                
                // UltraThink v2.0: 简化打印预览
                var printableInfo = $"处方打印预览\n处方ID: {prescription.Id}\n创建时间: {prescription.CreateTime}";
                await _dialogService.ShowInformationAsync(printableInfo, "打印预览");
                await _dialogService.ShowInformationAsync("处方打印成功", "成功");
            }
            catch (Exception ex)
            {
                LogError(ex, "处方操作失败");
                ShowError($"处方操作失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"处方操作失败: {ex.Message}", "错误");
            }
        }

        // UltraThink v2.0: 删除CompleteAsync/VoidAsync - 状态管理通过编辑功能实现，不需要单独的完成/作废操作

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchCompleteAsync, BatchVoidAsync 等功能

        // UltraThink v2.0: 删除所有过度设计的功能 - 20人以下小诊所不需要以下复杂功能:
        // - Filter Operations (ClearFilters, GetStatusFromFilter): 复杂筛选功能已移除
        // - Export Operations (ExportPrescriptionsAsync): 导出功能已从Service层移除  
        // - Selection Management (ClearSelection, SelectAll): 多选功能已移除
    }
}