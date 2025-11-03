using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Formula.Interfaces; // Issue #1786: 为SelectFormulaDialogViewModel提供Formula查询功能
using LYBT.Desktop.Herbs.Interfaces; // Issue #1786: 为HerbSelectionDialogViewModel提供Herb查询功能
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common; // Issue #1786: 为ApiResponse和PagedResult添加命名空间
using LYBT.Shared.Models.Contracts.Formula; // Issue #1786: 为Formula查询添加DTO命名空间
using LYBT.Shared.Models.Contracts.Herbs; // Issue #1786: 为Herb查询添加DTO命名空间
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{

    /// <summary>
    /// 处方数据管理器 - UltraThink专门化组件
    /// 职责单一：专注处方数据的CRUD操作和状态管理
    /// 代码干净：清晰的数据管理接口
    /// 性能出色：优化的数据加载和缓存策略
    /// </summary>
    public class PrescriptionDataManager
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IHerbRepository _herbRepository; // Issue #1786: 为HerbSelectionDialogViewModel提供Herb查询功能
        private readonly IFormulaRepository _formulaRepository; // Issue #1786: 为SelectFormulaDialogViewModel提供Formula查询功能
        private readonly ILogger<PrescriptionDataManager> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRegionManager _regionManager;
        private readonly ISessionManager? _sessionManager;
        private readonly IUserNotificationService? _userNotificationService;

        public PrescriptionDataManager(
            IPrescriptionApi prescriptionApi,
            IMedicalCaseRepository medicalCaseRepository,
            IHerbRepository herbRepository, // Issue #1786: 为HerbSelectionDialogViewModel提供Herb查询功能
            IFormulaRepository formulaRepository, // Issue #1786: 为SelectFormulaDialogViewModel提供Formula查询功能
            ILogger<PrescriptionDataManager> logger,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
        {
            _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository)); // Issue #1786
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository)); // Issue #1786
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _sessionManager = sessionManager;
            _userNotificationService = userNotificationService;
        }

        #region 核心数据属性

        public Guid MedicalCaseId { get; private set; }
        public Guid PrescriptionId { get; private set; }
        public PrescriptionDto? CurrentPrescription { get; private set; }
        public bool IsNewPrescription { get; private set; } = true;
        public string PrescriptionNo { get; set; } = string.Empty;

        /// <summary>
        /// 处方编号（服务端自动生成，格式：RX-YYYYMMDD-NNNN）
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        public string? PrescriptionNumber { get; private set; }

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
                // Issue #1608: 使用IPrescriptionApi替代IPrescriptionRepository
                _logger.LogInformation("开始加载处方数据，医疗案例ID: {MedicalCaseId}", MedicalCaseId);

                var response = await _prescriptionApi.GetPrescriptionsByMedicalCaseIdAsync(MedicalCaseId);
                var prescriptions = response.Data ?? new List<PrescriptionDto>();

                if (prescriptions != null && prescriptions.Any())
                {
                    var existingPrescription = prescriptions.First(); // 取第一个处方
                    CurrentPrescription = existingPrescription;
                    PrescriptionId = existingPrescription.Id;
                    IsNewPrescription = false;
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

                    // Issue #1551: 加载服务端生成的处方编号
                    PrescriptionNumber = existingPrescription.PrescriptionNumber;

                    // 加载处方项
                    PrescriptionItems.Clear();
                    if (existingPrescription.Items != null)
                    {
                        foreach (var item in existingPrescription.Items)
                        {
                            var viewModel = new PrescriptionItemViewModel(
                                _eventAggregator,
                                _loggerFactory,
                                _regionManager,
                                _sessionManager,
                                _userNotificationService)
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
                    Items = PrescriptionItems.Select(item => new PrescriptionItemInputDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Remark = item.Remark
                    }).ToList()
                };

                // Issue #1608: 使用IMedicalCaseRepository.CreatePrescriptionAsync替代IPrescriptionRepository.CreateAsync
                var result = await _medicalCaseRepository.CreatePrescriptionAsync(MedicalCaseId, prescriptionCreateDto);
                if (result != null)
                {
                    // Issue #1551: 保存后更新服务端生成的处方编号
                    PrescriptionNumber = result.PrescriptionNumber;
                    PrescriptionId = result.Id;
                    CurrentPrescription = result;
                    IsNewPrescription = false;

                    HasChanges = false;
                    _logger.LogInformation("处方数据保存成功，处方编号: {PrescriptionNumber}", PrescriptionNumber);
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
        /// 分页获取处方列表（支持关键字查询）
        /// Issue #1786: 为PrescriptionsMainViewModel提供统计查询功能
        /// </summary>
        public virtual async Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null)
        {
            try
            {
                _logger.LogDebug("分页获取处方列表: Page={Page}, PageSize={PageSize}, Keyword={Keyword}", page, pageSize, keyword);
                var response = await _prescriptionApi.GetPrescriptionsAsync(page, pageSize, keyword);
                _logger.LogInformation("处方列表加载成功: TotalCount={TotalCount}, CurrentPage={Page}",
                    response.Data?.TotalCount, page);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取处方列表失败: Page={Page}", page);
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取处方详情（API查询方法）
        /// Issue #1786: 为PrescriptionEditorDialogViewModel提供查询功能
        /// </summary>
        public virtual async Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid prescriptionId)
        {
            try
            {
                _logger.LogDebug("获取处方详情: PrescriptionId={PrescriptionId}", prescriptionId);
                var response = await _prescriptionApi.GetPrescriptionByIdAsync(prescriptionId);
                _logger.LogInformation("处方详情加载成功: PrescriptionId={PrescriptionId}, Success={Success}",
                    prescriptionId, response.Success);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败: PrescriptionId={PrescriptionId}", prescriptionId);
                throw;
            }
        }

        /// <summary>
        /// 更新处方（Repository方法）
        /// Issue #1786: 为PrescriptionEditorDialogViewModel提供更新功能
        /// </summary>
        public virtual async Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)
        {
            try
            {
                _logger.LogDebug("更新处方: MedicalCaseId={MedicalCaseId}", medicalCaseId);
                var result = await _medicalCaseRepository.UpdatePrescriptionAsync(medicalCaseId, dto);
                _logger.LogInformation("处方更新成功: MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败: MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }
        }

        /// <summary>
        /// 分页获取药材列表（Repository方法）
        /// Issue #1786: 为HerbSelectionDialogViewModel提供Herb查询功能
        /// </summary>
        public virtual async Task<PagedResult<HerbDto>> GetHerbsPagedAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                _logger.LogDebug("分页获取药材列表: Page={Page}, PageSize={PageSize}", page, pageSize);
                var result = await _herbRepository.GetPagedAsync(page, pageSize);
                _logger.LogInformation("药材列表加载成功: TotalCount={TotalCount}, CurrentPage={Page}",
                    result.TotalCount, page);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取药材列表失败: Page={Page}", page);
                throw;
            }
        }

        /// <summary>
        /// 分页获取验方列表（Repository方法）
        /// Issue #1786: 为SelectFormulaDialogViewModel提供Formula查询功能
        /// </summary>
        public virtual async Task<PagedResult<FormulaDto>> GetFormulasPagedAsync(int page = 1, int pageSize = int.MaxValue, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("分页获取验方列表: Page={Page}, PageSize={PageSize}, Keyword={Keyword}", page, pageSize, keyword);
                var result = await _formulaRepository.GetPagedAsync(page, pageSize, keyword);
                _logger.LogInformation("验方列表加载成功: TotalCount={TotalCount}, CurrentPage={Page}",
                    result.TotalCount, page);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取验方列表失败: Page={Page}", page);
                throw;
            }
        }

        /// <summary>
        /// 导入验方到处方（Repository方法）
        /// Issue #1786: 为FormulaTemplateDialogViewModel提供导入验方功能
        /// </summary>
        public virtual async Task ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId)
        {
            try
            {
                _logger.LogDebug("导入验方到处方: MedicalCaseId={MedicalCaseId}, FormulaId={FormulaId}", medicalCaseId, formulaId);
                await _medicalCaseRepository.ImportFormulaIntoPrescriptionAsync(medicalCaseId, formulaId);
                _logger.LogInformation("验方导入成功: FormulaId={FormulaId}", formulaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方失败: FormulaId={FormulaId}", formulaId);
                throw;
            }
        }

        /// <summary>
        /// 删除处方（Repository方法）
        /// Issue #1786: 为PrescriptionManagementViewModel提供删除处方功能
        /// </summary>
        public virtual async Task DeletePrescriptionAsync(Guid prescriptionId)
        {
            try
            {
                _logger.LogDebug("删除处方: PrescriptionId={PrescriptionId}", prescriptionId);
                await _medicalCaseRepository.DeletePrescriptionAsync(prescriptionId);
                _logger.LogInformation("处方删除成功: PrescriptionId={PrescriptionId}", prescriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败: PrescriptionId={PrescriptionId}", prescriptionId);
                throw;
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
