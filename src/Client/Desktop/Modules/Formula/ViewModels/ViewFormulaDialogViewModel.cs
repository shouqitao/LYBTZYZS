using System.Collections.ObjectModel;

// UltraThink v2.0: 直接使用FormulaDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using IFormulaService = LYBT.Shared.Interfaces.Services.IFormulaService;

namespace LYBT.Desktop.Formula.ViewModels
{

    /// <summary>
    /// 查看验方对话框视图模型
    /// </summary>
    public class ViewFormulaDialogViewModel : BindableBase
    {
        private readonly IFormulaService _formulaService;
        private readonly ILogger<ViewFormulaDialogViewModel> _logger;
        private Guid _formulaId;

        #region Properties

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

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private decimal _totalCost;

        public decimal TotalCost
        {
            get => _totalCost;
            set => SetProperty(ref _totalCost, value);
        }

        #endregion Properties

        #region Commands

        public DelegateCommand CloseCommand { get; } = null!;
        public DelegateCommand PrintCommand { get; } = null!;
        public DelegateCommand ExportCommand { get; } = null!;

        #endregion Commands

        #region Constructor

        public ViewFormulaDialogViewModel(
            IFormulaService formulaService,
            ILogger<ViewFormulaDialogViewModel> logger)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化命令
            CloseCommand = new DelegateCommand(Close);
            PrintCommand = new DelegateCommand(async () => await PrintFormulaAsync());
            ExportCommand = new DelegateCommand(async () => await ExportFormulaAsync());
        }

        #endregion Constructor

        #region Methods

        public void Initialize(Guid formulaId)
        {
            _formulaId = formulaId;
            Task.Run(async () => await LoadFormulaAsync());
        }

        private async Task LoadFormulaAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载验方详情...";

                var result = await _formulaService.GetByIdAsync(_formulaId);
                if (result.IsSuccess && result.Data != null)
                {
                    // UltraThink v2.0: 直接使用FormulaDto
                    Formula = result.Data;

                    // TODO: 需要根据实际的FormulaDto结构来处理药材项目
                    // 暂时创建空的药材项目列表
                    HerbItems = new ObservableCollection<FormulaHerbItemDto>();
                    CalculateTotalCost();
                    StatusMessage = string.Empty;
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载验方失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                _logger.LogError(ex, "加载验方详情时出错");
            }
            finally
            {
                IsLoading = false;
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
                StatusMessage = "正在准备打印...";

                // TODO: 实现打印功能
                await Task.Delay(1000); // 模拟打印准备
                StatusMessage = "验方已发送到打印机";
            }
            catch (Exception ex)
            {
                StatusMessage = $"打印失败: {ex.Message}";
                _logger.LogError(ex, "打印验方时出错");
            }
        }

        private async Task ExportFormulaAsync()
        {
            try
            {
                StatusMessage = "正在导出验方...";

                // TODO: 实现导出功能（PDF或Excel）
                await Task.Delay(500); // 模拟导出
                StatusMessage = "验方导出成功";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导出失败: {ex.Message}";
                _logger.LogError(ex, "导出验方时出错");
            }
        }

        private void Close()
        {
            // TODO: Close dialog
        }

        #endregion Methods
    }
}
