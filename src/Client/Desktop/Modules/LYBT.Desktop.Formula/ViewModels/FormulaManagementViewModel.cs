using LYBT.Desktop.Formula.ViewModels.Components; // Issue #1787: 添加Component命名空间
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方管理视图模型 - UltraThink架构重构版本
    /// 基于UnifiedListViewModelBase实现配方管理功能
    /// </summary>
    public class FormulaManagementViewModel : UnifiedListViewModelBase<FormulaDto>
    {
        #region 服务依赖

        // Issue #1787: 使用CommandHandler替代直接Repository访问
        private readonly FormulaCommandHandler _commandHandler;

        // Issue #2080: 注入DataManager用于复制验方
        private readonly FormulaDataManager _dataManager;

        #endregion

        #region 构造函数

        public FormulaManagementViewModel(
            FormulaCommandHandler commandHandler, // Issue #1787: 注入CommandHandler
            FormulaDataManager dataManager, // Issue #2080: 注入DataManager
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1787: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // Issue #2080: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            PageTitle = "配方管理";
        }

        #endregion

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<FormulaDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                // Issue #1787: 使用CommandHandler分页查询
                var result = await _commandHandler.GetPagedAsync(page, pageSize, searchText);

                if (!result.success || result.data == null)
                {
                    Logger.LogError("加载配方数据失败：{ErrorMessage}", result.errorMessage);
                    throw new InvalidOperationException(result.errorMessage ?? "查询配方失败");
                }

                var pagedData = result.data;

                // 更新分页信息
                TotalCount = pagedData.TotalCount;
                CurrentPage = pagedData.CurrentPage;
                PageSize = pagedData.PageSize;

                return pagedData.Items;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载配方数据时发生异常");
                throw;  // 重新抛出异常，让ExecuteSafelyAsync统一处理
            }
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 执行添加操作
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            NavigateTo("ContentRegion", "FormulaDetailView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(FormulaDto item)
        {
            try
            {
                // Issue #1787: 使用CommandHandler删除
                var success = await _commandHandler.DeleteAsync(item.Id);

                if (success)
                {
                    await ShowSuccessMessageAsync($"配方 '{item.Name}' 删除成功");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync($"删除配方 {item.Name} 失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除配方时发生异常：{FormulaId}", item.Id);
                await ShowErrorMessageAsync($"删除配方 {item.Name} 时发生系统错误");
            }
        }

        /// <summary>
        /// 执行批量删除操作
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<FormulaDto> items)
        {
            try
            {
                var selectedIds = items.Select(f => f.Id).ToList();

                // 循环调用DeleteAsync（Repository暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in selectedIds)
                {
                    try
                    {
                        // Issue #1787: 使用CommandHandler删除
                        var success = await _commandHandler.DeleteAsync(id);
                        if (success)
                            successCount++;
                        else
                            errors.Add($"删除配方 {id} 失败");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"删除配方 {id} 异常: {ex.Message}");
                    }
                }

                if (successCount == selectedIds.Count)
                {
                    await ShowSuccessMessageAsync($"成功删除 {items.Count} 个配方");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync($"批量删除完成，成功 {successCount} 个，失败 {errors.Count} 个");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除配方时发生异常");
                await ShowErrorMessageAsync("批量删除配方时发生系统错误");
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadPageAsync();
        }

        #endregion

        #region 暴露基类命令

        /// <summary>
        /// 搜索命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand SearchCommand => base.SearchCommand;

        /// <summary>
        /// 刷新命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand RefreshCommand => base.RefreshCommand;

        /// <summary>
        /// 添加命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand AddCommand => base.AddCommand;

        /// <summary>
        /// 删除命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand<FormulaDto> DeleteCommand => base.DeleteCommand;

        /// <summary>
        /// 上一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;

        /// <summary>
        /// 下一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand NextPageCommand => base.NextPageCommand;

        /// <summary>
        /// 添加配方命令 - 别名指向 AddCommand
        /// </summary>
        public DelegateCommand AddFormulaCommand => AddCommand;

        /// <summary>
        /// 查看详情命令 - 别名指向 ViewDetailCommand
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailsCommand => ViewDetailCommand;

        #endregion

        #region 自定义功能

        private DelegateCommand<FormulaDto>? _viewDetailCommand;
        
        /// <summary>
        /// 查看配方详情
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailCommand => 
            _viewDetailCommand ??= new DelegateCommand<FormulaDto>(ViewFormulaDetail, CanViewDetail);

        private DelegateCommand<FormulaDto>? _editCommand;
        
        /// <summary>
        /// 编辑配方
        /// </summary>
        public DelegateCommand<FormulaDto> EditCommand => 
            _editCommand ??= new DelegateCommand<FormulaDto>(EditFormula, CanEditFormula);

        private DelegateCommand<FormulaDto>? _copyCommand;
        
        /// <summary>
        /// 复制配方
        /// </summary>
        public DelegateCommand<FormulaDto> CopyCommand => 
            _copyCommand ??= new DelegateCommand<FormulaDto>(CopyFormula, CanCopyFormula);

        /// <summary>
        /// 查看配方详情
        /// </summary>
        private void ViewFormulaDetail(FormulaDto formula)
        {
            try
            {
                Logger.LogInformation("开始查看验方详情，ID: {FormulaId}, Name: {FormulaName}", formula?.Id, formula?.Name);
                
                if (formula == null)
                {
                    Logger.LogWarning("验方对象为null，无法查看详情");
                    return;
                }

                var parameters = new NavigationParameters
                {
                    { "FormulaId", formula.Id },
                    { "ReadOnly", true }
                };
                
                Logger.LogInformation("准备导航到FormulaDetailView，参数: FormulaId={FormulaId}", formula.Id);
                NavigateTo("ContentRegion", "FormulaDetailView", parameters);
                Logger.LogInformation("导航命令已发送");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看验方详情失败");
                HandleError(ex, "查看验方详情");
            }
        }

        /// <summary>
        /// 编辑配方
        /// </summary>
        private void EditFormula(FormulaDto formula)
        {
            if (formula == null) return;

            var parameters = new NavigationParameters
            {
                { "FormulaId", formula.Id },
                { "ReadOnly", false } // 明确指定编辑模式
            };
            NavigateTo("ContentRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 复制配方（Issue #2080: 使用DataManager.CreateFormulaCopy）
        /// </summary>
        private void CopyFormula(FormulaDto formula)
        {
            if (formula == null)
            {
                Logger.LogWarning("复制配方失败：formula参数为null");
                return;
            }

            try
            {
                Logger.LogInformation("开始复制验方: {FormulaId}, Name: {FormulaName}", formula.Id, formula.Name);

                // 1. 获取当前用户名
                var currentUserName = SessionManager?.CurrentUser?.UserName ?? "Unknown";
                Logger.LogDebug("当前用户: {UserName}", currentUserName);

                // 2. 调用DataManager创建副本
                var copiedFormula = _dataManager.CreateFormulaCopy(formula, currentUserName);
                Logger.LogInformation("验方副本创建成功，包含 {HerbCount} 个药材", copiedFormula.Herbs?.Count ?? 0);

                // 3. 导航至详情页，传递IsCopy=true参数
                var parameters = new NavigationParameters
                {
                    { "Formula", copiedFormula },
                    { "IsCopy", true }
                };

                Logger.LogInformation("准备导航到FormulaDetailView（复制模式），FormulaName: {FormulaName}", copiedFormula.Name);
                NavigateTo("ContentRegion", "FormulaDetailView", parameters);
                Logger.LogInformation("导航命令已发送");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制验方时发生异常: {FormulaId}", formula.Id);
                HandleError(ex, "复制验方");
            }
        }

        /// <summary>
        /// 检查是否可以查看详情
        /// </summary>
        private bool CanViewDetail(FormulaDto formula)
        {
            var canView = formula != null && !IsBusy;
            Logger.LogDebug("CanViewDetail: formula={FormulaId}, IsBusy={IsBusy}, Result={CanView}", 
                formula?.Id, IsBusy, canView);
            return canView;
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEditFormula(FormulaDto formula)
        {
            return formula != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以复制
        /// </summary>
        private bool CanCopyFormula(FormulaDto formula)
        {
            return formula != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true;
        }

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand =>
            new DelegateCommand(ExecuteFirstPage, () => CanGoPreviousPage && !IsLoading);

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand =>
            new DelegateCommand(ExecuteLastPage, () => CanGoNextPage && !IsLoading);

        /// <summary>
        /// 导入配方命令
        /// </summary>
        public DelegateCommand ImportFormulasCommand =>
            new DelegateCommand(async () => await ExecuteImportFormulasAsync(), () => !IsLoading);

        /// <summary>
        /// 导出模板命令
        /// </summary>
        public DelegateCommand ExportTemplateCommand =>
            new DelegateCommand(async () => await ExecuteExportTemplateAsync(), () => !IsLoading);

        /// <summary>
        /// 导出配方命令
        /// </summary>
        public DelegateCommand ExportFormulasCommand =>
            new DelegateCommand(async () => await ExecuteExportFormulasAsync(), () => Items.Count > 0 && !IsLoading);

        /// <summary>
        /// 清除筛选命令
        /// </summary>
        public DelegateCommand ClearFiltersCommand =>
            new DelegateCommand(ExecuteClearFilters, () => !string.IsNullOrEmpty(SearchText));

        /// <summary>
        /// 跳转首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        /// <summary>
        /// 导入配方
        /// </summary>
        private async Task ExecuteImportFormulasAsync()
        {
            try
            {
                // TODO: 实现导入逻辑
                await ShowSuccessMessageAsync("导入配方功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入配方时发生异常");
                await ShowErrorMessageAsync("导入配方时发生系统错误");
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private async Task ExecuteExportTemplateAsync()
        {
            try
            {
                // TODO: 实现导出模板逻辑
                await ShowSuccessMessageAsync("导出模板功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出模板时发生异常");
                await ShowErrorMessageAsync("导出模板时发生系统错误");
            }
        }

        /// <summary>
        /// 导出配方
        /// </summary>
        private async Task ExecuteExportFormulasAsync()
        {
            try
            {
                // TODO: 实现导出逻辑
                await ShowSuccessMessageAsync("导出配方功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出配方时发生异常");
                await ShowErrorMessageAsync("导出配方时发生系统错误");
            }
        }

        /// <summary>
        /// 清除筛选条件
        /// </summary>
        private void ExecuteClearFilters()
        {
            SearchText = string.Empty;
        }

        #endregion

        #region 搜索功能增强

        /// <summary>
        /// 按分类搜索
        /// </summary>
        public DelegateCommand<string> SearchByCategoryCommand =>
            new DelegateCommand<string>(SearchByCategory);

        /// <summary>
        /// 按分类搜索
        /// </summary>
        private async void SearchByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            SearchText = $"分类:{category}";
            await LoadPageAsync();
        }

        #endregion

        #region 命令刷新

        /// <summary>
        /// 刷新所有命令的可执行状态
        /// </summary>
        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();

            ViewDetailCommand?.RaiseCanExecuteChanged();
            EditCommand?.RaiseCanExecuteChanged();
            CopyCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged();
            FirstPageCommand?.RaiseCanExecuteChanged();
            LastPageCommand?.RaiseCanExecuteChanged();
            ImportFormulasCommand?.RaiseCanExecuteChanged();
            ExportTemplateCommand?.RaiseCanExecuteChanged();
            ExportFormulasCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            SearchByCategoryCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
