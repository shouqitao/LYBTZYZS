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
    /// 验方管理视图模型（UltraThink架构重构版）
    /// 使用新的三层架构：PaginationCoordinator + SearchManager + NewBaseListViewModel
    /// 实现完全的关注点分离和单一职责原则
    /// </summary>
    public class FormulaManagementViewModel : NewBaseListViewModel<FormulaDto>
    {
        #region Fields

        private readonly FormulaModuleService _formulaService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        private ObservableCollection<FormulaViewModel> _formulaViewModels = new();
        private FormulaViewModel? _selectedFormulaViewModel;
        private ObservableCollection<string> _categories = new();
        private string _selectedCategory = "全部";

        #endregion

        #region Properties

        /// <summary>验方视图模型集合 - 替代原始的FormulaInfo集合</summary>
        public ObservableCollection<FormulaViewModel> FormulaViewModels
        {
            get => _formulaViewModels;
            set => SetProperty(ref _formulaViewModels, value);
        }

        /// <summary>选中的验方视图模型</summary>
        public FormulaViewModel? SelectedFormulaViewModel
        {
            get => _selectedFormulaViewModel;
            set
            {
                if (SetProperty(ref _selectedFormulaViewModel, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    CopyCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>分类列表</summary>
        public ObservableCollection<string> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        /// <summary>选中的分类</summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    // 分类变更时重新加载数据
                    _ = RefreshDataAsync();
                }
            }
        }

        /// <summary>批量选中的验方数量</summary>
        public int SelectedFormulasCount => FormulaViewModels.Count(f => f.IsSelected);

        /// <summary>是否有选中的验方</summary>
        public bool HasSelectedFormulas => SelectedFormulasCount > 0;

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<FormulaViewModel> EditCommand { get; private set; }
        public DelegateCommand<FormulaViewModel> DeleteCommand { get; private set; }
        public DelegateCommand<FormulaViewModel> CopyCommand { get; private set; }
        public DelegateCommand<FormulaViewModel> ViewDetailsCommand { get; private set; }
        public DelegateCommand<FormulaViewModel> ToggleStatusCommand { get; private set; }
        public DelegateCommand BatchEnableCommand { get; private set; }
        public DelegateCommand BatchDisableCommand { get; private set; }
        public DelegateCommand ClearSelectionCommand { get; private set; }
        public DelegateCommand SelectAllCommand { get; private set; }
        public DelegateCommand ExportCommand { get; private set; }
        public DelegateCommand ImportCommand { get; private set; }

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
            
            // 监听选择状态变化
            FormulaViewModels.CollectionChanged += (s, e) => UpdateSelectionProperties();
            
            // 初始化分类和数据
            _ = InitializeAsync();
        }

        #endregion

        #region Command Initialization

        private void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddFormulaAsync());
            EditCommand = new DelegateCommand<FormulaViewModel>(async formula => await EditFormulaAsync(formula), CanExecuteFormulaCommand);
            DeleteCommand = new DelegateCommand<FormulaViewModel>(async formula => await DeleteFormulaAsync(formula), CanExecuteFormulaCommand);
            CopyCommand = new DelegateCommand<FormulaViewModel>(async formula => await CopyFormulaAsync(formula), CanExecuteFormulaCommand);
            ViewDetailsCommand = new DelegateCommand<FormulaViewModel>(async formula => await ViewDetailsAsync(formula), CanExecuteFormulaCommand);
            ToggleStatusCommand = new DelegateCommand<FormulaViewModel>(async formula => await ToggleStatusAsync(formula), CanExecuteFormulaCommand);
            
            BatchEnableCommand = new DelegateCommand(async () => await BatchEnableAsync(), () => HasSelectedFormulas);
            BatchDisableCommand = new DelegateCommand(async () => await BatchDisableAsync(), () => HasSelectedFormulas);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedFormulas);
            SelectAllCommand = new DelegateCommand(SelectAll);
            
            ExportCommand = new DelegateCommand(async () => await ExportFormulasAsync());
            ImportCommand = new DelegateCommand(async () => await ImportFormulasAsync());
        }

        private bool CanExecuteFormulaCommand(FormulaViewModel formula)
        {
            return formula != null && !IsLoading;
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await RefreshDataAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var result = await _formulaService.GetCategoriesAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    Categories.Clear();
                    Categories.Add("全部");
                    foreach (var category in result.Data)
                    {
                        Categories.Add(category);
                    }
                    SelectedCategory = "全部";
                }
                else
                {
                    // 使用默认分类
                    InitializeDefaultCategories();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载验方分类失败");
                ShowError("加载验方分类失败");
                InitializeDefaultCategories();
            }
        }

        private void InitializeDefaultCategories()
        {
            Categories.Clear();
            Categories.Add("全部");
            Categories.Add("内科方");
            Categories.Add("外科方");
            Categories.Add("妇科方");
            Categories.Add("儿科方");
            Categories.Add("经典方");
            Categories.Add("验方");
            SelectedCategory = "全部";
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<FormulaDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // 转换为验方查询DTO，包含分类筛选
            var formulaQuery = new FormulaQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword
                // UltraThink v2.0: FormulaQueryDto不支持Category筛选，分类在前端通过计算属性处理
            };

            return await _formulaService.GetPagedAsync(formulaQuery);
        }

        protected override void OnDataLoaded(PagedResult<FormulaDto> data)
        {
            base.OnDataLoaded(data);
            
            // 将FormulaDto转换为FormulaViewModel
            UpdateFormulaViewModels(data.Items);
        }

        protected override void OnDataLoadFailed(string errorMessage)
        {
            base.OnDataLoadFailed(errorMessage);
            
            // 清空验方视图模型
            FormulaViewModels.Clear();
            UpdateSelectionProperties();
            
            // 显示错误
            _ = _dialogService.ShowErrorAsync(errorMessage, "加载失败");
        }

        #endregion
        
        #region Formula ViewModels Management

        private void UpdateFormulaViewModels(System.Collections.Generic.List<FormulaDto> formulaDtos)
        {
            // 保存当前选择状态
            var selectedIds = FormulaViewModels.Where(f => f.IsSelected).Select(f => f.Id).ToHashSet();
            
            // 清空并重新创建
            FormulaViewModels.Clear();
            
            foreach (var dto in formulaDtos)
            {
                // UltraThink v2.0: 直接使用DTO创建FormulaViewModel
                var formulaViewModel = FormulaViewModel.Create(dto);
                
                // 恢复选择状态
                if (selectedIds.Contains(formulaViewModel.Id))
                {
                    formulaViewModel.IsSelected = true;
                }
                
                // 监听选择状态变化
                formulaViewModel.State.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FormulaStateViewModel.IsSelected))
                    {
                        UpdateSelectionProperties();
                    }
                };
                
                FormulaViewModels.Add(formulaViewModel);
            }
            
            UpdateSelectionProperties();
        }

        private void UpdateSelectionProperties()
        {
            RaisePropertyChanged(nameof(SelectedFormulasCount));
            RaisePropertyChanged(nameof(HasSelectedFormulas));
            
            BatchEnableCommand.RaiseCanExecuteChanged();
            BatchDisableCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        #endregion

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

        private async Task EditFormulaAsync(FormulaViewModel formulaViewModel)
        {
            if (formulaViewModel == null) return;
            
            try
            {
                // TODO: 实现验方编辑对话框
                await _dialogService.ShowInformationAsync($"编辑验方 {formulaViewModel.DisplayName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑验方失败: {FormulaId}", formulaViewModel.Id);
                ShowError($"编辑验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑验方失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteFormulaAsync(FormulaViewModel formulaViewModel)
        {
            if (formulaViewModel == null) return;
            
            // 验方信息不支持真正删除，只能禁用
            await ToggleStatusAsync(formulaViewModel);
        }

        private async Task CopyFormulaAsync(FormulaViewModel formulaViewModel)
        {
            if (formulaViewModel == null) return;
            
            try
            {
                // TODO: 实现验方复制功能
                await _dialogService.ShowInformationAsync($"复制验方 {formulaViewModel.DisplayName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "复制验方失败: {FormulaId}", formulaViewModel.Id);
                ShowError($"复制验方失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"复制验方失败: {ex.Message}", "错误");
            }
        }

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(FormulaViewModel formulaViewModel)
        {
            if (formulaViewModel == null) return;

            var isEnabled = formulaViewModel.FormulaData.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}验方 {formulaViewModel.DisplayName} 吗？",
                $"{action}验方");

            if (confirm)
            {
                try
                {
                    formulaViewModel.IsLoading = true;
                    
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _formulaService.DisableAsync(formulaViewModel.Id);
                    }
                    else
                    {
                        result = await _formulaService.EnableAsync(formulaViewModel.Id);
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
                    LogError(ex, "切换验方状态失败: {FormulaId}", formulaViewModel.Id);
                    ShowError($"验方{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"验方{action}失败: {ex.Message}", "错误");
                }
                finally
                {
                    formulaViewModel.IsLoading = false;
                }
            }
        }

        private async Task ViewDetailsAsync(FormulaViewModel formulaViewModel)
        {
            if (formulaViewModel == null) return;

            try
            {
                formulaViewModel.IsLoading = true;
                
                var result = await _formulaService.GetByIdAsync(formulaViewModel.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var formula = result.Data;
                    var detailInfo = formulaViewModel.Display.GetDetailedInfo();

                    await _dialogService.ShowInformationAsync(detailInfo, $"验方详情 - {formula.Name}");
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
                LogError(ex, "查看验方详情失败: {FormulaId}", formulaViewModel.Id);
                ShowError($"查看验方详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看验方详情失败: {ex.Message}", "错误");
            }
            finally
            {
                formulaViewModel.IsLoading = false;
            }
        }

        #endregion

        #region Batch Operations

        private async Task BatchEnableAsync()
        {
            var selectedFormulas = FormulaViewModels.Where(f => f.IsSelected).ToList();
            if (!selectedFormulas.Any()) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要启用选中的 {selectedFormulas.Count} 个验方吗？",
                "批量启用");

            if (confirm)
            {
                try
                {
                    var ids = selectedFormulas.Select(f => f.Id).ToList();
                    
                    // UltraThink v2.0: 移除批量操作，改为逐个启用
                    int successCount = 0;
                    var errors = new List<string>();
                    
                    foreach (var id in ids)
                    {
                        var result = await _formulaService.EnableAsync(id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"验方 {id}: {result.ErrorMessage}");
                        }
                    }

                    await RefreshDataAsync();
                    
                    if (errors.Count == 0)
                    {
                        await _dialogService.ShowInformationAsync($"已成功启用 {successCount} 个验方", "成功");
                    }
                    else
                    {
                        var errorMsg = $"启用完成，成功 {successCount} 个，失败 {errors.Count} 个";
                        if (errors.Count <= 3)
                        {
                            errorMsg += "\n失败详情:\n" + string.Join("\n", errors);
                        }
                        await _dialogService.ShowWarningAsync(errorMsg, "部分成功");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "批量启用验方失败");
                    ShowError($"批量启用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量启用失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchDisableAsync()
        {
            var selectedFormulas = FormulaViewModels.Where(f => f.IsSelected).ToList();
            if (!selectedFormulas.Any())
            {
                await _dialogService.ShowWarningAsync("没有选中的验方", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要禁用选中的 {selectedFormulas.Count} 个验方吗？",
                "批量禁用");

            if (confirm)
            {
                try
                {
                    var ids = selectedFormulas.Select(f => f.Id).ToList();
                    
                    // UltraThink v2.0: 移除批量操作，改为逐个禁用
                    int successCount = 0;
                    var errors = new List<string>();
                    
                    foreach (var id in ids)
                    {
                        var result = await _formulaService.DisableAsync(id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"验方 {id}: {result.ErrorMessage}");
                        }
                    }

                    await RefreshDataAsync();
                    
                    if (errors.Count == 0)
                    {
                        await _dialogService.ShowInformationAsync($"已成功禁用 {successCount} 个验方", "成功");
                    }
                    else
                    {
                        var errorMsg = $"禁用完成，成功 {successCount} 个，失败 {errors.Count} 个";
                        if (errors.Count <= 3)
                        {
                            errorMsg += "\n失败详情:\n" + string.Join("\n", errors);
                        }
                        await _dialogService.ShowWarningAsync(errorMsg, "部分成功");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "批量禁用验方失败");
                    ShowError($"批量禁用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量禁用失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

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

        #region Selection Management

        private void ClearSelection()
        {
            foreach (var formula in FormulaViewModels)
            {
                formula.IsSelected = false;
            }
        }

        private void SelectAll()
        {
            foreach (var formula in FormulaViewModels)
            {
                formula.IsSelected = true;
            }
        }

        #endregion
    }
}