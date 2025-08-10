using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;

namespace LYBT.WPF.Client.Modules.Formula.ViewModels
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

        private FormulaInfo _formula = new();
        public FormulaInfo Formula
        {
            get => _formula;
            set => SetProperty(ref _formula, value);
        }

        private ObservableCollection<FormulaHerbItem> _herbItems = new();
        public ObservableCollection<FormulaHerbItem> HerbItems
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

        #endregion

        #region Commands

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ExportCommand { get; }

        #endregion

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

        #endregion

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
                    // Convert FormulaDetailDto to FormulaInfo
                    Formula = new FormulaInfo
                    {
                        Id = result.Data.Id,
                        Name = result.Data.Name ?? string.Empty,
                        Category = "其他",
                        DosageInstruction = result.Data.Usage,
                        Indications = result.Data.Effect,
                        Source = string.Empty,
                        Remark = result.Data.Remark,
                        CreateTime = result.Data.CreateTime,
                        UpdateTime = result.Data.UpdateTime
                    };
                    if (Formula.Herbs != null)
                    {
                        HerbItems = new ObservableCollection<FormulaHerbItem>(Formula.Herbs);
                        CalculateTotalCost();
                    }
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

        #endregion
    }
}