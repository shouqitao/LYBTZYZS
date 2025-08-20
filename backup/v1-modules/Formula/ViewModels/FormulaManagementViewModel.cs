using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Formulas;
using LYBT.Desktop.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
// UltraThink四层架构重构：使用新的三层架构组件实现验方管理

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

        private readonly FormulaService _formulaService;
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

        public DelegateCommand AddCommand { get; }
        public DelegateCommand<FormulaViewModel> EditCommand { get; }
        public DelegateCommand<FormulaViewModel> DeleteCommand { get; }
        public DelegateCommand<FormulaViewModel> CopyCommand { get; }
        public DelegateCommand<FormulaViewModel> ViewDetailsCommand { get; }
        public DelegateCommand<FormulaViewModel> ToggleStatusCommand { get; }
        public DelegateCommand BatchEnableCommand { get; }
        public DelegateCommand BatchDisableCommand { get; }
        public DelegateCommand ClearSelectionCommand { get; }
        public DelegateCommand SelectAllCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand ImportCommand { get; }

        #endregion

        #region Constructor

        public FormulaManagementViewModel(
            FormulaService formulaService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ILogger<FormulaManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(logger, paginationCoordinator, searchManager)
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
                Logger.LogError(ex, "加载验方分类失败");
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
            var formulaQuery = new FormulaPagedQueryDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword,
                Category = SelectedCategory == "全部" ? null : SelectedCategory
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
                // 转换为FormulaInfoClean
                var formulaInfo = _mapper.Map<FormulaInfoClean>(dto);
                
                // 创建FormulaViewModel
                var formulaViewModel = FormulaViewModel.Create(formulaInfo);
                
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
                Logger.LogError(ex, "添加验方失败");
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
                Logger.LogError(ex, "编辑验方失败: {FormulaId}", formulaViewModel.Id);
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
                Logger.LogError(ex, "复制验方失败: {FormulaId}", formulaViewModel.Id);
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
                    
                    ServiceResult<bool> result;
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
                    Logger.LogError(ex, "切换验方状态失败: {FormulaId}", formulaViewModel.Id);
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
                Logger.LogError(ex, "查看验方详情失败: {FormulaId}", formulaViewModel.Id);
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
                    var result = await _formulaService.BatchEnableAsync(ids);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功启用 {result.Data} 个验方", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "批量启用失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量启用验方失败");
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
                    var result = await _formulaService.BatchDisableAsync(ids);

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功禁用 {result.Data} 个验方", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "批量禁用失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量禁用验方失败");
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
                Logger.LogError(ex, "导入验方失败");
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
                Logger.LogError(ex, "导出验方失败");
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