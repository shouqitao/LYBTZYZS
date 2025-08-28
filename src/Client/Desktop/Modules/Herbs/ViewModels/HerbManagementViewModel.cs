using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Herbs;
using LYBT.Desktop.Services;
using LYBT.Desktop.Herbs.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的批量操作、多选功能，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的药材管理需求
    /// </summary>
    public class HerbManagementViewModel : NewBaseListViewModel<HerbDto>
    {
        #region Fields

        private readonly HerbModule _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装
        private HerbDto? _selectedHerb;

        #endregion

        #region Properties

        /// <summary>选中的药材 - UltraThink v2.0: 直接使用DTO</summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // 暴露基类的搜索和分页属性供XAML绑定
        public string SearchKeyword
        {
            get => SearchManager.SearchKeyword;
            set => SearchManager.SearchKeyword = value;
        }

        public DelegateCommand SearchCommand { get; private set; }

        public int CurrentPage => PaginationCoordinator.CurrentPage;
        public int TotalPages => PaginationCoordinator.TotalPages;
        public DelegateCommand FirstPageCommand { get; private set; }
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand LastPageCommand { get; private set; }

        public string StatusText => $"共 {PaginationCoordinator.TotalCount} 条记录";

        // UltraThink v2.0: 删除批量选择功能 - 20人以下小诊所不需要复杂的多选和批量操作
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<HerbDto> EditCommand { get; private set; }
        public DelegateCommand<HerbDto> DeleteCommand { get; private set; }
        public DelegateCommand<HerbDto> ToggleStatusCommand { get; private set; }
        public DelegateCommand<HerbDto> ViewDetailsCommand { get; private set; }
        public DelegateCommand ImportHerbsCommand { get; private set; }
        public DelegateCommand ExportTemplateCommand { get; private set; }
        // 注意：RefreshCommand由基类NewBaseListViewModel提供，不需要重复定义

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - BatchEnableCommand/BatchDisableCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计

        #endregion

        #region Constructor

        public HerbManagementViewModel(
            HerbModule herbService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<HerbManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            System.Diagnostics.Debug.WriteLine("🌿 HerbManagementViewModel 构造函数开始");
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            System.Diagnostics.Debug.WriteLine("✅ HerbManagementViewModel 构造函数完成");

            InitializeCommands();
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除RefreshDataAsync(): 直接使用基类的数据加载机制
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            // 重要：必须先调用基类的命令初始化 (包含RefreshCommand)
            base.InitializeCommands();
            
            AddCommand = new DelegateCommand(async () => await AddHerbAsync());
            EditCommand = new DelegateCommand<HerbDto>(async herb => await EditHerbAsync(herb), CanExecuteHerbCommand);
            DeleteCommand = new DelegateCommand<HerbDto>(async herb => await DeleteHerbAsync(herb), CanExecuteHerbCommand);
            ToggleStatusCommand = new DelegateCommand<HerbDto>(async herb => await ToggleStatusAsync(herb), CanExecuteHerbCommand);
            ViewDetailsCommand = new DelegateCommand<HerbDto>(async herb => await ViewDetailsAsync(herb), CanExecuteHerbCommand);
            ImportHerbsCommand = new DelegateCommand(async () => await ImportHerbsAsync());
            ExportTemplateCommand = new DelegateCommand(async () => await ExportTemplateAsync());
            // RefreshCommand由基类NewBaseListViewModel提供，已修复async void问题
            
            // 初始化搜索和分页命令
            SearchCommand = new DelegateCommand(async () => await SearchManager.ExecuteSearchAsync());
            FirstPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToFirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToPreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToNextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToLastPageAsync());
            
            // UltraThink v2.0: 删除批量操作命令初始化 - 20人以下小诊所不需要复杂的批量操作
        }

        private bool CanExecuteHerbCommand(HerbDto herb)
        {
            return herb != null && !IsLoading;
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<HerbDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // 转换为药材查询DTO
            var herbQuery = new HerbPagedQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword
            };

            return await _herbService.GetPagedAsync(herbQuery);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion
        
        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<HerbDto>数据

        #region CRUD Operations

        private async Task AddHerbAsync()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };
                
                var result = await _dialogService.ShowDialogAsync("HerbAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync("药材信息添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "添加药材失败");
                ShowError($"添加药材失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加药材失败: {ex.Message}", "错误");
            }
        }

        private async Task EditHerbAsync(HerbDto herb)
        {
            if (herb == null) return;
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = true,
                    ["Herb"] = herb
                };
                
                var result = await _dialogService.ShowDialogAsync("HerbAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync($"药材 {herb.Name} 信息更新成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑药材失败: {HerbId}", herb.Id);
                ShowError($"编辑药材失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑药材失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteHerbAsync(HerbDto herb)
        {
            if (herb == null) return;
            
            // 药材信息不支持真正删除，只能禁用
            await ToggleStatusAsync(herb);
        }

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(HerbDto herb)
        {
            if (herb == null) return;

            var isEnabled = herb.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}药材 {herb.Name} 吗？",
                $"{action}药材");

            if (confirm)
            {
                try
                {
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _herbService.DisableAsync(herb.Id);
                    }
                    else
                    {
                        result = await _herbService.EnableAsync(herb.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"药材{action}成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? $"药材{action}失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "切换药材状态失败: {HerbId}", herb.Id);
                    ShowError($"药材{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"药材{action}失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task ViewDetailsAsync(HerbDto herb)
        {
            if (herb == null) return;

            try
            {
                var result = await _herbService.GetByIdAsync(herb.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var herbDetail = result.Data;
                    var detailInfo = $"药材详情：\n\n" +
                                   $"名称: {herbDetail.Name}\n" +
                                   $"产地: {herbDetail.Origin ?? "未知"}\n" +
                                   $"规格: {herbDetail.Spec ?? "未知"}\n" +
                                   $"单价: ¥{herbDetail.Price:F2}/{herbDetail.Unit}\n" +
                                   $"功效: {herbDetail.Effect ?? "未录入"}\n" +
                                   $"用法: {herbDetail.Usage ?? "未录入"}\n" +
                                   $"状态: {(herbDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                                   $"备注: {herbDetail.Remark ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"药材详情 - {herbDetail.Name}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取药材详情失败", 
                        "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "查看药材详情失败: {HerbId}", herb.Id);
                ShowError($"查看药材详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看药材详情失败: {ex.Message}", "错误");
            }
        }

        private async Task ImportHerbsAsync()
        {
            try
            {
                var filePath = await _dialogService.ShowOpenFileDialogAsync("选择药材导入文件", "Excel文件|*.xlsx;*.xls|CSV文件|*.csv|所有文件|*.*");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    // 这里应该实现实际的导入逻辑
                    await _dialogService.ShowInformationAsync(
                        $"已选择导入文件：\n{filePath}\n\n药材批量导入功能将在后续版本中提供\n\n当前支持：\n• 手动创建药材\n• 编辑现有药材\n• 药材状态管理", 
                        "导入功能说明");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "导入药材失败");
                ShowError($"导入药材失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"导入药材失败: {ex.Message}", "错误");
            }
        }

        private async Task ExportTemplateAsync()
        {
            try
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync("导出药材模板", "Excel文件|*.xlsx|CSV文件|*.csv|所有文件|*.*", "药材导入模板.xlsx");
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    // 这里应该实现实际的模板导出逻辑
                    await _dialogService.ShowInformationAsync(
                        $"模板导出路径：\n{filePath}\n\n药材导入模板生成功能将在后续版本中提供\n\n模板将包含：\n• 药材名称\n• 拼音码\n• 产地\n• 规格\n• 单位\n• 单价\n• 功效\n• 用法", 
                        "模板导出说明");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "导出药材模板失败");
                ShowError($"导出药材模板失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"导出药材模板失败: {ex.Message}", "错误");
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchEnableAsync, BatchDisableAsync 等功能

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
    }
}