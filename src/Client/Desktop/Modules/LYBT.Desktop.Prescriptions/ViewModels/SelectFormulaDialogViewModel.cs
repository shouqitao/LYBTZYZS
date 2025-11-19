using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components; // Issue #1786: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 选择验方对话框视图模型 - UltraThink精简架构
    /// 用于从验方库中选择验方模板
    /// </summary>
    public class SelectFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        // Issue #1786: 使用DataManager替代直接Repository访问
        private readonly PrescriptionDataManager _dataManager;

        #endregion

        #region 数据属性

        private ObservableCollection<FormulaDto> _formulas = new();
        private FormulaDto? _selectedFormula;
        private string _searchText = string.Empty;
        private string _categoryFilter = string.Empty;
        private string _effectFilter = string.Empty;

        /// <summary>
        /// 验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> Formulas
        {
            get => _formulas;
            set => SetProperty(ref _formulas, value);
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
                    LoadFormulaDetails();
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
        /// 功效筛选
        /// </summary>
        public string EffectFilter
        {
            get => _effectFilter;
            set => SetProperty(ref _effectFilter, value);
        }

        /// <summary>
        /// 分类选项
        /// </summary>
        public string[] CategoryOptions { get; } = new[]
        {
            "全部", "解表剂", "泻下剂", "和解剂", "清热剂",
            "祛暑剂", "温里剂", "表里双解剂", "补益剂", "固涩剂",
            "安神剂", "开窍剂", "理气剂", "理血剂", "治风剂",
            "治燥剂", "祛湿剂", "祛痰剂", "消导剂", "驱虫剂"
        };

        /// <summary>
        /// 功效选项
        /// </summary>
        public string[] EffectOptions { get; } = new[]
        {
            "全部", "解表散寒", "解表清热", "扶正解表", "攻里泻热",
            "润燥通便", "温中祛寒", "回阳救逆", "温经散寒", "清热泻火",
            "清热凉血", "清热解毒", "清脏腑热", "清虚热", "祛暑解表",
            "祛暑利湿", "补气", "补血", "气血双补", "补阴", "补阳"
        };

        #endregion

        #region 详情属性

        private string _formulaDetails = string.Empty;
        private string _composition = string.Empty;
        private string _usage = string.Empty;
        private string _indications = string.Empty;

        /// <summary>
        /// 验方详情
        /// </summary>
        public string FormulaDetails
        {
            get => _formulaDetails;
            set => SetProperty(ref _formulaDetails, value);
        }

        /// <summary>
        /// 组成
        /// </summary>
        public string Composition
        {
            get => _composition;
            set => SetProperty(ref _composition, value);
        }

        /// <summary>
        /// 用法
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>
        /// 主治
        /// </summary>
        public string Indications
        {
            get => _indications;
            set => SetProperty(ref _indications, value);
        }

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "选择验方";

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
        /// 重置筛选命令
        /// </summary>
        public DelegateCommand ResetFilterCommand { get; }

        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand ViewDetailsCommand { get; }

        #endregion

        #region 构造函数

        public SelectFormulaDialogViewModel(
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
            ResetFilterCommand = new DelegateCommand(ResetFilter);
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailsCommand = new DelegateCommand(async () => await ViewDetailsAsync(), CanViewDetails);

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

                if (parameters.ContainsKey("Effect"))
                {
                    EffectFilter = parameters.GetValue<string>("Effect");
                }

                // 加载数据
                Task.Run(async () => await LoadDataAsync());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开选择验方对话框时发生异常");
                _ = ShowErrorMessageAsync("初始化失败，请稍后重试");
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
                SetIsBusy(true, "正在加载验方列表...");

                // Issue #1786: 使用DataManager包装Repository方法
                var pagedData = await _dataManager.GetFormulasPagedAsync(1, int.MaxValue, null);
                Formulas.Clear();
                foreach (var item in pagedData.Items)
                {
                    Formulas.Add(item);
                }

                Logger.LogInformation("验方列表加载完成，共 {Count} 个", Formulas.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方列表时发生异常");
                await ShowErrorMessageAsync("加载验方列表时发生系统错误，请稍后重试");
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

                // 按关键字筛选
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(f =>
                        f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        f.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                        f.Indications?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
                }

                // 按分类筛选
                if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "全部")
                {
                    filtered = filtered.Where(f => f.Category == CategoryFilter);
                }

                // 按功效筛选
                if (!string.IsNullOrWhiteSpace(EffectFilter) && EffectFilter != "全部")
                {
                    filtered = filtered.Where(f => f.Effect?.Contains(EffectFilter) == true);
                }

                Formulas.Clear();
                foreach (var item in filtered)
                {
                    Formulas.Add(item);
                }

                Logger.LogDebug("搜索完成，找到 {Count} 个验方", Formulas.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索验方时发生异常");
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
            ResetFilter();
            await LoadDataAsync();
        }

        /// <summary>
        /// 加载验方详情
        /// </summary>
        private void LoadFormulaDetails()
        {
            if (SelectedFormula == null)
            {
                ClearDetails();
                return;
            }

            try
            {
                var formula = SelectedFormula;

                // 构建详情信息
                var details = $"验方名称: {formula.Name}\n";
                details += $"分类: {formula.Category}\n";

                if (!string.IsNullOrEmpty(formula.Source))
                {
                    details += $"出处: {formula.Source}\n";
                }

                if (!string.IsNullOrEmpty(formula.Description))
                {
                    details += $"描述: {formula.Description}\n";
                }

                FormulaDetails = details;

                // 构建组成信息
                if (formula.Herbs?.Any() == true)
                {
                    var composition = "组成:\n";
                    foreach (var item in formula.Herbs)
                    {
                        composition += $"• {item.HerbName} {item.Quantity}{item.Unit}";
                        if (!string.IsNullOrEmpty(item.Processing))
                        {
                            composition += $" ({item.Processing})";
                        }
                        composition += "\n";
                    }
                    Composition = composition;
                }
                else
                {
                    Composition = "组成信息暂无";
                }

                // 用法
                Usage = !string.IsNullOrEmpty(formula.Usage) ? $"用法: {formula.Usage}" : "用法信息暂无";

                // 主治
                Indications = !string.IsNullOrEmpty(formula.Indications) ? $"主治: {formula.Indications}" : "主治信息暂无";

                Logger.LogDebug("加载验方详情: {FormulaName}", formula.Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方详情时发生异常");
                ClearDetails();
            }
        }

        /// <summary>
        /// 清空详情
        /// </summary>
        private void ClearDetails()
        {
            FormulaDetails = string.Empty;
            Composition = string.Empty;
            Usage = string.Empty;
            Indications = string.Empty;
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 重置筛选
        /// </summary>
        private void ResetFilter()
        {
            SearchText = string.Empty;
            CategoryFilter = "全部";
            EffectFilter = "全部";
        }

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
        /// 查看详情
        /// </summary>
        private async Task ViewDetailsAsync()
        {
            if (SelectedFormula != null)
            {
                var detailInfo = GenerateDetailInfo(SelectedFormula);
                await ShowSuccessMessageAsync($"验方详情\n\n{detailInfo}");
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanConfirm() => SelectedFormula != null && !IsBusy;
        private bool CanViewDetails() => SelectedFormula != null;

        private void UpdateCommandStates()
        {
            ConfirmCommand.RaiseCanExecuteChanged();
            ViewDetailsCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成详细信息
        /// </summary>
        private string GenerateDetailInfo(FormulaDto formula)
        {
            var info = $"名称: {formula.Name}\n";
            info += $"分类: {formula.Category}\n";

            if (!string.IsNullOrEmpty(formula.Source))
            {
                info += $"出处: {formula.Source}\n";
            }

            if (!string.IsNullOrEmpty(formula.Description))
            {
                info += $"描述: {formula.Description}\n";
            }

            if (formula.Herbs?.Any() == true)
            {
                info += "\n药材组成:\n";
                foreach (var item in formula.Herbs)
                {
                    info += $"• {item.HerbName} {item.Quantity}{item.Unit}";
                    if (!string.IsNullOrEmpty(item.Processing))
                    {
                        info += $" ({item.Processing})";
                    }
                    info += "\n";
                }
            }

            if (!string.IsNullOrEmpty(formula.Usage))
            {
                info += $"\n用法: {formula.Usage}\n";
            }

            if (!string.IsNullOrEmpty(formula.Indications))
            {
                info += $"\n主治: {formula.Indications}\n";
            }

            if (!string.IsNullOrEmpty(formula.Contraindications))
            {
                info += $"\n禁忌: {formula.Contraindications}\n";
            }

            if (!string.IsNullOrEmpty(formula.Remark))
            {
                info += $"\n备注: {formula.Remark}";
            }

            return info;
        }

        /// <summary>
        /// 检查验方是否符合搜索条件
        /// </summary>
        public bool DoesFormulaMatchSearch(FormulaDto formula)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return formula.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   formula.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                   formula.Indications?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
        }

        #endregion
    }
}
