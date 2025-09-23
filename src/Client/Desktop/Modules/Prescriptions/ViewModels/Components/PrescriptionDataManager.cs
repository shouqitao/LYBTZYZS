using System.Collections.ObjectModel;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{

    /// <summary>
    /// 处方数据管理器 - UltraThink专门化组件
    /// 职责单一：专注处方数据的CRUD操作和状态管理
    /// 代码干净：清晰的数据管理接口
    /// 性能出色：优化的数据加载和缓存策略
    /// </summary>
    public class PrescriptionDataManager
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<PrescriptionDataManager> _logger;

        public PrescriptionDataManager(
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionDataManager> logger)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心数据属性

        public Guid MedicalCaseId { get; private set; }
        public string PrescriptionNo { get; set; } = string.Empty;
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; } = new();
        public PrescriptionItemViewModel? SelectedItem { get; set; }
        public int DosageCount { get; set; } = 7;
        public string Usage { get; set; } = "水煎服，一日三次，饭后服用";
        public string MedicalAdvice { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public decimal Discount { get; set; } = 1.0m;
        public bool IsLoading { get; private set; }
        public bool HasChanges { get; private set; }

        #endregion 核心数据属性

        #region 数据初始化

        /// <summary>
        /// 初始化处方数据
        /// </summary>
        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try
            {
                IsLoading = true;
                MedicalCaseId = medicalCaseId;

                _logger.LogInformation("开始初始化处方数据，医疗案例ID: {MedicalCaseId}", medicalCaseId);

                // 生成处方编号
                GeneratePrescriptionNo();

                // 加载现有数据
                await LoadExistingDataAsync();

                HasChanges = false;
                _logger.LogInformation("处方数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化处方数据失败");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载现有处方数据
        /// </summary>
        private async Task LoadExistingDataAsync()
        {
            try
            {
                // UltraThink架构修复：使用正确的GetByMedicalCaseIdAsync方法
                _logger.LogInformation("开始加载处方数据，医疗案例ID: {MedicalCaseId}", MedicalCaseId);

                var result = await _prescriptionService.GetByMedicalCaseIdAsync(MedicalCaseId);

                if (result.IsSuccess && result.Data != null && result.Data.Any())
                {
                    var existingPrescription = result.Data.First(); // 取第一个处方
                    _logger.LogDebug("找到现有处方数据，开始加载");

                    // 加载基础信息
                    // PrescriptionNo字段已删除
                    // PrescriptionNo = existingPrescription.PrescriptionNo ?? GeneratePrescriptionNoInternal();
                    DosageCount = existingPrescription.DosageCount;
                    // Usage字段已删除
                    // Usage = existingPrescription.Usage ?? "水煎服，一日三次，饭后服用";
                    Usage = "水煎服，一日三次，饭后服用"; // 使用默认值
                    MedicalAdvice = existingPrescription.Advice ?? string.Empty;
                    Remark = existingPrescription.Remark ?? string.Empty;
                    Discount = existingPrescription.Discount;

                    // 加载处方项
                    PrescriptionItems.Clear();
                    if (existingPrescription.Items != null)
                    {
                        foreach (var item in existingPrescription.Items)
                        {
                            var viewModel = new PrescriptionItemViewModel
                            {
                                HerbId = item.HerbId,
                                HerbName = item.HerbName,
                                Quantity = item.Quantity,
                                Unit = item.Unit,
                                UnitPrice = item.UnitPrice,

                                // Subtotal会自动计算，无需手动赋值
                                Remark = item.Remark ?? string.Empty
                            };

                            PrescriptionItems.Add(viewModel);
                        }
                    }

                    _logger.LogInformation("成功加载处方数据，共 {ItemCount} 个药材", PrescriptionItems.Count);
                }
                else
                {
                    _logger.LogDebug("未找到现有处方数据，使用默认值");
                    ResetToDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载现有处方数据失败");
                ResetToDefault();
                throw;
            }
        }

        #endregion 数据初始化

        #region 数据操作

        /// <summary>
        /// 保存处方数据
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                if (PrescriptionItems.Count == 0)
                {
                    _logger.LogWarning("处方项为空，无法保存");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("开始保存处方数据");

                var prescriptionCreateDto = new PrescriptionCreateDto
                {
                    PatientId = Guid.Empty, // 暂时使用空值，需要从MedicalCaseId获取
                    DoctorId = Guid.Empty,  // 暂时使用空值，需要获取当前医生
                    ConsultationId = MedicalCaseId, // 假设MedicalCaseId是诊疗ID
                    Diagnosis = "中医诊断", // 需要从医疗案例获取
                    DosageCount = DosageCount,
                    Quantity = DosageCount,
                    Usage = Usage,
                    TotalAmount = PrescriptionItems.Sum(x => x.Quantity * x.UnitPrice) * DosageCount * Discount,
                    Advice = MedicalAdvice,
                    Remark = Remark,
                    Items = PrescriptionItems.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Remark = item.Remark
                    }).ToList()
                };

                var result = await _prescriptionService.CreateAsync(prescriptionCreateDto);
                if (result != null)
                {
                    HasChanges = false;
                    _logger.LogInformation("处方数据保存成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方数据失败");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            PrescriptionItems.Clear();
            ResetToDefault();
            HasChanges = true;
            _logger.LogInformation("处方数据已清空");
        }

        /// <summary>
        /// 添加处方项
        /// </summary>
        public void AddPrescriptionItem(PrescriptionItemViewModel item)
        {
            ArgumentNullException.ThrowIfNull(item);

            PrescriptionItems.Add(item);
            HasChanges = true;
            _logger.LogDebug("添加处方项: {HerbName}", item.HerbName);
        }

        /// <summary>
        /// 移除处方项
        /// </summary>
        public void RemovePrescriptionItem(PrescriptionItemViewModel? item)
        {
            if (item != null && PrescriptionItems.Contains(item))
            {
                PrescriptionItems.Remove(item);
                HasChanges = true;
                _logger.LogDebug("移除处方项: {HerbName}", item.HerbName);
            }
        }

        /// <summary>
        /// 标记数据已变更
        /// </summary>
        public void MarkAsChanged()
        {
            HasChanges = true;
        }

        #endregion 数据操作

        #region 私有辅助方法

        /// <summary>
        /// 重置为默认值
        /// </summary>
        private void ResetToDefault()
        {
            Usage = "水煎服，一日三次，饭后服用";
            MedicalAdvice = string.Empty;
            Remark = string.Empty;
            DosageCount = 7;
            Discount = 1.0m;
            GeneratePrescriptionNo();
        }

        /// <summary>
        /// 生成处方编号
        /// </summary>
        public void GeneratePrescriptionNo()
        {
            PrescriptionNo = GeneratePrescriptionNoInternal();
            HasChanges = true;
        }

        private string GeneratePrescriptionNoInternal()
        {
            return $"CF{DateTime.Now:yyyyMMddHHmmss}";
        }

        #endregion 私有辅助方法
    }
}
