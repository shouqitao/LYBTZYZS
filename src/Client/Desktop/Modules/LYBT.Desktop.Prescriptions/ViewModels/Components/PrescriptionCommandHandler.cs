using System.Windows.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Services.Print;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方命令处理器 - UltraThink架构实现
    /// 负责处理处方相关的业务命令
    /// </summary>
    public class PrescriptionCommandHandler
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IPrescriptionPrintService _printService;
        private readonly ILogger<PrescriptionCommandHandler> _logger;
        private readonly IDialogService _dialogService;

        #region 事件定义

        /// <summary>
        /// 价格重算事件
        /// </summary>
        public event Action? OnPriceRecalculated;

        /// <summary>
        /// 处方保存成功事件
        /// </summary>
        public event Action? OnPrescriptionSaved;

        /// <summary>
        /// 处方清空事件
        /// </summary>
        public event Action? OnPrescriptionCleared;

        /// <summary>
        /// 验方导入成功事件 (Issue #1368 ENTRY-10)
        /// </summary>
        public event Action? OnFormulaImported;
        #endregion

        #region 命令定义

        /// <summary>
        /// 重新计算命令
        /// </summary>
        public ICommand RecalculateCommand { get; private set; }

        /// <summary>
        /// 打印预览命令
        /// </summary>
        public ICommand PrintPreviewCommand { get; private set; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 清空命令
        /// </summary>
        public ICommand ClearCommand { get; private set; }

        /// <summary>
        /// 添加药材命令
        /// </summary>
        public ICommand AddHerbCommand { get; private set; }

        /// <summary>
        /// 移除药材命令
        /// </summary>
        public ICommand RemoveHerbCommand { get; private set; }

        /// <summary>
        /// 导入验方命令
        /// </summary>
        public ICommand ImportFormulaCommand { get; private set; }

        /// <summary>
        /// 生成处方编号命令
        /// </summary>
        public ICommand GeneratePrescriptionNoCommand { get; private set; }

        /// <summary>
        /// 验证命令
        /// </summary>
        public ICommand ValidateCommand { get; private set; }

        #endregion

        #region 依赖字段

        private PrescriptionDataManager? _dataManager;
        private PrescriptionValidator? _validator;
        private PrescriptionCalculator? _calculator;
        private ISessionManager? _sessionManager;

        #endregion

        public PrescriptionCommandHandler(
            IPrescriptionApi prescriptionApi,
            IMedicalCaseRepository medicalCaseRepository,
            IPrescriptionPrintService printService,
            ILogger<PrescriptionCommandHandler> logger,
            ISessionManager sessionManager,
            IDialogService dialogService)
        {
            _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化命令
            RecalculateCommand = new DelegateCommand(ExecuteRecalculate);
            PrintPreviewCommand = new DelegateCommand(ExecutePrintPreview);
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            ClearCommand = new DelegateCommand(ExecuteClear, CanExecuteClear);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
            RemoveHerbCommand = new DelegateCommand(ExecuteRemoveHerb);
            ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula);
            GeneratePrescriptionNoCommand = new DelegateCommand(ExecuteGeneratePrescriptionNo);
            ValidateCommand = new DelegateCommand(ExecuteValidate);
        }

        /// <summary>
        /// 设置依赖
        /// </summary>
        public void SetDependencies(
            PrescriptionDataManager dataManager,
            PrescriptionValidator validator,
            PrescriptionCalculator calculator)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        #region 处方CRUD操作

        /// <summary>
        /// 创建处方
        /// </summary>
        public async Task<CommandResult<PrescriptionDto>> CreatePrescriptionAsync(
            Guid medicalCaseId,
            string prescriptionNumber,
            Guid patientId,
            string patientName,
            string doctorName,
            IEnumerable<PrescriptionItemViewModel> items,
            string? notes = null)
        {
            try
            {
                _logger.LogInformation("开始创建处方:{PrescriptionNumber}", prescriptionNumber);

                // 创建处方DTO
                var createDto = new PrescriptionCreateDto
                {
                    PrescriptionNumber = prescriptionNumber,
                    PatientId = patientId,
                    PatientName = patientName,
                    DoctorName = doctorName,
                    Notes = notes,
                    Items = ConvertToCreateItems(items)
                };

                var prescription = await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, createDto);
                _logger.LogInformation("处方创建成功:{PrescriptionId}", prescription.Id);
                return CommandResult<PrescriptionDto>.Success(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方时发生异常:{PrescriptionNumber}", prescriptionNumber);
                return CommandResult<PrescriptionDto>.Failure("创建处方时发生系统错误");
            }
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        public async Task<CommandResult<PrescriptionDto>> UpdatePrescriptionAsync(
            Guid prescriptionId,
            string prescriptionNumber,
            IEnumerable<PrescriptionItemViewModel> items,
            string? notes = null)
        {
            try
            {
                _logger.LogInformation("开始更新处方:{PrescriptionId}", prescriptionId);

                var updateDto = new PrescriptionUpdateDto
                {
                    PrescriptionNumber = prescriptionNumber,
                    Notes = notes,
                    Items = ConvertToUpdateItems(items)
                };

                var prescription = await _medicalCaseRepository.UpdatePrescriptionAsync(prescriptionId, updateDto);
                _logger.LogInformation("处方更新成功:{PrescriptionId}", prescriptionId);
                return CommandResult<PrescriptionDto>.Success(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方时发生异常:{PrescriptionId}", prescriptionId);
                return CommandResult<PrescriptionDto>.Failure("更新处方时发生系统错误");
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        public async Task<CommandResult<bool>> DeletePrescriptionAsync(Guid prescriptionId)
        {
            try
            {
                _logger.LogInformation("开始删除处方:{PrescriptionId}", prescriptionId);

                await _medicalCaseRepository.DeletePrescriptionAsync(prescriptionId);
                _logger.LogInformation("处方删除成功:{PrescriptionId}", prescriptionId);
                return CommandResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方时发生异常:{PrescriptionId}", prescriptionId);
                return CommandResult<bool>.Failure("删除处方时发生系统错误");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行重新计算
        /// </summary>
        private void ExecuteRecalculate()
        {
            _logger.LogInformation("执行价格重新计算");
            OnPriceRecalculated?.Invoke();
        }

        /// <summary>
        /// 执行打印预览
        /// Issue #1381: PRINT-4 集成打印服务
        /// </summary>
        private async void ExecutePrintPreview()
        {
            _logger.LogInformation("执行打印预览");

            try
            {
                // MVP阶段:需要先保存处方才能打印
                // TODO: PRINT-5优化 - 支持未保存处方的打印预览
                if (_dataManager == null || _dataManager.PrescriptionId == Guid.Empty)
                {
                    _logger.LogWarning("无法打印预览:处方未保存");
                    // TODO: 使用通知服务提示用户
                    return;
                }

                // 获取完整的处方数据 (Issue #1608: 使用IPrescriptionApi)
                var response = await _prescriptionApi.GetPrescriptionByIdAsync(_dataManager.PrescriptionId);
                var prescription = response.Data;
                if (prescription == null)
                {
                    _logger.LogWarning("无法打印预览:未找到处方 ID={PrescriptionId}", _dataManager.PrescriptionId);
                    return;
                }

                // 调用打印服务进行预览
                await _printService.PreviewPrescriptionAsync(prescription);
                _logger.LogInformation("打印预览完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印预览失败");
                // TODO: 使用通知服务显示错误信息
            }
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private Task ExecuteSaveAsync()
        {
            _logger.LogInformation("执行保存处方");
            OnPrescriptionSaved?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行清空
        /// </summary>
        private void ExecuteClear()
        {
            _logger.LogInformation("执行清空处方");
            OnPrescriptionCleared?.Invoke();
        }

        /// <summary>
        /// 执行添加药材
        /// </summary>
        private void ExecuteAddHerb()
        {
            _logger.LogInformation("执行添加药材");
        }

        /// <summary>
        /// 执行移除药材
        /// </summary>
        private void ExecuteRemoveHerb()
        {
            _logger.LogInformation("执行移除药材");
        }

        /// <summary>
        /// 执行导入验方 (Issue #1368 ENTRY-10)
        /// </summary>
        private void ExecuteImportFormula()
        {
            if (_dataManager?.PrescriptionId == null || _dataManager.PrescriptionId == Guid.Empty)
            {
                _logger.LogWarning("无法导入验方：处方ID无效");
                return;
            }

            _logger.LogInformation("打开验方模板对话框，处方ID: {PrescriptionId}", _dataManager.PrescriptionId);

            var parameters = new DialogParameters
            {
                { "PrescriptionId", _dataManager.PrescriptionId },
                { "MedicalCaseId", _dataManager.MedicalCaseId } // Epic #1600 Phase 5
            };

            _dialogService.ShowDialog("FormulaTemplateDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("Imported"))
                {
                    var formulaName = result.Parameters.GetValue<string>("FormulaName");
                    _logger.LogInformation("验方 \"{FormulaName}\" 导入成功", formulaName);
                    OnFormulaImported?.Invoke();
                }
            });
        }

        /// <summary>
        /// 执行生成处方编号
        /// </summary>
        private void ExecuteGeneratePrescriptionNo()
        {
            _logger.LogInformation("执行生成处方编号");
        }

        /// <summary>
        /// 执行验证
        /// </summary>
        private void ExecuteValidate()
        {
            _logger.LogInformation("执行处方验证");
        }

        /// <summary>
        /// 可以执行保存
        /// </summary>
        private bool CanExecuteSave() => true;

        /// <summary>
        /// 可以执行清空
        /// </summary>
        private bool CanExecuteClear() => true;

        /// <summary>
        /// 转换为创建项列表
        /// </summary>
        private List<PrescriptionItemCreateDto> ConvertToCreateItems(IEnumerable<PrescriptionItemViewModel>? items)
        {
            if (items == null) return new List<PrescriptionItemCreateDto>();

            return items.Select(i => new PrescriptionItemCreateDto
            {
                HerbId = i.HerbId,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Remark = i.Remark
            }).ToList();
        }

        /// <summary>
        /// 转换为更新项列表
        /// </summary>
        private List<PrescriptionItemUpdateDto> ConvertToUpdateItems(IEnumerable<PrescriptionItemViewModel>? items)
        {
            if (items == null) return new List<PrescriptionItemUpdateDto>();

            return items.Select(i => new PrescriptionItemUpdateDto
            {
                HerbId = i.HerbId,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Dosage = i.Quantity, // 临时使用数量作为剂量
                Remark = i.Remark
            }).ToList();
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除处方
        /// </summary>
        public async Task<CommandResult<bool>> BatchDeletePrescriptionsAsync(IEnumerable<Guid> prescriptionIds)
        {
            try
            {
                var ids = prescriptionIds?.ToList() ?? new List<Guid>();
                _logger.LogInformation("开始批量删除处方,数量:{Count}", ids.Count);

                if (!ids.Any())
                {
                    return CommandResult<bool>.Failure("没有选择要删除的处方");
                }

                // 循环调用DeleteAsync（IMedicalCaseRepository使用prescriptionId作为medicalCaseId）
                int successCount = 0;
                int failureCount = 0;
                foreach (var id in ids)
                {
                    try
                    {
                        await _medicalCaseRepository.DeletePrescriptionAsync(id);
                        successCount++;
                    }
                    catch
                    {
                        failureCount++;
                    }
                }

                if (failureCount == 0)
                {
                    _logger.LogInformation("批量删除处方成功,数量:{Count}", ids.Count);
                    return CommandResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogWarning("批量删除处方部分失败:成功 {SuccessCount} 个,失败 {FailureCount} 个", successCount, failureCount);
                    return CommandResult<bool>.Failure($"批量删除完成:成功 {successCount} 个,失败 {failureCount} 个");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除处方时发生异常");
                return CommandResult<bool>.Failure("批量删除处方时发生系统错误");
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        public async Task<CommandResult<IEnumerable<PrescriptionDto>>> GetPrescriptionsByPatientAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("开始获取患者处方列表:{PatientId}", patientId);

                var response = await _prescriptionApi.GetPrescriptionsAsync(1, int.MaxValue, null);
                var prescriptions = response.Data?.Items
                    .Where(p => p.PatientId == patientId)
                    .ToList() ?? new List<PrescriptionDto>();
                _logger.LogInformation("获取患者处方列表成功,数量:{Count}", prescriptions.Count);
                return CommandResult<IEnumerable<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方列表时发生异常:{PatientId}", patientId);
                return CommandResult<IEnumerable<PrescriptionDto>>.Failure("获取处方列表时发生系统错误");
            }
        }

        #endregion
    }

    /// <summary>
    /// 命令执行结果
    /// </summary>
    public class CommandResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }

        private CommandResult(bool isSuccess, T? data, string? errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static CommandResult<T> Success(T data)
        {
            return new CommandResult<T>(true, data, null);
        }

        public static CommandResult<T> Failure(string errorMessage)
        {
            return new CommandResult<T>(false, default, errorMessage);
        }
    }
}
