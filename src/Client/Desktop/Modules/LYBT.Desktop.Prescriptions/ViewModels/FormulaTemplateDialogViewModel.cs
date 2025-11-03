using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components; // Issue #1786: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
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

        // Issue #1786: 使用DataManager替代直接Repository访问
        private readonly PrescriptionDataManager _dataManager;

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

        /// <summary>
        /// 处方ID - 用于导入验方功能 (Issue #1367 ENTRY-9)
        /// </summary>
        public Guid PrescriptionId { get; private set; }

        /// <summary>
        /// 医案ID（Epic #1600 Phase 5）
        /// </summary>
        public Guid MedicalCaseId { get; private set; }

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

        /// <summary>
        /// 选择命令 - Phase 4B 补充（别名 ConfirmCommand）
        /// </summary>
        public DelegateCommand SelectCommand { get; }

        /// <summary>
        /// 查看详情命令 - Phase 4B 补充（别名 PreviewCommand）
        /// </summary>
        public DelegateCommand ViewDetailsCommand { get; }

        /// <summary>
        /// 导入验方命令 (Issue #1367 ENTRY-9)
        /// </summary>
        public DelegateCommand ImportCommand { get; }

        #endregion

        #region 构造函数

        public FormulaTemplateDialogViewModel(
            PrescriptionDataManager dataManager, // Issue #1786: 注入DataManager
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1786: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            PreviewCommand = new DelegateCommand(Preview, CanPreview);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ImportCommand = new DelegateCommand(async () => await ImportFormulaAsync(), CanImport);

            // Phase 4B 别名命令
            SelectCommand = ConfirmCommand;
            ViewDetailsCommand = PreviewCommand;

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

                // Issue #1367 ENTRY-9: 获取处方ID用于导入功能
                if (parameters.ContainsKey("PrescriptionId"))
                {
                    PrescriptionId = parameters.GetValue<Guid>("PrescriptionId");
                }

                // Epic #1600 Phase 5: 获取医案ID用于聚合根方法
                if (parameters.ContainsKey("MedicalCaseId"))
                {
                    MedicalCaseId = parameters.GetValue<Guid>("MedicalCaseId");
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

                // Issue #1786: 使用DataManager包装Repository方法
                var pagedData = await _dataManager.GetFormulasPagedAsync(1, int.MaxValue, null);
                FormulaTemplates.Clear();
                // Issue #1354: 只显示已验证的验方
                foreach (var item in pagedData.Items.Where(f => f.ValidationStatus == FormulaValidationStatus.Validated))
                {
                    FormulaTemplates.Add(item);
                }

                Logger.LogInformation("验方模板加载完成，共 {Count} 个", FormulaTemplates.Count);
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

                // Issue #1786: 使用DataManager包装Repository方法
                var allFormulas = await _dataManager.GetFormulasPagedAsync(1, int.MaxValue, null);
                var filtered = allFormulas.Items.AsEnumerable();

                // Issue #1354: 只显示已验证的验方
                filtered = filtered.Where(f => f.ValidationStatus == FormulaValidationStatus.Validated);

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

        /// <summary>
        /// 导入验方到处方 (Issue #1367 ENTRY-9)
        /// </summary>
        private async Task ImportFormulaAsync()
        {
            if (SelectedFormula == null || PrescriptionId == Guid.Empty || MedicalCaseId == Guid.Empty)
            {
                return;
            }

            try
            {
                SetIsBusy(true, $"正在导入验方\"{SelectedFormula.Name}\"...");

                // Issue #1786: 使用DataManager包装Repository方法
                await _dataManager.ImportFormulaIntoPrescriptionAsync(MedicalCaseId, SelectedFormula.Id);

                Logger.LogInformation("验方\"{FormulaName}\"导入成功", SelectedFormula.Name);
                await ShowSuccessMessageAsync($"验方\"{SelectedFormula.Name}\"已成功导入到处方");

                // 关闭对话框并通知刷新
                var parameters = new DialogParameters
                {
                    { "Imported", true },
                    { "FormulaName", SelectedFormula.Name }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导入验方时发生异常: {FormulaName}", SelectedFormula?.Name);
                await ShowErrorMessageAsync($"导入验方失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanConfirm() => SelectedFormula != null && !IsBusy;
        private bool CanPreview() => SelectedFormula != null;
        private bool CanImport() => SelectedFormula != null && PrescriptionId != Guid.Empty && !IsBusy;

        private void UpdateCommandStates()
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            PreviewCommand.RaiseCanExecuteChanged();
            ImportCommand.RaiseCanExecuteChanged();
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

            if (formula.Herbs?.Any() == true)
            {
                info += "\n药材组成:\n";
                foreach (var item in formula.Herbs)
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
