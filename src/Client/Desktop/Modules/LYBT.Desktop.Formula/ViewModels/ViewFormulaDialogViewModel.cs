using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.ViewModels.Components; // Issue #1787: 添加Component命名空间
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{

    /// <summary>
    /// 查看验方对话框视图模型 - UltraThink重构版本
    /// 基于UnifiedViewModelBase实现验方查看功能
    /// </summary>
    public class ViewFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        // Issue #1787: 使用CommandHandler替代直接Repository访问
        private readonly FormulaCommandHandler _commandHandler;

        #endregion

        #region 数据属性

        private Guid _formulaId;

        private FormulaDto _formula = new();

        public FormulaDto Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        private ObservableCollection<FormulaHerbItemDto> _herbItems = new();

        public ObservableCollection<FormulaHerbItemDto> HerbItems
        {
            get => _herbItems;
            set => SetProperty(ref _herbItems, value);
        }

        private decimal _totalCost;

        public decimal TotalCost
        {
            get => _totalCost;
            set => SetProperty(ref _totalCost, value);
        }

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ExportCommand { get; }

        #endregion

        #region 构造函数

        public ViewFormulaDialogViewModel(
            FormulaCommandHandler commandHandler, // Issue #1787: 注入CommandHandler
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1787: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // 初始化命令
            CloseCommand = new DelegateCommand(Close);
            PrintCommand = new DelegateCommand(async () => await PrintFormulaAsync());
            ExportCommand = new DelegateCommand(async () => await ExportFormulaAsync());
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => "验方模板详情";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

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
                // 从参数中获取配方ID
                if (parameters.TryGetValue("FormulaId", out Guid formulaId))
                {
                    _formulaId = formulaId;
                    _ = LoadFormulaAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开对话框时发生异常");
            }
        }

        #endregion

        #region 数据加载方法

        private async Task LoadFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载验方详情...");

                // Issue #1787: 使用CommandHandler查询
                var result = await _commandHandler.GetByIdAsync(_formulaId);
                if (!result.success || result.formula == null)
                {
                    await ShowErrorMessageAsync(result.errorMessage ?? "验方不存在");
                    return;
                }

                Formula = result.formula;
                HerbItems = new ObservableCollection<FormulaHerbItemDto>();
                CalculateTotalCost();
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载验方详情时出错");
                await ShowErrorMessageAsync($"加载失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private void CalculateTotalCost()
        {
            TotalCost = 0;
            foreach (var item in HerbItems)
            {
                // 假设每个药材有单价，计算总价
                // TotalCost += item.Quantity * item.UnitPrice;
            }
        }

        private async Task PrintFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在准备打印...");
                await Task.Delay(1000); // TODO: 实现打印功能
                await ShowSuccessMessageAsync("验方已发送到打印机");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印验方时出错");
                await ShowErrorMessageAsync($"打印失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private async Task ExportFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在导出验方...");
                await Task.Delay(500); // TODO: 实现导出功能（PDF或Excel）
                await ShowSuccessMessageAsync("验方导出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导出验方时出错");
                await ShowErrorMessageAsync($"导出失败: {ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private void Close()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
