using System;
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
    /// 中药材管理视图模型（UltraThink架构重构版）
    /// 使用新的三层架构：PaginationCoordinator + SearchManager + NewBaseListViewModel
    /// 实现完全的关注点分离和单一职责原则
    /// </summary>
    public class HerbManagementViewModel : NewBaseListViewModel<HerbDto>
    {
        #region Fields

        private readonly HerbModuleService _herbService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        private ObservableCollection<HerbViewModel> _herbViewModels = new();
        private HerbViewModel? _selectedHerbViewModel;

        #endregion

        #region Properties

        /// <summary>药材视图模型集合 - 替代原始的HerbInfo集合</summary>
        public ObservableCollection<HerbViewModel> HerbViewModels
        {
            get => _herbViewModels;
            set => SetProperty(ref _herbViewModels, value);
        }

        /// <summary>选中的药材视图模型</summary>
        public HerbViewModel? SelectedHerbViewModel
        {
            get => _selectedHerbViewModel;
            set
            {
                if (SetProperty(ref _selectedHerbViewModel, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>批量选中的药材数量</summary>
        public int SelectedHerbsCount => HerbViewModels.Count(h => h.IsSelected);

        /// <summary>是否有选中的药材</summary>
        public bool HasSelectedHerbs => SelectedHerbsCount > 0;

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<HerbViewModel> EditCommand { get; private set; }
        public DelegateCommand<HerbViewModel> DeleteCommand { get; private set; }
        public DelegateCommand<HerbViewModel> ToggleStatusCommand { get; private set; }
        public DelegateCommand<HerbViewModel> ViewDetailsCommand { get; private set; }
        public DelegateCommand BatchEnableCommand { get; private set; }
        public DelegateCommand BatchDisableCommand { get; private set; }
        public DelegateCommand ClearSelectionCommand { get; private set; }
        public DelegateCommand SelectAllCommand { get; private set; }

        #endregion

        #region Constructor

        public HerbManagementViewModel(
            HerbModuleService herbService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<HerbManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // 监听选择状态变化
            HerbViewModels.CollectionChanged += (s, e) => UpdateSelectionProperties();
            
            // 初始化加载数据
            _ = RefreshDataAsync();
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddHerbAsync());
            EditCommand = new DelegateCommand<HerbViewModel>(async herb => await EditHerbAsync(herb), CanExecuteHerbCommand);
            DeleteCommand = new DelegateCommand<HerbViewModel>(async herb => await DeleteHerbAsync(herb), CanExecuteHerbCommand);
            ToggleStatusCommand = new DelegateCommand<HerbViewModel>(async herb => await ToggleStatusAsync(herb), CanExecuteHerbCommand);
            ViewDetailsCommand = new DelegateCommand<HerbViewModel>(async herb => await ViewDetailsAsync(herb), CanExecuteHerbCommand);
            
            BatchEnableCommand = new DelegateCommand(async () => await BatchEnableAsync(), () => HasSelectedHerbs);
            BatchDisableCommand = new DelegateCommand(async () => await BatchDisableAsync(), () => HasSelectedHerbs);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedHerbs);
            SelectAllCommand = new DelegateCommand(SelectAll);
        }

        private bool CanExecuteHerbCommand(HerbViewModel herb)
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

        protected override void OnDataLoaded(PagedResult<HerbDto> data)
        {
            base.OnDataLoaded(data);
            
            // 将HerbDto转换为HerbViewModel
            UpdateHerbViewModels(data.Items);
        }

        protected override void OnDataLoadFailed(string errorMessage)
        {
            base.OnDataLoadFailed(errorMessage);
            
            // 清空药材视图模型
            HerbViewModels.Clear();
            UpdateSelectionProperties();
            
            // 显示错误
            _ = _dialogService.ShowErrorAsync(errorMessage, "加载失败");
        }

        #endregion
        
        #region Herb ViewModels Management

        private void UpdateHerbViewModels(System.Collections.Generic.List<HerbDto> herbDtos)
        {
            // 保存当前选择状态
            var selectedIds = HerbViewModels.Where(h => h.IsSelected).Select(h => h.Id).ToHashSet();
            
            // 清空并重新创建
            HerbViewModels.Clear();
            
            foreach (var dto in herbDtos)
            {
                // UltraThink v2.0: 直接使用DTO创建HerbViewModel
                var herbViewModel = HerbViewModel.Create(dto);
                
                // 恢复选择状态
                if (selectedIds.Contains(herbViewModel.Id))
                {
                    herbViewModel.IsSelected = true;
                }
                
                // 监听选择状态变化
                herbViewModel.State.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(HerbStateViewModel.IsSelected))
                    {
                        UpdateSelectionProperties();
                    }
                };
                
                HerbViewModels.Add(herbViewModel);
            }
            
            UpdateSelectionProperties();
        }

        private void UpdateSelectionProperties()
        {
            RaisePropertyChanged(nameof(SelectedHerbsCount));
            RaisePropertyChanged(nameof(HasSelectedHerbs));
            
            BatchEnableCommand.RaiseCanExecuteChanged();
            BatchDisableCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region CRUD Operations

        private async Task AddHerbAsync()
        {
            try
            {
                // TODO: 实现药材创建对话框
                await _dialogService.ShowInformationAsync("新增药材功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "添加药材失败");
                ShowError($"添加药材失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加药材失败: {ex.Message}", "错误");
            }
        }

        private async Task EditHerbAsync(HerbViewModel herbViewModel)
        {
            if (herbViewModel == null) return;
            
            try
            {
                // TODO: 实现药材编辑对话框
                await _dialogService.ShowInformationAsync($"编辑药材 {herbViewModel.DisplayName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑药材失败: {HerbId}", herbViewModel.Id);
                ShowError($"编辑药材失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑药材失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteHerbAsync(HerbViewModel herbViewModel)
        {
            if (herbViewModel == null) return;
            
            // 药材信息不支持真正删除，只能禁用
            await ToggleStatusAsync(herbViewModel);
        }

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(HerbViewModel herbViewModel)
        {
            if (herbViewModel == null) return;

            var isEnabled = herbViewModel.HerbData.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}药材 {herbViewModel.DisplayName} 吗？",
                $"{action}药材");

            if (confirm)
            {
                try
                {
                    herbViewModel.IsLoading = true;
                    
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _herbService.DisableAsync(herbViewModel.Id);
                    }
                    else
                    {
                        result = await _herbService.EnableAsync(herbViewModel.Id);
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
                    LogError(ex, "切换药材状态失败: {HerbId}", herbViewModel.Id);
                    ShowError($"药材{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"药材{action}失败: {ex.Message}", "错误");
                }
                finally
                {
                    herbViewModel.IsLoading = false;
                }
            }
        }

        private async Task ViewDetailsAsync(HerbViewModel herbViewModel)
        {
            if (herbViewModel == null) return;

            try
            {
                herbViewModel.IsLoading = true;
                
                var result = await _herbService.GetByIdAsync(herbViewModel.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var herb = result.Data;
                    var detailInfo = $"药材详情：\n\n" +
                                   $"名称: {herb.Name}\n" +
                                   $"产地: {herb.Origin ?? "未知"}\n" +
                                   $"规格: {herb.Spec ?? "未知"}\n" +
                                   $"单价: ¥{herb.Price:F2}/{herb.Unit}\n" +
                                   $"功效: {herb.Effect ?? "未录入"}\n" +
                                   $"用法: {herb.Usage ?? "未录入"}\n" +
                                   $"状态: {(herb.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                                   $"备注: {herb.Remark ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"药材详情 - {herb.Name}");
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
                LogError(ex, "查看药材详情失败: {HerbId}", herbViewModel.Id);
                ShowError($"查看药材详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看药材详情失败: {ex.Message}", "错误");
            }
            finally
            {
                herbViewModel.IsLoading = false;
            }
        }

        #endregion

        #region Batch Operations

        private async Task BatchEnableAsync()
        {
            var selectedHerbs = HerbViewModels.Where(h => h.IsSelected).ToList();
            if (!selectedHerbs.Any()) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要启用选中的 {selectedHerbs.Count} 个药材吗？",
                "批量启用");

            if (confirm)
            {
                try
                {
                    var ids = selectedHerbs.Select(h => h.Id).ToList();
                    var result = await _herbService.BatchUpdateStatusAsync(ids, true, "批量启用");

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功启用 {result.Data} 个药材", "成功");
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
                    LogError(ex, "批量启用药材失败");
                    ShowError($"批量启用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量启用失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchDisableAsync()
        {
            var selectedHerbs = HerbViewModels.Where(h => h.IsSelected).ToList();
            if (!selectedHerbs.Any())
            {
                await _dialogService.ShowWarningAsync("没有选中的药材", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要禁用选中的 {selectedHerbs.Count} 个药材吗？",
                "批量禁用");

            if (confirm)
            {
                try
                {
                    var ids = selectedHerbs.Select(h => h.Id).ToList();
                    var result = await _herbService.BatchUpdateStatusAsync(ids, false, "批量禁用");

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"已成功禁用 {result.Data} 个药材", "成功");
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
                    LogError(ex, "批量禁用药材失败");
                    ShowError($"批量禁用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量禁用失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        #region Selection Management

        private void ClearSelection()
        {
            foreach (var herb in HerbViewModels)
            {
                herb.IsSelected = false;
            }
        }

        private void SelectAll()
        {
            foreach (var herb in HerbViewModels)
            {
                herb.IsSelected = true;
            }
        }

        #endregion
    }
}