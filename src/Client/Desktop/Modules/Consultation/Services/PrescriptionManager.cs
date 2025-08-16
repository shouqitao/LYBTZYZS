using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
// UltraThink重构: 统一HerbInfo和HerbDto，使用Dto作为统一模型
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 处方管理器 - 负责处方的创建、编辑、验证和保存
    /// </summary>
    public class PrescriptionManager : IPrescriptionManager
    {
        #region 处方验证常量
        
        private const decimal MIN_HERB_QUANTITY = 0.1m;
        private const decimal MAX_HERB_QUANTITY = 1000m;
        private const decimal DEFAULT_HERB_QUANTITY = 10m;
        private const int MAX_PRESCRIPTION_ITEMS = 50;
        
        #endregion

        #region 依赖服务
        
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPrescriptionValidationService _validationService;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionManager> _logger;
        
        #endregion

        #region 处方数据
        
        private ObservableCollection<PrescriptionItemInfo> _prescriptionItems = new();
        private PrescriptionInfo? _currentPrescription;
        
        #endregion

        public PrescriptionManager(
            IPrescriptionService prescriptionService,
            IPrescriptionValidationService validationService,
            IMapper mapper,
            ILogger<PrescriptionManager> logger)
        {
            _prescriptionService = prescriptionService;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region 公共属性

        /// <summary>
        /// 当前处方项目集合
        /// </summary>
        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems => _prescriptionItems;

        /// <summary>
        /// 当前处方
        /// </summary>
        public PrescriptionInfo? CurrentPrescription
        {
            get => _currentPrescription;
            set => _currentPrescription = value;
        }

        /// <summary>
        /// 处方总价
        /// </summary>
        public decimal TotalPrice => CalculateTotalPrice();

        #endregion

        #region 处方项目操作

        /// <summary>
        /// 添加药材到处方
        /// </summary>
        public bool AddHerbToPrescription(HerbDto herb, decimal quantity = DEFAULT_HERB_QUANTITY)
        {
            try
            {
                // 验证处方项目数量
                if (_prescriptionItems.Count >= MAX_PRESCRIPTION_ITEMS)
                {
                    _logger.LogWarning($"处方项目数量已达上限 {MAX_PRESCRIPTION_ITEMS}");
                    return false;
                }

                // 检查是否已存在
                var existingItem = _prescriptionItems.FirstOrDefault(x => x.HerbId == herb.Id);
                if (existingItem != null)
                {
                    // 更新数量（Subtotal会自动重新计算）
                    existingItem.Quantity += quantity;
                    _logger.LogInformation($"更新药材 {herb.Name} 数量至 {existingItem.Quantity}");
                    return true;
                }

                // 创建新的处方项目
                var prescriptionItem = new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = quantity,
                    Unit = herb.Unit,
                    UnitPrice = herb.Price,
                    Usage = herb.Usage,
                    Remark = herb.Remark
                };

                // 验证处方项目
                if (!ValidatePrescriptionItem(prescriptionItem))
                {
                    return false;
                }

                _prescriptionItems.Add(prescriptionItem);
                _logger.LogInformation($"添加药材 {herb.Name} 到处方，数量: {quantity}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加药材 {herb.Name} 到处方时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 从处方中移除药材
        /// </summary>
        public bool RemoveHerbFromPrescription(Guid herbId)
        {
            try
            {
                var item = _prescriptionItems.FirstOrDefault(x => x.HerbId == herbId);
                if (item != null)
                {
                    _prescriptionItems.Remove(item);
                    _logger.LogInformation($"从处方中移除药材 {item.HerbName}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"移除药材时发生异常，HerbId: {herbId}");
                return false;
            }
        }

        /// <summary>
        /// 更新处方项目数量
        /// </summary>
        public bool UpdateHerbQuantity(Guid herbId, decimal newQuantity)
        {
            try
            {
                var item = _prescriptionItems.FirstOrDefault(x => x.HerbId == herbId);
                if (item != null)
                {
                    // 验证数量
                    if (newQuantity < MIN_HERB_QUANTITY || newQuantity > MAX_HERB_QUANTITY)
                    {
                        _logger.LogWarning($"药材数量 {newQuantity} 超出有效范围 [{MIN_HERB_QUANTITY}, {MAX_HERB_QUANTITY}]");
                        return false;
                    }

                    item.Quantity = newQuantity;
                    _logger.LogInformation($"更新药材 {item.HerbName} 数量至 {newQuantity} (Subtotal自动更新为 {item.Subtotal})");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新药材数量时发生异常，HerbId: {herbId}");
                return false;
            }
        }

        /// <summary>
        /// 清空处方
        /// </summary>
        public void ClearPrescription()
        {
            _prescriptionItems.Clear();
            _currentPrescription = null;
            _logger.LogInformation("已清空处方");
        }

        #endregion

        #region 处方保存

        /// <summary>
        /// 保存处方
        /// </summary>
        public async Task<bool> SavePrescriptionAsync(Guid consultationId, string diagnosis, string dosageForm, int quantity, string usage)
        {
            try
            {
                if (!_prescriptionItems.Any())
                {
                    _logger.LogWarning("处方为空，无法保存");
                    return false;
                }

                // 创建处方DTO
                var createDto = new PrescriptionCreateDto
                {
                    ConsultationId = consultationId,
                    Diagnosis = diagnosis,
                    DosageForm = dosageForm,
                    Quantity = quantity,
                    Usage = usage,
                    TotalAmount = CalculateTotalPrice(),
                    Items = _prescriptionItems.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        Usage = item.Usage,
                        Note = item.Remark
                    }).ToList()
                };

                // 调用服务保存处方
                var result = await _prescriptionService.CreateAsync(createDto);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation($"成功保存处方，包含 {_prescriptionItems.Count} 味药材");
                    return true;
                }

                _logger.LogWarning($"保存处方失败: {result.ErrorMessage}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生异常");
                return false;
            }
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证处方项目
        /// </summary>
        private bool ValidatePrescriptionItem(PrescriptionItemInfo item)
        {
            // 验证数量
            if (item.Quantity < MIN_HERB_QUANTITY || item.Quantity > MAX_HERB_QUANTITY)
            {
                _logger.LogWarning($"药材 {item.HerbName} 数量 {item.Quantity} 超出有效范围");
                return false;
            }

            // 验证价格
            if (item.UnitPrice <= 0)
            {
                _logger.LogWarning($"药材 {item.HerbName} 单价无效: {item.UnitPrice}");
                return false;
            }

            // 可以添加更多验证规则
            return true;
        }

        /// <summary>
        /// 验证整个处方
        /// </summary>
        public async Task<bool> ValidatePrescriptionAsync()
        {
            try
            {
                if (!_prescriptionItems.Any())
                {
                    _logger.LogWarning("处方为空");
                    return false;
                }

                // 使用验证服务进行验证
                var patientInfo = new PatientValidationInfo
                {
                    Age = 30, // 默认值，实际应用中需要从当前患者获取
                    Gender = "未知",
                    Allergies = new List<string>(),
                    MedicalHistory = new List<string>(),
                    CurrentMedications = new List<string>()
                };

                var validationResult = await _validationService.ValidatePrescriptionAsync(_prescriptionItems, patientInfo, "");
                
                if (!validationResult.CanPrescribe)
                {
                    _logger.LogWarning($"处方验证失败: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方时发生异常");
                return false;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算处方总价
        /// </summary>
        private decimal CalculateTotalPrice()
        {
            return _prescriptionItems.Sum(x => x.Subtotal);
        }

        /// <summary>
        /// 导入处方项目列表
        /// </summary>
        public void ImportPrescriptionItems(IEnumerable<PrescriptionItemInfo> items)
        {
            _prescriptionItems.Clear();
            foreach (var item in items)
            {
                if (ValidatePrescriptionItem(item))
                {
                    _prescriptionItems.Add(item);
                }
            }
            _logger.LogInformation($"导入 {_prescriptionItems.Count} 个处方项目");
        }

        #endregion
    }
}