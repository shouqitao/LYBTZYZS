using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 验方模板对话框视图模型 - UltraThink精简架构
    /// 提供验方模板的选择和预览功能
    /// </summary>
    public class FormulaTemplateDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly IFormulaService _formulaService;

        #endregion

        #region 数据属性

        private ObservableCollection<FormulaDto> _formulaTemplates = new();
        private FormulaDto? _selectedFormula;
        private string _searchText = string.Empty;
        private string _categoryFilter = string.Empty;

        /// <summary>
        /// 验方模板列表
        /// </summary>
        public ObservableCollection<FormulaDto> FormulaTemplates
        {
            get => _formulaTemplates;
            set => SetProperty(ref _formulaTemplates, value);
        }

        /// <summary>
        /// 选中的验方
        /// </summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 分类筛选
        /// </summary>
        public string CategoryFilter
        {
            get => _categoryFilter;
            set => SetProperty(ref _categoryFilter, value);
        }

        /// <summary>
        /// 分类选项
        /// </summary>
        public string[] CategoryOptions { get; } = new[]
        {
            "全部", "补益方", "解表方", "清热方", "泻下方",
            "化痰止咳方", "理气方", "活血化瘀方", "温里方", "其他"
        };

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "选择验方模板";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 预览命令
        /// </summary>
        public DelegateCommand PreviewCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public FormulaTemplateDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IFormulaService formulaService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            PreviewCommand = new DelegateCommand(Preview, CanPreview);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed() { }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 获取参数
                if (parameters.ContainsKey("Title"))
                {
                    Title = parameters.GetValue<string>("Title");
                }

                if (parameters.ContainsKey("Category"))
                {
                    CategoryFilter = parameters.GetValue<string>("Category");
                }

                // 加载数据
                Task.Run(async () => await LoadDataAsync());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开验方模板对话框时发生异常");
                ShowErrorMessage("初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载验方模板...");

                var result = await _formulaService.GetPagedAsync(1, int.MaxValue, null);
                if (result.IsSuccess && result.Data?.Items != null)
                {
                    FormulaTemplates.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        FormulaTemplates.Add(item);
                    }

                    Logger.LogInformation("验方模板加载完成，共 {Count} 个", FormulaTemplates.Count);
                }
                else
                {
                    await ShowErrorMessageAsync($"加载验方模板失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方模板时发生异常");
                await ShowErrorMessageAsync("加载验方模板时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private async Task SearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索...");

                var allFormulas = await _formulaService.GetPagedAsync(1, int.MaxValue, null);
                if (allFormulas.IsSuccess && allFormulas.Data?.Items != null)
                {
                    var filtered = allFormulas.Data.Items.AsEnumerable();

                    // 按关键字筛选
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        filtered = filtered.Where(f =>
                            f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            f.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    // 按分类筛选
                    if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "全部")
                    {
                        filtered = filtered.Where(f => f.Category == CategoryFilter);
                    }

                    FormulaTemplates.Clear();
                    foreach (var item in filtered)
                    {
                        FormulaTemplates.Add(item);
                    }

                    Logger.LogDebug("搜索完成，找到 {Count} 个验方", FormulaTemplates.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索验方模板时发生异常");
                await ShowErrorMessageAsync("搜索失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private async Task RefreshAsync()
        {
            SearchText = string.Empty;
            CategoryFilter = "全部";
            await LoadDataAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 确定
        /// </summary>
        private void Confirm()
        {
            if (SelectedFormula != null)
            {
                var parameters = new DialogParameters
                {
                    { "SelectedFormula", SelectedFormula }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                Logger.LogInformation("选择验方: {FormulaName}", SelectedFormula.Name);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 预览
        /// </summary>
        private void Preview()
        {
            if (SelectedFormula != null)
            {
                var previewInfo = GeneratePreviewInfo(SelectedFormula);
                ShowInfoMessage($"验方预览\n\n{previewInfo}");
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanConfirm() => SelectedFormula != null && !IsBusy;
        private bool CanPreview() => SelectedFormula != null;

        private void UpdateCommandStates()
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            PreviewCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成预览信息
        /// </summary>
        private string GeneratePreviewInfo(FormulaDto formula)
        {
            var info = $"名称: {formula.Name}\n";
            info += $"分类: {formula.Category}\n";

            if (!string.IsNullOrEmpty(formula.Description))
            {
                info += $"描述: {formula.Description}\n";
            }

            if (formula.Items?.Any() == true)
            {
                info += "\n药材组成:\n";
                foreach (var item in formula.Items)
                {
                    info += $"• {item.HerbName} {item.Quantity}{item.Unit}\n";
                }
            }

            if (!string.IsNullOrEmpty(formula.Usage))
            {
                info += $"\n用法: {formula.Usage}";
            }

            return info;
        }

        #endregion
    }
}
