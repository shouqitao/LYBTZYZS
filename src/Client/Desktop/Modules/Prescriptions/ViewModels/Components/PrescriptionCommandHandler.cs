using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方命令处理器 - UltraThink架构实现
    /// 负责处理处方相关的业务命令
    /// </summary>
    public class PrescriptionCommandHandler
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<PrescriptionCommandHandler> _logger;

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
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionCommandHandler> logger,
            ISessionManager sessionManager)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

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
            string prescriptionNumber,
            Guid patientId,
            string patientName,
            string doctorName,
            IEnumerable<PrescriptionItemViewModel> items,
            string? notes = null)
        {
            try
            {
                _logger.LogInformation("开始创建处方：{PrescriptionNumber}", prescriptionNumber);

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

                var result = await _prescriptionService.CreateAsync(createDto);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("处方创建成功：{PrescriptionId}", result.Data.Id);
                    return CommandResult<PrescriptionDto>.Success(result.Data);
                }
                else
                {
                    _logger.LogWarning("处方创建失败：{ErrorMessage}", result.ErrorMessage);
                    return CommandResult<PrescriptionDto>.Failure(result.ErrorMessage ?? "创建处方失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方时发生异常：{PrescriptionNumber}", prescriptionNumber);
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
                _logger.LogInformation("开始更新处方：{PrescriptionId}", prescriptionId);

                var updateDto = new PrescriptionUpdateDto
                {
                    PrescriptionNumber = prescriptionNumber,
                    Notes = notes,
                    Items = ConvertToUpdateItems(items)
                };

                var result = await _prescriptionService.UpdateAsync(prescriptionId, updateDto);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("处方更新成功：{PrescriptionId}", prescriptionId);
                    return CommandResult<PrescriptionDto>.Success(result.Data);
                }
                else
                {
                    _logger.LogWarning("处方更新失败：{ErrorMessage}", result.ErrorMessage);
                    return CommandResult<PrescriptionDto>.Failure(result.ErrorMessage ?? "更新处方失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方时发生异常：{PrescriptionId}", prescriptionId);
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
                _logger.LogInformation("开始删除处方：{PrescriptionId}", prescriptionId);

                var result = await _prescriptionService.DeleteAsync(prescriptionId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("处方删除成功：{PrescriptionId}", prescriptionId);
                    return CommandResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogWarning("处方删除失败：{ErrorMessage}", result.ErrorMessage);
                    return CommandResult<bool>.Failure(result.ErrorMessage ?? "删除处方失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方时发生异常：{PrescriptionId}", prescriptionId);
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
        /// </summary>
        private void ExecutePrintPreview()
        {
            _logger.LogInformation("执行打印预览");
            // 打印预览逻辑将在后续实现
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            _logger.LogInformation("执行保存处方");
            OnPrescriptionSaved?.Invoke();
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
        /// 执行导入验方
        /// </summary>
        private void ExecuteImportFormula()
        {
            _logger.LogInformation("执行导入验方");
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
                _logger.LogInformation("开始批量删除处方，数量：{Count}", ids.Count);

                if (!ids.Any())
                {
                    return CommandResult<bool>.Failure("没有选择要删除的处方");
                }

                // 循环调用DeleteAsync（Shared.Interfaces暂无BatchDeleteAsync）
                int successCount = 0;
                List<string> errors = new();
                foreach (var id in ids)
                {
                    var deleteResult = await _prescriptionService.DeleteAsync(id);
                    if (deleteResult.IsSuccess)
                        successCount++;
                    else if (!string.IsNullOrEmpty(deleteResult.ErrorMessage))
                        errors.Add(deleteResult.ErrorMessage);
                }
                var result = successCount == ids.Count
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(string.Join("; ", errors));

                if (result.IsSuccess)
                {
                    _logger.LogInformation("批量删除处方成功，数量：{Count}", ids.Count);
                    return CommandResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogWarning("批量删除处方失败：{ErrorMessage}", result.ErrorMessage);
                    return CommandResult<bool>.Failure(result.ErrorMessage ?? "批量删除处方失败");
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
                _logger.LogInformation("开始获取患者处方列表：{PatientId}", patientId);

                var result = await _prescriptionService.GetPagedAsync(1, int.MaxValue, null);

                if (result.IsSuccess && result.Data?.Items != null)
                {
                    var prescriptions = result.Data.Items
                        .Where(p => p.PatientId == patientId)
                        .ToList();
                    _logger.LogInformation("获取患者处方列表成功，数量：{Count}", prescriptions.Count);
                    return CommandResult<IEnumerable<PrescriptionDto>>.Success(prescriptions);
                }
                else
                {
                    _logger.LogWarning("获取患者处方列表失败：{ErrorMessage}", result.ErrorMessage);
                    return CommandResult<IEnumerable<PrescriptionDto>>.Failure(result.ErrorMessage ?? "获取处方列表失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方列表时发生异常：{PatientId}", patientId);
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