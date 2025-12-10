using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>处方数据管理器</summary>
    public class PrescriptionDataManager
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IHerbRepository _herbRepository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly ILogger<PrescriptionDataManager> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRegionManager _regionManager;
        private readonly ISessionManager? _sessionManager;
        private readonly IUserNotificationService? _userNotificationService;

        public PrescriptionDataManager(
            IPrescriptionApi prescriptionApi, IMedicalCaseRepository medicalCaseRepository,
            IHerbRepository herbRepository, IFormulaRepository formulaRepository,
            ILogger<PrescriptionDataManager> logger, IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory, IRegionManager regionManager,
            ISessionManager? sessionManager = null, IUserNotificationService? userNotificationService = null)
        {
            _prescriptionApi = prescriptionApi ?? throw new ArgumentNullException(nameof(prescriptionApi));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _sessionManager = sessionManager;
            _userNotificationService = userNotificationService;
        }

        public Guid MedicalCaseId { get; private set; }
        public Guid PrescriptionId { get; private set; }
        public PrescriptionDto? CurrentPrescription { get; private set; }
        public bool IsNewPrescription { get; private set; } = true;
        public string PrescriptionNo { get; set; } = string.Empty;
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

        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try { IsLoading = true; MedicalCaseId = medicalCaseId; GeneratePrescriptionNo(); await LoadExistingDataAsync(); HasChanges = false; }
            catch (Exception ex) { _logger.LogError(ex, "初始化处方数据失败"); throw; }
            finally { IsLoading = false; }
        }

        private async Task LoadExistingDataAsync()
        {
            try
            {
                var response = await _prescriptionApi.GetPrescriptionsByMedicalCaseIdAsync(MedicalCaseId);
                var prescriptions = response.Data ?? new List<PrescriptionDto>();
                if (prescriptions.Any())
                {
                    var p = prescriptions.First();
                    CurrentPrescription = p; PrescriptionId = p.Id; IsNewPrescription = false;
                    DosageCount = p.DosageCount; Usage = "水煎服，一日三次，饭后服用";
                    MedicalAdvice = p.Advice ?? string.Empty; Remark = p.Remark ?? string.Empty;
                    Discount = p.Discount; PrescriptionNumber = p.PrescriptionNumber;
                    PrescriptionItems.Clear();
                    if (p.Items != null)
                        foreach (var item in p.Items)
                            PrescriptionItems.Add(new PrescriptionItemViewModel(_eventAggregator, _loggerFactory, _regionManager, _sessionManager, _userNotificationService)
                            { HerbId = item.HerbId, HerbName = item.HerbName, Quantity = item.Quantity, Unit = item.Unit, UnitPrice = item.UnitPrice, Remark = item.Remark ?? string.Empty });
                }
                else ResetToDefault();
            }
            catch (Exception ex) { _logger.LogError(ex, "加载处方数据失败"); ResetToDefault(); throw; }
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                if (PrescriptionItems.Count == 0) return false;
                IsLoading = true;
                var dto = new PrescriptionCreateDto
                {
                    PatientId = Guid.Empty, DoctorId = Guid.Empty, ConsultationId = MedicalCaseId,
                    Diagnosis = "中医诊断", DosageCount = DosageCount, Quantity = DosageCount, Usage = Usage,
                    TotalAmount = PrescriptionItems.Sum(x => x.Quantity * x.UnitPrice) * DosageCount * Discount,
                    Advice = MedicalAdvice, Remark = Remark,
                    Items = PrescriptionItems.Select(i => new PrescriptionItemInputDto { HerbId = i.HerbId, HerbName = i.HerbName, Quantity = (int)i.Quantity, Unit = i.Unit, UnitPrice = i.UnitPrice, Remark = i.Remark }).ToList()
                };
                var result = await _medicalCaseRepository.CreatePrescriptionAsync(MedicalCaseId, dto);
                if (result != null) { PrescriptionNumber = result.PrescriptionNumber; PrescriptionId = result.Id; CurrentPrescription = result; IsNewPrescription = false; HasChanges = false; return true; }
                return false;
            }
            catch (Exception ex) { _logger.LogError(ex, "保存处方失败"); return false; }
            finally { IsLoading = false; }
        }

        public virtual async Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try { return await _prescriptionApi.GetPrescriptionsAsync(page, pageSize, keyword); }
            catch (Exception ex) { _logger.LogError(ex, "获取处方列表失败"); throw; }
        }

        public virtual async Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid prescriptionId)
        {
            try { return await _prescriptionApi.GetPrescriptionByIdAsync(prescriptionId); }
            catch (Exception ex) { _logger.LogError(ex, "获取处方详情失败"); throw; }
        }

        public virtual async Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)
        {
            try { return await _medicalCaseRepository.UpdatePrescriptionAsync(medicalCaseId, dto); }
            catch (Exception ex) { _logger.LogError(ex, "更新处方失败"); return null; }
        }

        public virtual async Task<PagedResult<HerbDto>> GetHerbsPagedAsync(int page = 1, int pageSize = 100)
        {
            try { return await _herbRepository.GetPagedAsync(page, pageSize); }
            catch (Exception ex) { _logger.LogError(ex, "获取药材列表失败"); throw; }
        }

        public virtual async Task<PagedResult<FormulaDto>> GetFormulasPagedAsync(int page = 1, int pageSize = int.MaxValue, string? keyword = null)
        {
            try { return await _formulaRepository.GetPagedAsync(page, pageSize, keyword); }
            catch (Exception ex) { _logger.LogError(ex, "获取验方列表失败"); throw; }
        }

        public virtual async Task ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId)
        {
            try { await _medicalCaseRepository.ImportFormulaIntoPrescriptionAsync(medicalCaseId, formulaId); }
            catch (Exception ex) { _logger.LogError(ex, "导入验方失败"); throw; }
        }

        public virtual async Task DeletePrescriptionAsync(Guid prescriptionId)
        {
            try { await _medicalCaseRepository.DeletePrescriptionAsync(prescriptionId); }
            catch (Exception ex) { _logger.LogError(ex, "删除处方失败"); throw; }
        }

        public virtual async Task<MedicalCaseDto?> GetMedicalCaseByIdAsync(Guid medicalCaseId)
        {
            try { return await _medicalCaseRepository.GetByIdAsync(medicalCaseId); }
            catch (Exception ex) { _logger.LogError(ex, "获取医案详情失败"); throw; }
        }

        public virtual async Task<List<HerbDto>?> SearchHerbsAsync(string keyword)
        {
            try { return await _herbRepository.SearchAsync(keyword); }
            catch (Exception ex) { _logger.LogError(ex, "搜索药材失败"); throw; }
        }

        public virtual async Task<ApiResponse<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(Guid patientId, int count)
        {
            try { return await _prescriptionApi.GetPatientRecentPrescriptionsAsync(patientId, count); }
            catch (Exception ex) { _logger.LogError(ex, "获取患者最近处方失败"); throw; }
        }

        public void Clear() { PrescriptionItems.Clear(); ResetToDefault(); HasChanges = true; }
        public void AddPrescriptionItem(PrescriptionItemViewModel item) { ArgumentNullException.ThrowIfNull(item); PrescriptionItems.Add(item); HasChanges = true; }
        public void RemovePrescriptionItem(PrescriptionItemViewModel? item) { if (item != null && PrescriptionItems.Contains(item)) { PrescriptionItems.Remove(item); HasChanges = true; } }
        public void MarkAsChanged() => HasChanges = true;
        public void GeneratePrescriptionNo() { PrescriptionNo = $"CF{DateTime.Now:yyyyMMddHHmmss}"; HasChanges = true; }

        private void ResetToDefault() { Usage = "水煎服，一日三次，饭后服用"; MedicalAdvice = Remark = string.Empty; DosageCount = 7; Discount = 1.0m; GeneratePrescriptionNo(); }
    }
}
