using System.IO;
using LYBT.Desktop.Herbs.Components; // Epic #1773: 添加Component命名空间
using LYBT.Desktop.Herbs.Interfaces; // Epic #1962: 重新添加IHerbRepository（批量导入/导出需要）
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
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

        // Epic #1773: 使用DataManager替代Repository依赖
        private readonly HerbDataManager _dataManager;
        // Epic #1962: 批量导入/导出需要Repository和对话框服务
        private readonly IHerbRepository _herbRepository;
        private readonly ICommonDialogService _dialogService;

        #endregion

        #region 构造函数

        public HerbManagementViewModel(
            HerbDataManager dataManager, // Epic #1773: 注入DataManager
            IHerbRepository herbRepository, // Epic #1962: 注入Repository（批量导入/导出）
            ICommonDialogService dialogService, // Epic #1962: 注入对话框服务
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Epic #1773: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            // Epic #1962: 注入批量导入/导出依赖
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

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
            // 视图导航命令
            ViewDetailsCommand = new DelegateCommand<HerbDto>(ViewHerbDetail, CanViewDetail);
            EditCommand = new DelegateCommand<HerbDto>(EditHerb, CanEditHerb);
            CopyCommand = new DelegateCommand<HerbDto>(CopyHerb, CanCopyHerb);

            // 状态管理命令
            ToggleStatusCommand = new DelegateCommand<HerbDto>(
                async (herb) => await ToggleStatusAsync(herb),
                herb => herb != null && !IsBusy
            );

            // Epic #1962: 批量导入/导出命令（移除占位实现，启用真实功能）
            ImportHerbsCommand = new DelegateCommand(
                async () => await ImportHerbsAsync(),
                () => !IsBusy && !IsLoading
            ).ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading);

            ExportTemplateCommand = new DelegateCommand(
                async () => await ExportTemplateAsync(),
                () => !IsBusy && !IsLoading
            ).ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading);

            ExportHerbsCommand = new DelegateCommand(
                async () => await ExportHerbsAsync(),
                () => !IsBusy && !IsLoading && Items.Count > 0
            ).ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading).ObservesProperty(() => Items);

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
                // Epic #1773: 使用DataManager包装Repository方法
                var pagedData = await _dataManager.GetPagedAsync(page, pageSize, searchText);

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
            Logger.LogInformation("导航到创建药材视图");
            NavigateTo("ContentRegion", "HerbCreateView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(HerbDto item)
        {
            try
            {
                // Epic #1773: 使用DataManager包装Repository方法
                var success = await _dataManager.DeleteAsync(item.Id);

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
                        // Epic #1773: 使用DataManager包装Repository方法
                        var success = await _dataManager.DeleteAsync(id);
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

        #region 基类命令已自动暴露
        // Epic #1832: UnifiedListViewModelBase已公开SearchCommand, RefreshCommand, AddCommand等标准命令
        // 无需在子类中重复暴露，XAML可直接绑定基类命令
        #endregion

        #region 自定义命令

        /// <summary>
        /// 查看药材详情命令
        /// </summary>
        public DelegateCommand<HerbDto> ViewDetailsCommand { get; private set; } = null!;

        /// <summary>
        /// 编辑药材命令
        /// </summary>
        public DelegateCommand<HerbDto> EditCommand { get; private set; } = null!;

        /// <summary>
        /// 复制药材命令
        /// </summary>
        public DelegateCommand<HerbDto> CopyCommand { get; private set; } = null!;

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
        private void ViewHerbDetail(HerbDto herb)
        {
            if (herb == null) return;

            var parameters = new NavigationParameters
            {
                { "HerbId", herb.Id },
                { "ReadOnly", true }
            };
            NavigateTo("ContentRegion", "HerbDetailView", parameters);
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
            NavigateTo("ContentRegion", "HerbDetailView", parameters);
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
            NavigateTo("ContentRegion", "HerbDetailView", parameters);
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
            await ExecuteSafelyAsync(async () =>
            {
                // ① 打开文件选择对话框
                var filePath = await _dialogService.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择药材导入文件");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // ② 读取文件流
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                // ③ 调用Repository导入
                Logger.LogInformation("开始导入药材文件：{FileName}", fileName);
                var result = await _herbRepository.BatchImportAsync(fileStream, fileName);

                if (result == null)
                {
                    await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入药材");
                    return;
                }

                // ④ 显示导入结果
                var message = $"导入完成！\n\n" +
                              $"✅ 成功：{result.SuccessCount}条\n" +
                              $"❌ 失败：{result.FailureCount}条\n" +
                              $"⏭️ 跳过：{result.SkippedCount}条\n\n" +
                              $"成功率：{result.SuccessRate:F1}%";

                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3))
                    {
                        message += $"\n第{failure.RowNumber}行（{failure.HerbName}）：{failure.Reason}";
                    }
                }

                await _dialogService.ShowInfoAsync(message, "导入结果");

                // ⑤ 刷新列表
                if (result.SuccessCount > 0)
                {
                    await RefreshAsync();
                }
            }, "导入药材");
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private async Task ExportTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // ① 打开保存文件对话框
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存药材导入模板",
                    defaultFileName: $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // ② 下载模板
                Logger.LogInformation("下载药材导入模板");
                var bytes = await _herbRepository.ExportTemplateAsync();

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("下载模板失败，请稍后重试", "下载模板");
                    return;
                }

                // ③ 保存文件
                await File.WriteAllBytesAsync(filePath, bytes);

                await _dialogService.ShowInfoAsync(
                    $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入药材」功能导入。",
                    "下载成功");
            }, "下载模板");
        }

        /// <summary>
        /// 导出药材
        /// </summary>
        private async Task ExportHerbsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // ① 打开保存文件对话框
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出药材数据",
                    defaultFileName: $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // ② 导出数据（使用当前搜索关键词）
                Logger.LogInformation("导出药材数据，关键词：{Keyword}", SearchText);
                var bytes = await _herbRepository.ExportHerbsAsync(SearchText);

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("导出失败，请稍后重试", "导出药材");
                    return;
                }

                // ③ 保存文件
                await File.WriteAllBytesAsync(filePath, bytes);

                await _dialogService.ShowInfoAsync($"成功导出药材数据到：\n{filePath}", "导出成功");
            }, "导出药材");
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
