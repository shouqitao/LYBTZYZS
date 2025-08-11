using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Dialogs;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Extensions;

namespace LYBT.Desktop.Consultation.ViewModels.Components
{
    /// <summary>
    /// 处方命令处理器 - UltraThink专门化组件
    /// 职责单一：专注处方相关命令的处理和执行
    /// 代码干净：清晰的命令模式实现和错误处理
    /// 性能出色：优化的异步命令执行和资源管理
    /// </summary>
    public class PrescriptionCommandHandler
    {
        private readonly IHerbService _herbService;
        private readonly IFormulaService _formulaService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<PrescriptionCommandHandler> _logger;

        // 关联的数据管理器和验证器
        private PrescriptionDataManager? _dataManager;
        private PrescriptionValidator? _validator;
        private PrescriptionCalculator? _calculator;

        public PrescriptionCommandHandler(
            IHerbService herbService,
            IFormulaService formulaService,
            IPrescriptionService prescriptionService,
            IDialogService dialogService,
            ILogger<PrescriptionCommandHandler> logger)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
        }

        #region 命令属性

        public ICommand SaveCommand { get; private set; } = null!;
        public ICommand ClearCommand { get; private set; } = null!;
        public ICommand AddHerbCommand { get; private set; } = null!;
        public ICommand RemoveHerbCommand { get; private set; } = null!;
        public ICommand ImportFormulaCommand { get; private set; } = null!;
        public ICommand ImportHistoryCommand { get; private set; } = null!;
        public ICommand SetDiscountCommand { get; private set; } = null!;
        public ICommand SetDosageCommand { get; private set; } = null!;
        public ICommand GeneratePrescriptionNoCommand { get; private set; } = null!;
        public ICommand PrintPreviewCommand { get; private set; } = null!;
        public ICommand ValidateCommand { get; private set; } = null!;
        public ICommand RecalculateCommand { get; private set; } = null!;

        #endregion

        #region 依赖注入

        /// <summary>
        /// 设置关联组件（依赖注入）
        /// </summary>
        public void SetDependencies(
            PrescriptionDataManager dataManager,
            PrescriptionValidator validator,
            PrescriptionCalculator calculator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));

            // 重新初始化命令，因为现在有了依赖
            InitializeCommands();
        }

        #endregion

        #region 命令初始化

        private void InitializeCommands()
        {
            SaveCommand = new DelegateCommand(
                async () => await ExecuteCommandSafelyAsync(SaveAsync),
                () => CanExecuteSave());

            ClearCommand = new DelegateCommand(
                () => ExecuteCommandSafely(Clear),
                () => CanExecuteClear());

            AddHerbCommand = new DelegateCommand(
                async () => await ExecuteCommandSafelyAsync(AddHerbAsync));

            RemoveHerbCommand = new DelegateCommand<PrescriptionItemViewModel>(
                item => ExecuteCommandSafely(() => RemoveHerb(item)));

            ImportFormulaCommand = new DelegateCommand(
                async () => await ExecuteCommandSafelyAsync(ImportFormulaAsync));

            ImportHistoryCommand = new DelegateCommand(
                async () => await ExecuteCommandSafelyAsync(ImportHistoryAsync));

            SetDiscountCommand = new DelegateCommand<string>(
                discountStr => ExecuteCommandSafely(() => SetDiscount(discountStr)));

            SetDosageCommand = new DelegateCommand<string>(
                dosageStr => ExecuteCommandSafely(() => SetDosage(dosageStr)));

            GeneratePrescriptionNoCommand = new DelegateCommand(
                () => ExecuteCommandSafely(GeneratePrescriptionNo));

            PrintPreviewCommand = new DelegateCommand(
                async () => await ExecuteCommandSafelyAsync(PrintPreviewAsync));

            ValidateCommand = new DelegateCommand(
                () => ExecuteCommandSafely(ValidatePrescription));

            RecalculateCommand = new DelegateCommand(
                () => ExecuteCommandSafely(RecalculatePrice));
        }

        #endregion

        #region 命令执行方法

        /// <summary>
        /// 保存处方
        /// </summary>
        private async Task SaveAsync()
        {
            if (_dataManager == null) return;

            try
            {
                _logger.LogInformation("开始保存处方");

                var success = await _dataManager.SaveAsync();
                if (success)
                {
                    _logger.LogInformation("处方保存成功");
                    // 可以触发保存成功事件
                    OnPrescriptionSaved?.Invoke();
                }
                else
                {
                    _logger.LogWarning("处方保存失败");
                    ShowErrorMessage("保存失败", "处方保存失败，请重试");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生错误");
                ShowErrorMessage("保存错误", "保存过程中发生错误，请联系管理员");
            }
        }

        /// <summary>
        /// 清空处方
        /// </summary>
        private void Clear()
        {
            if (_dataManager == null) return;

            try
            {
                var result = ShowConfirmDialog("确认清空", "确定要清空当前处方吗？此操作不可撤销。");
                if (result)
                {
                    _dataManager.Clear();
                    _logger.LogInformation("处方已清空");
                    OnPrescriptionCleared?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空处方时发生错误");
            }
        }

        /// <summary>
        /// 添加药材
        /// </summary>
        private async Task AddHerbAsync()
        {
            try
            {
                _logger.LogDebug("开始选择药材");

                var parameters = new DialogParameters
                {
                    { "Title", "选择药材" },
                    { "AllowMultipleSelection", true }
                };

                _dialogService.ShowDialog("SelectHerbDialog", parameters, result =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedHerbs"))
                    {
                        var selectedHerbs = result.Parameters.GetValue<dynamic>("SelectedHerbs");
                        Task.Run(async () => await AddHerbItems(selectedHerbs));
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材时发生错误");
                ShowErrorMessage("添加失败", "添加药材失败，请重试");
            }
        }

        /// <summary>
        /// 添加药材项
        /// </summary>
        private async Task AddHerbItems(dynamic herbItems)
        {
            if (_dataManager == null || herbItems == null) return;

            try
            {
                foreach (var herbItem in herbItems)
                {
                    var prescriptionItem = new PrescriptionItemViewModel
                    {
                        HerbId = herbItem.Id,
                        HerbName = herbItem.Name,
                        Quantity = 10m, // 默认数量
                        Unit = herbItem.Unit ?? "g",
                        UnitPrice = herbItem.Price
                        // Subtotal会自动计算，无需手动赋值
                    };

                    _dataManager.AddPrescriptionItem(prescriptionItem);
                }

                RecalculatePrice();
                _logger.LogInformation("成功添加 {Count} 个药材", ((IEnumerable<dynamic>)herbItems).Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材项时发生错误");
                ShowErrorMessage("添加失败", "添加药材项失败");
            }
        }

        /// <summary>
        /// 移除药材
        /// </summary>
        private void RemoveHerb(PrescriptionItemViewModel? item)
        {
            if (_dataManager == null || item == null) return;

            try
            {
                _dataManager.RemovePrescriptionItem(item);
                RecalculatePrice();
                _logger.LogDebug("移除药材: {HerbName}", item.HerbName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除药材时发生错误");
            }
        }

        /// <summary>
        /// 导入验方
        /// </summary>
        private async Task ImportFormulaAsync()
        {
            try
            {
                _logger.LogDebug("开始选择验方");

                var parameters = new DialogParameters { { "Title", "选择验方" } };
                _dialogService.ShowDialog("SelectFormulaDialog", parameters, result =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedFormula"))
                    {
                        var selectedFormula = result.Parameters.GetValue<dynamic>("SelectedFormula");
                        ImportFormulaItems(selectedFormula);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方时发生错误");
                ShowErrorMessage("导入失败", "导入验方失败，请重试");
            }
        }

        /// <summary>
        /// 导入验方项
        /// </summary>
        private void ImportFormulaItems(dynamic formula)
        {
            if (_dataManager == null || formula?.Items == null) return;

            try
            {
                foreach (var item in formula.Items)
                {
                    var prescriptionItem = new PrescriptionItemViewModel
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice
                        // Subtotal会自动计算，无需手动赋值
                    };

                    _dataManager.AddPrescriptionItem(prescriptionItem);
                }

                RecalculatePrice();
                _logger.LogInformation("成功导入验方: {FormulaName}", (string)formula.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方项时发生错误");
                ShowErrorMessage("导入失败", "导入验方项失败");
            }
        }

        /// <summary>
        /// 导入历史处方
        /// </summary>
        private async Task ImportHistoryAsync()
        {
            if (_dataManager == null) return;

            try
            {
                _logger.LogDebug("开始选择历史处方");

                var parameters = new DialogParameters
                {
                    { "Title", "选择历史处方" },
                    { "MedicalCaseId", _dataManager.MedicalCaseId }
                };

                _dialogService.ShowDialog("SelectHistoryDialog", parameters, result =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("SelectedPrescription"))
                    {
                        var selectedPrescription = result.Parameters.GetValue<dynamic>("SelectedPrescription");
                        ImportHistoryItems(selectedPrescription);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入历史处方时发生错误");
                ShowErrorMessage("导入失败", "导入历史处方失败");
            }
        }

        /// <summary>
        /// 导入历史处方项
        /// </summary>
        private void ImportHistoryItems(dynamic prescription)
        {
            if (_dataManager == null || prescription?.Items == null) return;

            try
            {
                // 先清空当前处方
                _dataManager.Clear();

                // 导入历史数据
                _dataManager.Usage = prescription.Usage ?? "水煎服，一日三次，饭后服用";
                _dataManager.DosageCount = prescription.DosageCount;
                _dataManager.MedicalAdvice = prescription.MedicalAdvice ?? string.Empty;
                _dataManager.Remark = prescription.Remark ?? string.Empty;
                _dataManager.Discount = prescription.Discount;

                foreach (var item in prescription.Items)
                {
                    var prescriptionItem = new PrescriptionItemViewModel
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice
                        // Subtotal会自动计算，无需手动赋值
                    };

                    _dataManager.AddPrescriptionItem(prescriptionItem);
                }

                RecalculatePrice();
                _logger.LogInformation("成功导入历史处方");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入历史处方项时发生错误");
                ShowErrorMessage("导入失败", "导入历史处方项失败");
            }
        }

        /// <summary>
        /// 设置折扣
        /// </summary>
        private void SetDiscount(string? discountStr)
        {
            if (_dataManager == null || _validator == null) return;

            try
            {
                var validation = _validator.ValidateDiscount(discountStr ?? "1.0", out var discount);
                _dataManager.Discount = discount;
                _dataManager.MarkAsChanged();

                RecalculatePrice();

                if (!validation.IsValid || validation.Warnings.Any())
                {
                    ShowWarningMessage("折扣设置", validation.GetSummary());
                }

                _logger.LogDebug("设置折扣: {Discount}", discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置折扣时发生错误");
            }
        }

        /// <summary>
        /// 设置剂数
        /// </summary>
        private void SetDosage(string? dosageStr)
        {
            if (_dataManager == null || _validator == null) return;

            try
            {
                var validation = _validator.ValidateDosage(dosageStr ?? "7", out var dosage);
                _dataManager.DosageCount = dosage;
                _dataManager.MarkAsChanged();

                RecalculatePrice();

                if (!validation.IsValid || validation.Warnings.Any())
                {
                    ShowWarningMessage("剂数设置", validation.GetSummary());
                }

                _logger.LogDebug("设置剂数: {Dosage}", dosage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置剂数时发生错误");
            }
        }

        /// <summary>
        /// 生成处方编号
        /// </summary>
        private void GeneratePrescriptionNo()
        {
            if (_dataManager == null) return;

            try
            {
                _dataManager.GeneratePrescriptionNo();
                _logger.LogDebug("生成新的处方编号: {PrescriptionNo}", _dataManager.PrescriptionNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方编号时发生错误");
            }
        }

        /// <summary>
        /// 打印预览
        /// </summary>
        private async Task PrintPreviewAsync()
        {
            if (_dataManager == null) return;

            try
            {
                _logger.LogInformation("开始打印预览");

                var parameters = new DialogParameters
                {
                    { "PrescriptionData", CreatePrintData() }
                };

                _dialogService.ShowDialog("PrintPreviewDialog", parameters, result =>
                {
                    _logger.LogDebug("打印预览对话框关闭");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印预览时发生错误");
                ShowErrorMessage("打印预览失败", "无法打开打印预览");
            }
        }

        /// <summary>
        /// 验证处方
        /// </summary>
        private void ValidatePrescription()
        {
            if (_dataManager == null || _validator == null) return;

            try
            {
                var validation = _validator.ValidatePrescription(
                    _dataManager.PrescriptionItems,
                    _dataManager.PrescriptionNo,
                    _dataManager.DosageCount,
                    _dataManager.Usage,
                    _dataManager.Discount);

                if (!validation.IsValid)
                {
                    ShowErrorMessage("验证失败", validation.GetSummary());
                }
                else if (validation.Warnings.Any())
                {
                    ShowWarningMessage("验证警告", validation.GetSummary());
                }
                else
                {
                    ShowInfoMessage("验证成功", "处方验证通过");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方时发生错误");
            }
        }

        /// <summary>
        /// 重新计算价格
        /// </summary>
        private void RecalculatePrice()
        {
            if (_dataManager == null || _calculator == null) return;

            try
            {
                // 更新每项的小计
                _calculator.UpdateItemSubtotals(_dataManager.PrescriptionItems);

                // 触发价格重算事件
                OnPriceRecalculated?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新计算价格时发生错误");
            }
        }

        #endregion

        #region 命令条件检查

        private bool CanExecuteSave()
        {
            return _dataManager != null && !_dataManager.IsLoading && _dataManager.PrescriptionItems.Count > 0;
        }

        private bool CanExecuteClear()
        {
            return _dataManager != null && !_dataManager.IsLoading;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 安全执行异步命令
        /// </summary>
        private async Task ExecuteCommandSafelyAsync(Func<Task> command)
        {
            try
            {
                await command();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行异步命令时发生错误");
                ShowErrorMessage("操作失败", "操作执行失败，请重试");
            }
        }

        /// <summary>
        /// 安全执行同步命令
        /// </summary>
        private void ExecuteCommandSafely(Action command)
        {
            try
            {
                command();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行同步命令时发生错误");
                ShowErrorMessage("操作失败", "操作执行失败，请重试");
            }
        }

        /// <summary>
        /// 创建打印数据
        /// </summary>
        private object CreatePrintData()
        {
            if (_dataManager == null || _calculator == null)
                return new { };

            var calculation = _calculator.CalculatePrescriptionPrice(
                _dataManager.PrescriptionItems, _dataManager.DosageCount, _dataManager.Discount);

            return new
            {
                PrescriptionNo = _dataManager.PrescriptionNo,
                DosageCount = _dataManager.DosageCount,
                Usage = _dataManager.Usage,
                MedicalAdvice = _dataManager.MedicalAdvice,
                Remark = _dataManager.Remark,
                Items = _dataManager.PrescriptionItems.ToList(),
                Calculation = calculation
            };
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string title, string message)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message }
            };
            _dialogService.ShowDialog("ErrorDialog", parameters, (Action<IDialogResult>)null!);
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        private void ShowWarningMessage(string title, string message)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message }
            };
            _dialogService.ShowDialog("WarningDialog", parameters, (Action<IDialogResult>)null!);
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        private void ShowInfoMessage(string title, string message)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message }
            };
            _dialogService.ShowDialog("InfoDialog", parameters, (Action<IDialogResult>)null!);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        private bool ShowConfirmDialog(string title, string message)
        {
            var result = false;
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message }
            };

            _dialogService.ShowDialog("ConfirmDialog", parameters, dialogResult =>
            {
                result = dialogResult.Result == ButtonResult.OK;
            });

            return result;
        }

        #endregion

        #region 事件

        public event Action? OnPrescriptionSaved;
        public event Action? OnPrescriptionCleared;
        public event Action? OnPriceRecalculated;

        #endregion
    }
}