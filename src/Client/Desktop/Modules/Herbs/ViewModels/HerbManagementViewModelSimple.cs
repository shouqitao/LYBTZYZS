using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AutoMapper;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Prism.Commands;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Herbs.Services.Interfaces;
using LYBT.Desktop.Core.Models.Common;
using Prism.Mvvm;
// UltraThink四层架构重构：使用模块化服务，消除对SharedServices的依赖

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材管理视图模型（UltraThink架构重构版）
    /// UltraThink模块化架构：使用IHerbModuleService，实现模块自包含
    /// </summary>
    public class HerbManagementViewModelSimple : BindableBase
    {
        private readonly IHerbModuleService _herbModuleService;
        private readonly ICustomDialogService _commonDialogService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        #region 属性
        
        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        private ObservableCollection<HerbInfo> _herbs = new();
        public ObservableCollection<HerbInfo> Herbs
        {
            get => _herbs;
            set => SetProperty(ref _herbs, value);
        }
        
        private HerbInfo? _selectedHerb;
        public HerbInfo? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    BatchUpdateStatusCommand.RaiseCanExecuteChanged();
                }
            }
        }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }
        
        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadDataAsync();
                }
            }
        }
        
        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }
        
        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }
        
        #endregion

        #region Commands
        
        public DelegateCommand LoadCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand<HerbInfo> EditCommand { get; }
        public DelegateCommand<HerbInfo> DeleteCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<HerbInfo> ToggleStatusCommand { get; }
        public DelegateCommand<HerbInfo> BatchUpdateStatusCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        
        #endregion

        public HerbManagementViewModelSimple(
            IHerbModuleService herbModuleService,
            ICustomDialogService commonDialogService,
            ICustomDialogService dialogService,
            IMapper mapper)
        {
            _herbModuleService = herbModuleService ?? throw new ArgumentNullException(nameof(herbModuleService));
            _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadCommand = new DelegateCommand(async () => await LoadDataAsync());
            AddCommand = new DelegateCommand(async () => await AddAsync());
            EditCommand = new DelegateCommand<HerbInfo>(async herb => await EditAsync(herb), herb => herb != null);
            DeleteCommand = new DelegateCommand<HerbInfo>(async herb => await DeleteAsync(herb), herb => herb != null);
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ToggleStatusCommand = new DelegateCommand<HerbInfo>(async herb => await ToggleStatusAsync(herb), herb => herb != null);
            BatchUpdateStatusCommand = new DelegateCommand<HerbInfo>(async herb => await BatchUpdateStatusAsync(herb), herb => herb != null);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);
            
            // 初始化加载数据
            _ = LoadDataAsync();
        }

        #region 数据操作方法
        
        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize,
                    Keyword = SearchKeyword
                };
                
                var result = await _herbModuleService.GetPagedAsync(query);
                if (result.IsSuccess)
                {
                    Herbs.Clear();
                    foreach (var herb in result.Data.Items)
                    {
                        Herbs.Add(herb);
                    }
                    
                    TotalCount = result.Data.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                    
                    // 更新分页命令状态
                    PreviousPageCommand.RaiseCanExecuteChanged();
                    NextPageCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "加载中药材列表失败", "错误");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载中药材列表异常: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 搜索中药材
        /// </summary>
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }
        
        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }
        
        /// <summary>
        /// 上一页
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }
        
        /// <summary>
        /// 下一页
        /// </summary>
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }
        
        #endregion
        
        #region CRUD操作
        
        /// <summary>
        /// 新增中药材
        /// </summary>
        private async Task AddAsync()
        {
            try
            {
                var createInfo = new HerbCreateInfo();
                
                // 这里可以打开对话框进行中药材创建
                // 暂时使用简单的实现
                await _commonDialogService.ShowInformationAsync("新增中药材功能开发中", "提示");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"添加中药材失败: {ex.Message}", "错误");
            }
        }
        
        /// <summary>
        /// 编辑中药材
        /// </summary>
        private async Task EditAsync(HerbInfo herb)
        {
            if (herb == null) return;
            
            try
            {
                var updateInfo = HerbUpdateInfo.FromHerbInfo(herb);
                
                // 这里可以打开对话框进行中药材编辑
                // 暂时使用简单的实现
                await _commonDialogService.ShowInformationAsync($"编辑中药材 {herb.Name} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"编辑中药材失败: {ex.Message}", "错误");
            }
        }
        
        /// <summary>
        /// 删除中药材
        /// </summary>
        private async Task DeleteAsync(HerbInfo herb)
        {
            if (herb == null) return;
            
            // 中药材不支持删除，只能禁用
            await ToggleStatusAsync(herb);
        }
        
        #endregion

        #region 业务操作方法

        /// <summary>
        /// 切换中药材状态
        /// </summary>
        private async Task ToggleStatusAsync(HerbInfo herb)
        {
            if (herb == null) return;

            var action = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action}中药材 {herb.Name} 吗？",
                $"{action}中药材");

            if (confirm)
            {
                ServiceResult result;
                if (herb.Status == CommonStatus.Enabled)
                {
                    result = await _herbModuleService.DisableAsync(herb.Id);
                }
                else
                {
                    result = await _herbModuleService.EnableAsync(herb.Id);
                }

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"中药材{action}成功", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"中药材{action}失败",
                        "错误");
                }
            }
        }

        /// <summary>
        /// 批量更新中药材状态
        /// </summary>
        private async Task BatchUpdateStatusAsync(HerbInfo herb)
        {
            if (herb == null) return;
            
            // 获取选中的中药材列表
            var selectedHerbs = Herbs.Where(h => h.IsSelected).ToList();
            
            if (!selectedHerbs.Any())
            {
                // 如果没有选中项，则对当前项执行操作
                selectedHerbs.Add(herb);
            }
            
            var action = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _commonDialogService.ShowConfirmationAsync(
                $"确定要{action} {selectedHerbs.Count} 个中药材吗？",
                $"批量{action}中药材");

            if (confirm)
            {
                var ids = selectedHerbs.Select(h => h.Id);
                var isEnabled = herb.Status != CommonStatus.Enabled;
                
                var result = await _herbModuleService.BatchUpdateStatusAsync(ids, isEnabled, $"批量{action}操作");

                if (result.IsSuccess)
                {
                    await RefreshAsync();
                    await _commonDialogService.ShowInformationAsync($"批量{action}成功，共处理 {result.Data} 条记录", "成功");
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"批量{action}失败",
                        "错误");
                }
            }
        }

        #endregion
    }
}