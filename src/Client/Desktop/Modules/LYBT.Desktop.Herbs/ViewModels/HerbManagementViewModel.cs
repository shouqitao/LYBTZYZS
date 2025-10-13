using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 药材管理视图模型 - UltraThink架构重构版本
    /// 基于UnifiedListViewModelBase实现药材管理功能
    /// </summary>
    public class HerbManagementViewModel : UnifiedListViewModelBase<HerbDto>
    {
        #region 服务依赖

        private readonly IHerbRepository _herbRepository;

        #endregion

        #region 构造函数

        public HerbManagementViewModel(
            IHerbRepository herbRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));

            PageTitle = "药材管理";

            // 初始化自定义命令
            InitializeCustomCommands();
        }

        #endregion

        #region 命令初始化

        /// <summary>
        /// 初始化自定义命令
        /// </summary>
        private void InitializeCustomCommands()
        {
            ToggleStatusCommand = new DelegateCommand<HerbDto>(
                async (herb) => await ToggleStatusAsync(herb),
                herb => herb != null && !IsBusy
            );

            ImportHerbsCommand = new DelegateCommand(
                async () => await ImportHerbsAsync(),
                () => !IsBusy
            );

            ExportTemplateCommand = new DelegateCommand(
                async () => await ExportTemplateAsync(),
                () => !IsBusy
            );

            ExportHerbsCommand = new DelegateCommand(
                async () => await ExportHerbsAsync(),
                () => !IsBusy && Items.Count > 0
            );

            FirstPageCommand = new DelegateCommand(
                ExecuteFirstPage,
                () => CurrentPage > 1 && !IsBusy
            );

            LastPageCommand = new DelegateCommand(
                ExecuteLastPage,
                () => CurrentPage < TotalPages && !IsBusy
            );
        }

        #endregion

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<HerbDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                var pagedData = await _herbRepository.GetPagedAsync(page, pageSize, searchText);

                // 更新分页信息
                TotalCount = pagedData.TotalCount;
                CurrentPage = pagedData.CurrentPage;
                PageSize = pagedData.PageSize;

                return pagedData.Items;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材数据时发生异常");
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
            NavigateTo("MainRegion", "HerbDetailView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(HerbDto item)
        {
            try
            {
                var success = await _herbRepository.DeleteAsync(item.Id);

                if (success)
                {
                    await ShowSuccessMessageAsync($"药材 '{item.Name}' 删除成功");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync($"删除药材 {item.Name} 失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除药材时发生异常：{HerbId}", item.Id);
                await ShowErrorMessageAsync($"删除药材 {item.Name} 时发生系统错误");
            }
        }

        /// <summary>
        /// 执行批量删除操作
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<HerbDto> items)
        {
            try
            {
                var selectedIds = items.Select(h => h.Id).ToList();

                // 循环调用DeleteAsync（Repository暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in selectedIds)
                {
                    try
                    {
                        var success = await _herbRepository.DeleteAsync(id);
                        if (success)
                            successCount++;
                        else
                            errors.Add($"删除药材 {id} 失败");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"删除药材 {id} 异常: {ex.Message}");
                    }
                }

                if (successCount == selectedIds.Count)
                {
                    await ShowSuccessMessageAsync($"成功删除 {items.Count} 个药材");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync($"批量删除完成，成功 {successCount} 个，失败 {errors.Count} 个");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量删除药材时发生异常");
                await ShowErrorMessageAsync("批量删除药材时发生系统错误");
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

        /// &lt;summary&gt;
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
        public new DelegateCommand<HerbDto> DeleteCommand => base.DeleteCommand;

        /// <summary>
        /// 上一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;

        /// <summary>
        /// 下一页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand NextPageCommand => base.NextPageCommand;

        #endregion

        #region 自定义命令

        /// <summary>
        /// 编辑命令 - 别名指向 EditHerbCommand
        /// </summary>
        public DelegateCommand<HerbDto> EditCommand => EditHerbCommand;

        /// <summary>
        /// 切换状态命令
        /// </summary>
        public DelegateCommand<HerbDto> ToggleStatusCommand { get; private set; } = null!;

        /// <summary>
        /// 导入药材命令
        /// </summary>
        public DelegateCommand ImportHerbsCommand { get; private set; } = null!;

        /// <summary>
        /// 导出模板命令
        /// </summary>
        public DelegateCommand ExportTemplateCommand { get; private set; } = null!;

        /// <summary>
        /// 导出药材命令
        /// </summary>
        public DelegateCommand ExportHerbsCommand { get; private set; } = null!;

        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; } = null!;

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region 自定义功能

        /// <summary>
        /// 查看药材详情
        /// </summary>
        public DelegateCommand<HerbDto> ViewDetailCommand =>
            new DelegateCommand<HerbDto>(ViewHerbDetail, CanViewDetail);

        /// <summary>
        /// 编辑药材
        /// </summary>
        public DelegateCommand<HerbDto> EditHerbCommand =>
            new DelegateCommand<HerbDto>(EditHerb, CanEditHerb);

        /// <summary>
        /// 复制药材
        /// </summary>
        public DelegateCommand<HerbDto> CopyHerbCommand =>
            new DelegateCommand<HerbDto>(CopyHerb, CanCopyHerb);

        /// <summary>
        /// 查看药材详情
        /// </summary>
        private void ViewHerbDetail(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "HerbId", herb.Id },
                { "ReadOnly", true }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        private void EditHerb(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "HerbId", herb.Id }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 复制药材
        /// </summary>
        private void CopyHerb(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "SourceHerbId", herb.Id },
                { "Mode", "Copy" }
            };
            NavigateTo("MainRegion", "HerbDetailView", parameters);
        }

        /// <summary>
        /// 检查是否可以查看详情
        /// </summary>
        private bool CanViewDetail(HerbDto herb)
        {
            return herb != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEditHerb(HerbDto herb)
        {
            return herb != null && !IsBusy;
        }

        /// <summary>
        /// 检查是否可以复制
        /// </summary>
        private bool CanCopyHerb(HerbDto herb)
        {
            return herb != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true;
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

        #region 命令实现

        /// <summary>
        /// 切换药材状态
        /// </summary>
        private async Task ToggleStatusAsync(HerbDto herb)
        {
            if (herb == null) return;

            try
            {
                Logger.LogInformation("切换药材状态: {HerbId}", herb.Id);
                ShowInfoMessage($"切换药材 '{herb.Name}' 状态功能开发中");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "切换药材状态失败: {HerbId}", herb.Id);
                await ShowErrorMessageAsync("切换药材状态失败");
            }
        }

        /// <summary>
        /// 导入药材
        /// </summary>
        private async Task ImportHerbsAsync()
        {
            try
            {
                Logger.LogInformation("导入药材");
                ShowInfoMessage("导入药材功能开发中");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入药材失败");
                await ShowErrorMessageAsync("导入药材失败");
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private async Task ExportTemplateAsync()
        {
            try
            {
                Logger.LogInformation("导出药材导入模板");
                ShowInfoMessage("导出模板功能开发中");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出模板失败");
                await ShowErrorMessageAsync("导出模板失败");
            }
        }

        /// <summary>
        /// 导出药材
        /// </summary>
        private async Task ExportHerbsAsync()
        {
            try
            {
                Logger.LogInformation("导出药材数据");
                ShowInfoMessage("导出药材功能开发中");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出药材失败");
                await ShowErrorMessageAsync("导出药材失败");
            }
        }

        /// <summary>
        /// 跳转到首页
        /// </summary>
        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// 跳转到末页
        /// </summary>
        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
        }

        #endregion
    }
}
