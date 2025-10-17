using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 验方校验视图模型 - 用于处理导入验方的药材映射和校验
    /// Issue #1352: 创建FormulaValidationViewModel
    /// </summary>
    public class FormulaValidationViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IFormulaRepository _formulaRepository;
        private readonly IHerbRepository _herbRepository;

        #endregion

        #region 私有字段

        private FormulaDto? _selectedFormula;
        private int _pendingFormulaCount;
        private int _totalUnvalidatedHerbsCount;

        #endregion

        #region 属性

        /// <summary>
        /// 待校验验方列表
        /// </summary>
        public ObservableCollection<FormulaDto> PendingFormulas { get; } = new();

        /// <summary>
        /// 当前选中的验方
        /// </summary>
        public FormulaDto? SelectedFormula
        {
            get => _selectedFormula;
            set
            {
                if (SetProperty(ref _selectedFormula, value))
                {
                    LoadHerbItems();
                    RefreshCommandStates();
                }
            }
        }

        /// <summary>
        /// 选中验方的药材组成列表
        /// </summary>
        public ObservableCollection<FormulaHerbItemDto> HerbItems { get; } = new();

        /// <summary>
        /// 待校验验方数量
        /// </summary>
        public int PendingFormulaCount
        {
            get => _pendingFormulaCount;
            set => SetProperty(ref _pendingFormulaCount, value);
        }

        /// <summary>
        /// 总未校验药材数量
        /// </summary>
        public int TotalUnvalidatedHerbsCount
        {
            get => _totalUnvalidatedHerbsCount;
            set => SetProperty(ref _totalUnvalidatedHerbsCount, value);
        }

        /// <summary>
        /// 是否有选中的验方
        /// </summary>
        public bool HasSelectedFormula => SelectedFormula != null;

        /// <summary>
        /// 未校验的药材数量
        /// </summary>
        public int UnvalidatedHerbsCount => HerbItems?.Count(h => !h.IsValidated) ?? 0;

        #endregion

        #region 命令

        /// <summary>
        /// 加载待校验验方命令
        /// </summary>
        public DelegateCommand LoadPendingFormulasCommand { get; }

        /// <summary>
        /// 选择药材命令（打开药材选择对话框）
        /// </summary>
        public DelegateCommand<FormulaHerbItemDto> SelectHerbCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public FormulaValidationViewModel(
            IFormulaRepository formulaRepository,
            IHerbRepository herbRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));

            PageTitle = "验方校验管理";

            // 初始化命令
            LoadPendingFormulasCommand = new DelegateCommand(
                async () => await ExecuteSafelyAsync(LoadPendingFormulasAsync),
                () => !IsBusy);

            SelectHerbCommand = new DelegateCommand<FormulaHerbItemDto>(
                async (herbItem) => await ExecuteSafelyAsync(() => SelectHerbAsync(herbItem)),
                CanSelectHerb);

            RefreshCommand = new DelegateCommand(
                async () => await ExecuteSafelyAsync(RefreshAsync),
                () => !IsBusy);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(SelectedFormula))
                {
                    RefreshCommandStates();
                }
            };
        }

        #endregion

        #region 导航生命周期

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadPendingFormulasAsync();
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载待校验验方列表
        /// </summary>
        private async Task LoadPendingFormulasAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载待校验验方...");

                // TODO: 等待 API 端点实现（Issue #1349 GetPendingValidationFormulasAsync）
                // 暂时使用 GetPagedAsync 获取所有验方，然后筛选 Draft 状态
                var pagedResult = await _formulaRepository.GetPagedAsync(1, 100);

                PendingFormulas.Clear();

                if (pagedResult?.Items != null && pagedResult.Items.Any())
                {
                    // 筛选 Draft 状态的验方（临时方案）
                    var draftFormulas = pagedResult.Items
                        .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
                        .ToList();

                    foreach (var formula in draftFormulas)
                    {
                        PendingFormulas.Add(formula);
                    }

                    PendingFormulaCount = draftFormulas.Count;
                    // TODO: 等待 FormulaDto 添加 UnvalidatedHerbsCount 属性（Issue #1344）
                    // 暂时计算未校验药材数量
                    TotalUnvalidatedHerbsCount = draftFormulas.Sum(f =>
                        f.Herbs?.Count(h => !h.IsValidated) ?? 0);

                    Logger.LogInformation("加载待校验验方成功：{Count}个验方，{HerbCount}味未校验药材",
                        PendingFormulaCount, TotalUnvalidatedHerbsCount);
                }
                else
                {
                    PendingFormulaCount = 0;
                    TotalUnvalidatedHerbsCount = 0;
                    Logger.LogInformation("暂无待校验验方");
                }

                // 如果有验方且当前未选中，自动选中第一个
                if (PendingFormulas.Any() && SelectedFormula == null)
                {
                    SelectedFormula = PendingFormulas.First();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载待校验验方时发生异常");
                await ShowErrorMessageAsync("加载待校验验方时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 加载选中验方的药材组成
        /// </summary>
        private void LoadHerbItems()
        {
            HerbItems.Clear();

            if (SelectedFormula?.Herbs != null)
            {
                foreach (var herb in SelectedFormula.Herbs)
                {
                    HerbItems.Add(herb);
                }

                Logger.LogInformation("加载验方「{Name}」的药材组成：{Count}味药材，{Unvalidated}味未校验",
                    SelectedFormula.Name,
                    HerbItems.Count,
                    UnvalidatedHerbsCount);
            }

            // 刷新未校验药材数量
            RaisePropertyChanged(nameof(UnvalidatedHerbsCount));
            RaisePropertyChanged(nameof(HasSelectedFormula));
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 选择药材（打开药材选择对话框）
        /// </summary>
        private async Task SelectHerbAsync(FormulaHerbItemDto? herbItem)
        {
            if (herbItem == null || SelectedFormula == null)
            {
                return;
            }

            // 检查是否已校验
            if (herbItem.IsValidated)
            {
                await ShowWarningMessageAsync("该药材已校验，无需重复操作");
                return;
            }

            try
            {
                SetIsBusy(true, $"正在处理药材「{herbItem.HerbName}」...");

                // TODO: 打开药材选择对话框（Issue #1353）
                // TODO: 调用 API 端点验证药材（Issue #1348 ValidateFormulaHerbAsync）
                // TODO: 使用 GetByNameOrPinyinAsync 查找药材（Issue #1351）

                // 暂时显示功能开发中的提示
                await ShowWarningMessageAsync(
                    $"药材校验功能开发中\n" +
                    $"原始名称：{herbItem.OriginalHerbName ?? herbItem.HerbName}\n" +
                    $"待实现功能：\n" +
                    $"1. 打开药材选择对话框\n" +
                    $"2. 调用API验证药材映射\n" +
                    $"3. 更新验方状态");

                Logger.LogInformation(
                    "药材校验功能尚未实现 - 验方ID: {FormulaId}, 药材: {HerbName}",
                    SelectedFormula.Id,
                    herbItem.HerbName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择药材时发生异常：{HerbName}", herbItem.HerbName);
                await ShowErrorMessageAsync("选择药材时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task RefreshAsync()
        {
            await LoadPendingFormulasAsync();
            await ShowSuccessMessageAsync("数据已刷新");
        }

        #endregion

        #region 命令状态

        /// <summary>
        /// 检查是否可以选择药材
        /// </summary>
        private bool CanSelectHerb(FormulaHerbItemDto? herbItem)
        {
            return !IsBusy &&
                   herbItem != null &&
                   !herbItem.IsValidated &&
                   SelectedFormula != null;
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        private void RefreshCommandStates()
        {
            LoadPendingFormulasCommand?.RaiseCanExecuteChanged();
            SelectHerbCommand?.RaiseCanExecuteChanged();
            RefreshCommand?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 清理资源

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PendingFormulas.Clear();
                HerbItems.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
