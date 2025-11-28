using LYBT.Desktop.Formula.Interfaces; // Desktop层架构重构 Phase 1: 接口化
using LYBT.Desktop.Formula.ViewModels.Components; // Issue #1787: 添加Component命名空间
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs; // OpenSpec: add-global-audit-system

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
        private readonly IFormulaCommandHandler _commandHandler; // Desktop层架构重构 Phase 1: 接口化
        private readonly IDialogService _prismDialogService; // OpenSpec: add-global-audit-system

        #endregion

        #region 构造函数

        public FormulaManagementViewModel(
            IFormulaCommandHandler commandHandler, // Desktop层架构重构 Phase 1: 接口化
            IDialogService prismDialogService, // OpenSpec: add-global-audit-system
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1787: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService)); // OpenSpec: add-global-audit-system

            PageTitle = "配方管理";

            // OpenSpec: add-global-audit-system - 初始化审计日志命令
            ShowAuditLogCommand = new DelegateCommand<FormulaDto>(ExecuteShowAuditLog, formula => formula != null);
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
        /// 批量删除验方（实现基类虚方法）
        /// </summary>
        /// <summary>
        /// 批量删除验方（实现基类抽象方法）
        /// Issue #2158: BR-001（权限控制）、BR-003（结果反馈）、BR-004（失败不影响其他）
        /// </summary>
        /// <remarks>
        /// 基类ExecuteBatchDeleteAsync已处理确认对话框（BR-002），此方法只负责执行删除逻辑
        /// </remarks>
        protected override async Task OnExecuteBatchDeleteAsync(List<FormulaDto> items)
        {
            if (items == null || items.Count == 0)
            {
                Logger.LogWarning("OnExecuteBatchDeleteAsync: 验方列表为空");
                return;
            }

            Logger.LogInformation("开始批量删除验方，数量: {Count}", items.Count);

            // BR-003: 统计删除结果
            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();

            // BR-004: 逐个删除，部分失败不影响其他
            foreach (var item in items)
            {
                try
                {
                    // BR-001: 调用CommandHandler.DeleteAsync（包含权限检查）
                    var success = await _commandHandler.DeleteAsync(item.Id);
                    if (success)
                    {
                        successCount++;
                        Logger.LogInformation("成功删除验方: {FormulaName}", item.Name);
                    }
                    else
                    {
                        failureCount++;
                        failedItems.Add(item.Name);
                        Logger.LogWarning("删除验方失败: {FormulaName}", item.Name);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    failedItems.Add(item.Name);
                    Logger.LogError(ex, "删除验方时发生异常: {FormulaName}", item.Name);
                }
            }

            // BR-003: 生成结果消息
            var message = $"批量删除完成！\n\n" +
                          $"成功：{successCount}个\n" +
                          $"失败：{failureCount}个";

            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的验方：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5)
                {
                    message += $"等{failedItems.Count}个";
                }
            }

            // BR-003: 显示结果反馈
            if (failureCount > 0)
            {
                await ShowWarningMessageAsync(message);
            }
            else
            {
                await ShowSuccessMessageAsync(message);
            }

            Logger.LogInformation("批量删除完成，成功: {SuccessCount}, 失败: {FailureCount}",
                successCount, failureCount);
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
        /// 首页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand FirstPageCommand => base.FirstPageCommand;

        /// <summary>
        /// 末页命令 - 暴露基类实现
        /// </summary>
        public new DelegateCommand LastPageCommand => base.LastPageCommand;

        /// <summary>
        /// 添加配方命令 - 别名指向 AddCommand
        /// </summary>
        public DelegateCommand AddFormulaCommand => AddCommand;

        /// <summary>
        /// 查看详情命令 - 别名指向 ViewDetailCommand
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailsCommand => ViewDetailCommand;

        /// <summary>
        /// 查看审计日志命令
        /// OpenSpec: add-global-audit-system
        /// </summary>
        public DelegateCommand<FormulaDto> ShowAuditLogCommand { get; private set; } = null!;

        #endregion

        #region 自定义功能

        /// <summary>
        /// 查看配方详情
        /// </summary>
        public DelegateCommand<FormulaDto> ViewDetailCommand =>
            new DelegateCommand<FormulaDto>(ViewFormulaDetail, CanViewDetail);

        /// <summary>
        /// 编辑配方
        /// </summary>
        public DelegateCommand<FormulaDto> EditCommand =>
            new DelegateCommand<FormulaDto>(EditFormula, CanEditFormula);

        /// <summary>
        /// 复制配方
        /// </summary>
        public DelegateCommand<FormulaDto> CopyCommand =>
            new DelegateCommand<FormulaDto>(CopyFormula, CanCopyFormula);

        /// <summary>
        /// 查看配方详情
        /// </summary>
        private void ViewFormulaDetail(FormulaDto formula)
        {
            if (formula == null) return;

            var parameters = new NavigationParameters
            {
                { "FormulaId", formula.Id },
                { "ReadOnly", true }
            };
            NavigateTo("ContentRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 编辑配方
        /// </summary>
        private void EditFormula(FormulaDto formula)
        {
            if (formula == null) return;

            var parameters = new NavigationParameters
            {
                { "FormulaId", formula.Id }
            };
            NavigateTo("ContentRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 复制配方
        /// </summary>
        private void CopyFormula(FormulaDto formula)
        {
            if (formula == null) return;

            var parameters = new NavigationParameters
            {
                { "SourceFormulaId", formula.Id },
                { "Mode", "Copy" }
            };
            NavigateTo("ContentRegion", "FormulaDetailView", parameters);
        }

        /// <summary>
        /// 检查是否可以查看详情
        /// </summary>
        private bool CanViewDetail(FormulaDto formula)
        {
            return formula != null && !IsBusy;
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
        /// 显示审计日志
        /// OpenSpec: add-global-audit-system
        /// </summary>
        private void ExecuteShowAuditLog(FormulaDto? formula)
        {
            if (formula == null) return;
            Logger.LogInformation("查看验方审计日志：{FormulaId} - {FormulaName}", formula.Id, formula.Name);
            var parameters = new DialogParameters
            {
                { "EntityType", "formula" },
                { "EntityId", formula.Id },
                { "EntityDescription", $"验方：{formula.Name}" }
            };
            _prismDialogService.ShowDialog("EntityAuditLogDialog", parameters, _ => { });
        }

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
            ImportFormulasCommand?.RaiseCanExecuteChanged();
            ExportTemplateCommand?.RaiseCanExecuteChanged();
            ExportFormulasCommand?.RaiseCanExecuteChanged();
            ClearFiltersCommand?.RaiseCanExecuteChanged();
            SearchByCategoryCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
