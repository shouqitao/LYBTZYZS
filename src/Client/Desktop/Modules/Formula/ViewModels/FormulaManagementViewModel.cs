using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
// UltraThink v2.0: 移除Info模型引用，直接使用DTOs
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Formulas;
using LYBT.Desktop.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
// UltraThink四层架构重构：使用新的三层架构组件实现验方管理
// UltraThink v2.0: 添加SessionAware相关依赖
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的复制功能、分类筛选、批量操作，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的验方管理需求
    /// </summary>
    public class FormulaManagementViewModel : NewBaseListViewModel<FormulaDto>
    {
        #region Fields

        private readonly FormulaModuleService _formulaService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装和分类筛选
        private FormulaDto? _selectedFormula;

        #endregion

        #region Properties

        /// <summary>选中的验方 - UltraThink v2.0: 直接使用DTO</summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // UltraThink v2.0: 删除分类筛选功能 - 20人以下小诊所不需要复杂的分类筛选
        // UltraThink v2.0: 删除批量选择功能 - 20人以下小诊所不需要复杂的多选和批量操作
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<FormulaDto> EditCommand { get; private set; } = null!;
        public DelegateCommand<FormulaDto> DeleteCommand { get; private set; } = null!;
        public DelegateCommand<FormulaDto> ViewDetailsCommand { get; private set; } = null!;
        public DelegateCommand<FormulaDto> ToggleStatusCommand { get; private set; } = null!;
        public DelegateCommand ExportCommand { get; private set; } = null!;
        public DelegateCommand ImportCommand { get; private set; } = null!;

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - CopyCommand: 复制验方功能过度设计，医生直接新建即可
        // - BatchEnableCommand/BatchDisableCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计

        #endregion

        #region Constructor

        public FormulaManagementViewModel(
            FormulaModuleService formulaService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<FormulaManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除分类初始化: 分类筛选功能已移除
            // - 删除InitializeAsync(): 直接使用基类的数据加载机制
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddFormulaAsync());
            EditCommand = new DelegateCommand<FormulaDto>(async formula => await EditFormulaAsync(formula), CanExecuteFormulaCommand);
            DeleteCommand = new DelegateCommand<FormulaDto>(async formula => await DeleteFormulaAsync(formula), CanExecuteFormulaCommand);
            ViewDetailsCommand = new DelegateCommand<FormulaDto>(async formula => await ViewDetailsAsync(formula), CanExecuteFormulaCommand);
            ToggleStatusCommand = new DelegateCommand<FormulaDto>(async formula => await ToggleStatusAsync(formula), CanExecuteFormulaCommand);
            
            ExportCommand = new DelegateCommand(async () => await ExportFormulasAsync());
            ImportCommand = new DelegateCommand(async () => await ImportFormulasAsync());
            
            // UltraThink v2.0: 删除过度设计功能的命令初始化
        }

        private bool CanExecuteFormulaCommand(FormulaDto formula)
        {
            return formula != null && !IsLoading;
        }

        #endregion

        // UltraThink v2.0: 删除分类初始化功能 - 20人以下小诊所不需要复杂的分类筛选
        // 直接使用基类的数据加载机制，无需复杂的分类和初始化逻辑

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<FormulaDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 转换为FormulaQueryDto进行验方查询，删除分类筛选
            var formulaQuery = new FormulaQueryDto
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                Keyword = request.Keyword,
                SortField = request.SortField,
                IsDescending = request.IsDescending
            };
            return await _formulaService.GetPagedAsync(formulaQuery);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion
        
        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<FormulaDto>数据

        #region CRUD Operations

        private async Task AddFormulaAsync()
        {
            try
            {
                // TODO: 实现验方创建对话框
                await _dialogService.ShowInformationAsync("新增验方功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "添加验方失败");
                ShowError($"添加验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加验方失败: {ex.Message}", "错误");
            }
        }

        private async Task EditFormulaAsync(FormulaDto formula)
        {
            if (formula == null) return;
            
            try
            {
                // TODO: 实现验方编辑对话框
                await _dialogService.ShowInformationAsync($"编辑验方 {formula.Name} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑验方失败: {FormulaId}", formula.Id);
                ShowError($"编辑验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑验方失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteFormulaAsync(FormulaDto formula)
        {
            if (formula == null) return;
            
            // 验方信息不支持真正删除，只能禁用
            await ToggleStatusAsync(formula);
        }

        // UltraThink v2.0: 删除CopyFormulaAsync - 复制功能过度设计，医生直接新建即可

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(FormulaDto formula)
        {
            if (formula == null) return;

            var isEnabled = formula.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}验方 {formula.Name} 吗？",
                $"{action}验方");

            if (confirm)
            {
                try
                {
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _formulaService.DisableAsync(formula.Id);
                    }
                    else
                    {
                        result = await _formulaService.EnableAsync(formula.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"验方{action}成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? $"验方{action}失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "切换验方状态失败: {FormulaId}", formula.Id);
                    ShowError($"验方{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"验方{action}失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task ViewDetailsAsync(FormulaDto formula)
        {
            if (formula == null) return;

            try
            {
                var result = await _formulaService.GetByIdAsync(formula.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var formulaDetail = result.Data;
                    var detailInfo = $"验方详情：\n\n" +
                                   $"名称: {formulaDetail.Name}\n" +
                                   $"分类: {formulaDetail.Category ?? "未分类"}\n" +
                                   $"状态: {(formulaDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                                   $"备注: {formulaDetail.Remark ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"验方详情 - {formulaDetail.Name}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取验方详情失败", 
                        "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "查看验方详情失败: {FormulaId}", formula.Id);
                ShowError($"查看验方详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看验方详情失败: {ex.Message}", "错误");
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchEnableAsync, BatchDisableAsync 等功能

        #region Import/Export Operations

        private async Task ImportFormulasAsync()
        {
            try
            {
                // TODO: 实现验方导入功能
                await _dialogService.ShowInformationAsync("验方导入功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "导入验方失败");
                ShowError($"导入验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"导入验方失败: {ex.Message}", "错误");
            }
        }

        private async Task ExportFormulasAsync()
        {
            try
            {
                // TODO: 实现验方导出功能
                await _dialogService.ShowInformationAsync("验方导出功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "导出验方失败");
                ShowError($"导出验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"导出验方失败: {ex.Message}", "错误");
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
    }
}