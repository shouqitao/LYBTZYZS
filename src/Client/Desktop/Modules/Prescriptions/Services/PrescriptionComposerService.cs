using AutoMapper;
using LYBT.Desktop.Prescriptions.Components;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{

    /// <summary>
    /// 处方编辑器服务 - UltraThink简化版本
    /// 专注于处方编辑相关的业务逻辑，不包含复杂的协调功能
    /// </summary>
    public class PrescriptionComposerService : IPrescriptionComposerService
    {

        #region 私有字段

        private readonly IMapper _mapper;
        private readonly IPrescriptionService _prescriptionService;
        private readonly PriceCalculator _priceCalculator;
        private readonly BasicValidator _basicValidator;
        private readonly ILogger<PrescriptionComposerService> _logger;

        #endregion 私有字段

        #region 构造函数

        public PrescriptionComposerService(
            IMapper mapper,
            IPrescriptionService prescriptionService,
            PriceCalculator priceCalculator,
            BasicValidator basicValidator,
            ILogger<PrescriptionComposerService> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _priceCalculator = priceCalculator ?? throw new ArgumentNullException(nameof(priceCalculator));
            _basicValidator = basicValidator ?? throw new ArgumentNullException(nameof(basicValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion 构造函数

        #region 处方编辑核心功能

        /// <summary>
        /// 创建新的处方草稿
        /// </summary>
        /// <param name="medicalCaseId">医疗案例ID</param>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <returns>处方信息</returns>
        public async Task<PrescriptionDto> CreateDraftAsync(Guid medicalCaseId, Guid patientId, Guid doctorId)
        {
            try
            {
                _logger.LogInformation(
                    "创建处方草稿: 医疗案例={MedicalCaseId}, 患者={PatientId}, 医生={DoctorId}",
                    medicalCaseId, patientId, doctorId);

                var prescription = new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCaseId,
                    PatientId = patientId,
                    UserId = doctorId,
                    DosageCount = 7, // 默认7剂
                    Usage = "水煎服，日一剂，分早晚服", // 默认用法
                    DosageForm = "汤剂", // 默认剂型
                    Status = 0, // 草稿状态
                    CreateTime = DateTime.Now
                };

                return await Task.FromResult(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方草稿时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 保存处方草稿
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>保存结果</returns>
        public async Task<(bool Success, string Message)> SaveDraftAsync(PrescriptionDto prescription)
        {
            try
            {
                _logger.LogInformation("保存处方草稿: {PrescriptionId}", prescription.Id);

                // 基础验证
                if (string.IsNullOrWhiteSpace(prescription.Diagnosis))
                {
                    return (false, "诊断不能为空");
                }

                // 设置草稿状态
                prescription.Status = CommonStatus.Disabled;
                prescription.UpdateTime = DateTime.Now;

                // 计算价格信息
                var priceResult = _priceCalculator.CalculatePrescriptionPrice(prescription);

                _logger.LogDebug("处方草稿价格计算: {PriceResult}", priceResult);

                // 这里可以调用后端服务保存
                // var result = await _prescriptionService.SaveAsync(prescription);

                // 暂时模拟保存成功
                await Task.Delay(100);

                _logger.LogInformation("处方草稿保存成功: {PrescriptionId}", prescription.Id);
                return (true, "草稿保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方草稿时发生错误: {PrescriptionId}", prescription?.Id);
                return (false, "保存草稿失败");
            }
        }

        /// <summary>
        /// 保存正式处方
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>保存结果</returns>
        public async Task<(bool Success, string Message)> SavePrescriptionAsync(PrescriptionDto prescription)
        {
            try
            {
                _logger.LogInformation("保存正式处方: {PrescriptionId}", prescription.Id);

                // 完整验证
                var validationResult = _basicValidator.ValidatePrescription(prescription);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.Errors);
                    _logger.LogWarning("处方验证失败: {Errors}", errorMessage);
                    return (false, errorMessage);
                }

                // 设置正式状态
                prescription.Status = CommonStatus.Enabled;
                prescription.UpdateTime = DateTime.Now;

                // 生成处方编号
                if (string.IsNullOrWhiteSpace(prescription.PrescriptionNo))
                {
                    prescription.PrescriptionNo = await GeneratePrescriptionNoAsync();
                }

                // 计算并设置价格信息
                var priceResult = _priceCalculator.CalculatePrescriptionPrice(prescription);
                _logger.LogDebug("正式处方价格计算: {PriceResult}", priceResult);

                // 这里可以调用后端服务保存
                // var result = await _prescriptionService.SaveAsync(prescription);

                // 暂时模拟保存成功
                await Task.Delay(200);

                var message = validationResult.HasWarnings
                    ? $"处方保存成功，但有警告: {string.Join("; ", validationResult.Warnings)}"
                    : "处方保存成功";

                _logger.LogInformation("正式处方保存成功: {PrescriptionId}", prescription.Id);
                return (true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存正式处方时发生错误: {PrescriptionId}", prescription?.Id);
                return (false, "保存处方失败");
            }
        }

        /// <summary>
        /// 快速验证处方
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidatePrescription(PrescriptionDto prescription)
        {
            try
            {
                _logger.LogDebug("验证处方: {PrescriptionId}", prescription?.Id);
                if (prescription == null)
                {
                    var errorResult = new ValidationResult();
                    errorResult.AddError("处方信息不能为空");
                    return errorResult;
                }
                return _basicValidator.ValidatePrescription(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方时发生错误");
                var result = new ValidationResult();
                result.AddError("验证过程中发生错误");
                return result;
            }
        }

        /// <summary>
        /// 计算处方价格
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>价格计算结果</returns>
        public PriceCalculationResult CalculatePrice(PrescriptionDto prescription)
        {
            try
            {
                _logger.LogDebug("计算处方价格: {PrescriptionId}", prescription?.Id);
                if (prescription == null)
                {
                    return new PriceCalculationResult { IsSuccess = false, ErrorMessage = "处方信息不能为空" };
                }
                return _priceCalculator.CalculatePrescriptionPrice(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方价格时发生错误");
                return new PriceCalculationResult();
            }
        }

        #endregion 处方编辑核心功能

        #region 药材管理辅助功能

        /// <summary>
        /// 验证药材用量
        /// </summary>
        /// <param name="herbName">药材名称</param>
        /// <param name="quantity">用量</param>
        /// <returns>验证结果</returns>
        public (bool IsValid, string Message) ValidateHerbQuantity(string herbName, decimal quantity)
        {
            try
            {
                return _basicValidator.ValidateHerbQuantity(herbName, quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证药材用量时发生错误: {HerbName}", herbName);
                return (false, "验证过程中发生错误");
            }
        }

        /// <summary>
        /// 添加药材到处方
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <param name="herbItem">药材项目</param>
        /// <returns>添加结果</returns>
        public (bool Success, string Message) AddHerbToPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem)
        {
            try
            {
                if (prescription == null || herbItem == null)
                {
                    return (false, "参数不能为空");
                }

                // 检查是否已存在
                foreach (var existingItem in prescription.Items)
                {
                    if (existingItem.HerbId == herbItem.HerbId)
                    {
                        return (false, $"药材 {herbItem.HerbName} 已存在于处方中");
                    }
                }

                // 验证药材项目
                var quantityValidation = ValidateHerbQuantity(herbItem.HerbName, herbItem.Quantity);
                if (!quantityValidation.IsValid)
                {
                    return (false, quantityValidation.Message);
                }

                // 添加药材
                herbItem.Id = Guid.NewGuid();
                prescription.Items.Add(herbItem);

                _logger.LogInformation(
                    "已添加药材到处方: {HerbName}, 用量: {Quantity}{Unit}",
                    herbItem.HerbName, herbItem.Quantity, herbItem.Unit);

                return (true, quantityValidation.Message); // 成功，但可能有警告信息
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材到处方时发生错误: {HerbName}", herbItem?.HerbName);
                return (false, "添加药材失败");
            }
        }

        /// <summary>
        /// 从处方中移除药材
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <param name="herbItem">药材项目</param>
        /// <returns>移除结果</returns>
        public (bool Success, string Message) RemoveHerbFromPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem)
        {
            try
            {
                if (prescription == null || herbItem == null)
                {
                    return (false, "参数不能为空");
                }

                var removed = prescription.Items.Remove(herbItem);
                if (removed)
                {
                    _logger.LogInformation("已从处方中移除药材: {HerbName}", herbItem.HerbName);
                    return (true, "药材移除成功");
                }
                else
                {
                    return (false, "未找到要移除的药材");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方中移除药材时发生错误: {HerbName}", herbItem?.HerbName);
                return (false, "移除药材失败");
            }
        }

        #endregion 药材管理辅助功能

        #region 辅助方法

        /// <summary>
        /// 生成处方编号
        /// </summary>
        /// <returns>处方编号</returns>
        private async Task<string> GeneratePrescriptionNoAsync()
        {
            try
            {
                // 生成格式: CF + 年月日 + 4位序号
                var today = DateTime.Today;
                var datePrefix = today.ToString("yyyyMMdd");

                // 这里可以从数据库获取当日的序号
                // 暂时使用随机数模拟
                var sequence = new Random().Next(1, 9999);
                var prescriptionNo = $"CF{datePrefix}{sequence:D4}";

                _logger.LogDebug("生成处方编号: {PrescriptionNo}", prescriptionNo);

                return await Task.FromResult(prescriptionNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方编号时发生错误");
                return $"CF{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        #endregion 辅助方法
    }

    /// <summary>
    /// 处方编辑器服务接口
    /// </summary>
    public interface IPrescriptionComposerService
    {

        Task<PrescriptionDto> CreateDraftAsync(Guid medicalCaseId, Guid patientId, Guid doctorId);

        Task<(bool Success, string Message)> SaveDraftAsync(PrescriptionDto prescription);

        Task<(bool Success, string Message)> SavePrescriptionAsync(PrescriptionDto prescription);

        ValidationResult ValidatePrescription(PrescriptionDto prescription);

        PriceCalculationResult CalculatePrice(PrescriptionDto prescription);

        (bool IsValid, string Message) ValidateHerbQuantity(string herbName, decimal quantity);

        (bool Success, string Message) AddHerbToPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem);

        (bool Success, string Message) RemoveHerbFromPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem);
    }
}
