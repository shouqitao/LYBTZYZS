using AutoMapper;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供处方记录的只读查询功能、价格计算和打印格式生成
    /// 所有Write操作必须通过MedicalCaseService聚合根进行
    /// IMedicalCaseRepository用于Read关联患者信息（合法用途）
    /// Phase 3 (Epic #1725): 简化Service层，提取重复逻辑
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IPrescriptionNumberService _numberService;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IFormulaRepository formulaRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IPatientRepository patientRepository,
            IConsultationRepository consultationRepository,
            IPrescriptionNumberService numberService,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _formulaRepository = formulaRepository;
            _medicalCaseRepository = medicalCaseRepository;
            _patientRepository = patientRepository;
            _consultationRepository = consultationRepository;
            _numberService = numberService;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含处方项
                var entity = await _repository.GetByIdWithItemsAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDto>(entity);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败");
                return ServiceResult<PrescriptionDto>.Failure("获取处方详情失败");
            }
        }

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync, CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // 使用优化后的查询方法，直接查询并包含Items集合
                var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

                // 转换为DTO
                var prescriptionDtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

                return ServiceResult<List<PrescriptionDto>>.Success(prescriptionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病历相关处方时发生错误，病历ID：{MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取病历相关处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 计算处方总价 - 简化的价格计算逻辑
        /// </summary>
        /// <param name="items">处方项列表</param>
        /// <param name="dosageCount">处方帖数</param>
        /// <param name="discount">折扣</param>
        /// <returns>总价</returns>
        private decimal CalculateTotalAmount(IEnumerable<LYBT.Entities.Prescriptions.PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
        {
            decimal total = 0;

            foreach (var item in items)
            {
                // 基础价格计算：单价 × 数量 × 帖数
                var itemTotal = item.UnitPrice * item.Quantity * dosageCount;
                total += itemTotal;
            }

            // 应用折扣
            return total * discount;
        }

        /// <summary>
        /// 加载关联数据（提取重复逻辑 - Epic #1725 Phase 3）
        /// 统一的Dictionary构建方法，消除SearchPrescriptionsAsync和GetPatientRecentPrescriptionsAsync中的重复代码
        /// </summary>
        /// <param name="includePatients">是否加载所有患者数据</param>
        /// <returns>关联数据的Dictionary集合</returns>
        private async Task<(
            Dictionary<Guid, LYBT.Entities.MedicalCases.MedicalCase> medicalCases,
            Dictionary<Guid, LYBT.Entities.Consultations.Consultation> consultations,
            Dictionary<Guid, LYBT.Entities.Patients.Patient>? patients
        )> LoadRelatedDataAsync(bool includePatients)
        {
            // 加载病历
            var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
            var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

            // 加载诊疗记录
            var allConsultations = await _consultationRepository.GetAllAsync();
            var consultationDict = allConsultations.ToDictionary(c => c.Id);

            // 可选：加载患者
            Dictionary<Guid, LYBT.Entities.Patients.Patient>? patientDict = null;
            if (includePatients)
            {
                var allPatients = await _patientRepository.GetAllAsync();
                patientDict = allPatients.ToDictionary(p => p.Id);
            }

            return (medicalCaseDict, consultationDict, patientDict);
        }


        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// MVP实现：内存过滤，适用于小数据量（<1000条处方）
        /// Epic #1725 Phase 3: 使用LoadRelatedDataAsync提取重复逻辑
        ///  性能警告：全量加载 + 内存过滤，数据量增大后需优化为数据库层查询
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">症状/诊断关键字（可空）</param>
        /// <returns>处方搜索结果列表</returns>
        public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
            string? patientName = null,
            string? symptomKeyword = null)
        {
            try
            {
                // 如果两个参数都为空，返回空列表
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return ServiceResult<List<PrescriptionSearchResultDto>>.Success(new List<PrescriptionSearchResultDto>());
                }

                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();

                // Epic #1725 Phase 3: 使用统一方法加载关联数据（消除重复代码）
                var (medicalCaseDict, consultationDict, patientDict) = await LoadRelatedDataAsync(includePatients: true);

                // 内存过滤与关联
                var searchResults = new List<PrescriptionSearchResultDto>();

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历
                    if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    {
                        continue; // 找不到关联病历，跳过
                    }

                    // 关联患者（patientDict在MVP场景下必定不为null）
                    if (patientDict == null || !patientDict.TryGetValue(medicalCase.PatientId, out var patient))
                    {
                        continue; // 找不到关联患者，跳过
                    }

                    // 关联诊疗记录（MedicalCase 与 Consultation 共享主键）
                    consultationDict.TryGetValue(medicalCase.Id, out var consultation);

                    // 按患者姓名筛选
                    if (!string.IsNullOrWhiteSpace(patientName))
                    {
                        if (patient.Name == null || !patient.Name.Contains(patientName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // 患者姓名不匹配，跳过
                        }
                    }

                    // 按症状/诊断关键字筛选
                    if (!string.IsNullOrWhiteSpace(symptomKeyword))
                    {
                        var matchedInDiagnosis = consultation?.TCMDiagnosis != null &&
                            consultation.TCMDiagnosis.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase);

                        var matchedInIndication = prescription.Indication != null &&
                            prescription.Indication.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase);

                        if (!matchedInDiagnosis && !matchedInIndication)
                        {
                            continue; // 症状/诊断不匹配，跳过
                        }
                    }

                    // 构建搜索结果
                    searchResults.Add(new PrescriptionSearchResultDto
                    {
                        Id = prescription.Id,
                        CreatedAt = prescription.CreatedAt,
                        PatientId = patient.Id,
                        PatientName = patient.Name ?? string.Empty,
                        Indication = prescription.Indication,
                        TCMDiagnosis = consultation?.TCMDiagnosis,
                        DosageCount = prescription.DosageCount,
                        Advice = prescription.Advice,
                        FormulaSource = prescription.FormulaSource,
                        Remark = prescription.Remark
                    });
                }

                _logger.LogInformation("处方搜索完成，患者姓名：{PatientName}，症状关键字：{SymptomKeyword}，结果数量：{Count}",
                    patientName ?? "(空)", symptomKeyword ?? "(空)", searchResults.Count);

                return ServiceResult<List<PrescriptionSearchResultDto>>.Success(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方时发生错误，患者姓名：{PatientName}，症状关键字：{SymptomKeyword}",
                    patientName ?? "(空)", symptomKeyword ?? "(空)");
                return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// MVP实现：内存过滤，适用于小数据量（<1000条处方）
        /// Epic #1725 Phase 3: 使用LoadRelatedDataAsync提取重复逻辑 + 修复N+1查询
        ///  性能警告：全量加载 + 内存过滤，数据量增大后需优化为数据库层查询
        ///  N+1查询（已知MVP限制）：循环内查询处方Items，数据量增大后需优化
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条）</param>
        /// <returns>患者最近处方列表（按日期倒序）</returns>
        public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            int count = 5)
        {
            try
            {
                // 获取患者信息（先验证患者存在）
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    return ServiceResult<List<PrescriptionSearchResultDto>>.Failure("患者不存在");
                }

                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();

                // Epic #1725 Phase 3: 使用统一方法加载关联数据（消除重复代码）
                var (medicalCaseDict, consultationDict, _) = await LoadRelatedDataAsync(includePatients: false);

                // 内存过滤：找到该患者的处方
                var patientPrescriptions = new List<PrescriptionSearchResultDto>();
                var targetPrescriptionIds = new List<Guid>();

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历
                    if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    {
                        continue; // 找不到关联病历，跳过
                    }

                    // 筛选该患者的处方
                    if (medicalCase.PatientId != patientId)
                    {
                        continue; // 不是该患者的处方，跳过
                    }

                    // 收集需要查询详细信息的处方ID
                    targetPrescriptionIds.Add(prescription.Id);
                }

                // Task 1.5: 批量查询处方详情，解决N+1查询问题
                var prescriptionsWithItems = await _repository.GetByIdsWithItemsAsync(targetPrescriptionIds);
                var prescriptionsDict = prescriptionsWithItems.ToDictionary(p => p.Id);

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历（重新验证）
                    if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    {
                        continue;
                    }

                    // 筛选该患者的处方（重新验证）
                    if (medicalCase.PatientId != patientId)
                    {
                        continue;
                    }

                    // 关联诊疗记录（MedicalCase 与 Consultation 共享主键）
                    consultationDict.TryGetValue(medicalCase.Id, out var consultation);

                    //  优化后：从批量查询结果中获取处方详情
                    var prescriptionWithItems = prescriptionsDict.GetValueOrDefault(prescription.Id);
                    var herbCount = prescriptionWithItems?.Items?.Count ?? 0;

                    // 构建搜索结果
                    var prescriptionDto = new PrescriptionSearchResultDto
                    {
                        Id = prescription.Id,
                        CreatedAt = prescription.CreatedAt,
                        PatientId = patient.Id,
                        PatientName = patient.Name ?? string.Empty,
                        Indication = prescription.Indication,
                        TCMDiagnosis = consultation?.TCMDiagnosis,
                        DosageCount = prescription.DosageCount,
                        Advice = prescription.Advice,
                        FormulaSource = prescription.FormulaSource,
                        Remark = prescription.Remark,
                        HerbCount = herbCount, // Issue #1370 新增
                        Items = prescriptionWithItems?.Items != null
                            ? _mapper.Map<List<PrescriptionItemDto>>(prescriptionWithItems.Items)
                            : new List<PrescriptionItemDto>() // Issue #1370 新增
                    };

                    patientPrescriptions.Add(prescriptionDto);
                }

                // 按创建日期倒序排列，取前count条
                var recentPrescriptions = patientPrescriptions
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToList();

                _logger.LogInformation("获取患者最近处方完成，患者ID：{PatientId}，患者姓名：{PatientName}，请求数量：{RequestCount}，实际返回：{ActualCount}",
                    patientId, patient.Name ?? "(空)", count, recentPrescriptions.Count);

                return ServiceResult<List<PrescriptionSearchResultDto>>.Success(recentPrescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者最近处方时发生错误，患者ID：{PatientId}", patientId);
                return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"获取患者最近处方失败：{ex.Message}");
            }
        }
    }
}
