using AutoMapper;
using LYBT.Infrastructure.Services;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供处方记录的只读查询功能、价格计算和打印格式生成
    /// 所有Write操作必须通过MedicalCaseService聚合根进行
    /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代跨模块Repository依赖
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly ICrossModuleQueryService _crossModuleQuery;
        private readonly IPrescriptionNumberService _numberService;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            ICrossModuleQueryService crossModuleQuery,
            IPrescriptionNumberService numberService,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _crossModuleQuery = crossModuleQuery;
            _numberService = numberService;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<Result<PrescriptionDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含处方项
                var entity = await _repository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    return Result<PrescriptionDetailDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDetailDto>(entity);
                return Result<PrescriptionDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败");
                return Result<PrescriptionDetailDto>.Failure("获取处方详情失败");
            }
        }

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync, CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        public async Task<Result<List<PrescriptionDetailDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // 使用优化后的查询方法，直接查询并包含Items集合
                var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

                // 转换为DTO
                var prescriptionDtos = _mapper.Map<List<PrescriptionDetailDto>>(prescriptions);

                return Result<List<PrescriptionDetailDto>>.Success(prescriptionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病历相关处方时发生错误，病历ID：{MedicalCaseId}", medicalCaseId);
                return Result<List<PrescriptionDetailDto>>.Failure($"获取病历相关处方失败：{ex.Message}");
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
                var itemTotal = item.UnitPrice * item.Dosage * dosageCount;
                total += itemTotal;
            }

            // 应用折扣
            return total * discount;
        }

        /// <summary>
        /// 加载关联数据（OpenSpec: decouple-server-modules 重构版）
        /// 使用ICrossModuleQueryService按需批量查询，避免全量加载
        /// MedicalCaseBasicDto已包含TCMDiagnosis，无需单独查询Consultation
        /// </summary>
        /// <param name="medicalCaseIds">需要加载的医案ID集合</param>
        /// <returns>医案基本信息字典（含TCMDiagnosis）</returns>
        private async Task<Dictionary<Guid, LYBT.Shared.Models.Contracts.Common.MedicalCaseBasicDto>> LoadMedicalCasesAsync(
            IEnumerable<Guid> medicalCaseIds)
        {
            return await _crossModuleQuery.GetMedicalCasesBasicInfoAsync(medicalCaseIds);
        }

        /// <summary>
        /// 批量加载患者基本信息
        /// </summary>
        /// <param name="patientIds">患者ID集合</param>
        /// <returns>患者基本信息字典</returns>
        private async Task<Dictionary<Guid, LYBT.Shared.Models.Contracts.Common.PatientBasicDto>> LoadPatientsAsync(
            IEnumerable<Guid> patientIds)
        {
            return await _crossModuleQuery.GetPatientsBasicInfoAsync(patientIds);
        }


        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService按需批量查询
        /// MVP实现：内存过滤，适用于小数据量（<1000条处方）
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">症状/诊断关键字（可空）</param>
        /// <returns>处方搜索结果列表</returns>
        public async Task<Result<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
            string? patientName = null,
            string? symptomKeyword = null)
        {
            try
            {
                // 如果两个参数都为空，返回空列表
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return Result<List<PrescriptionSearchResultDto>>.Success(new List<PrescriptionSearchResultDto>());
                }

                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();

                // OpenSpec: decouple-server-modules - 批量加载关联数据
                var medicalCaseIds = allPrescriptions.Select(p => p.MedicalCaseId).Distinct().ToList();
                var medicalCaseDict = await LoadMedicalCasesAsync(medicalCaseIds);

                // 收集需要查询的患者ID
                var patientIds = medicalCaseDict.Values.Select(mc => mc.PatientId).Distinct().ToList();
                var patientDict = await LoadPatientsAsync(patientIds);

                // 内存过滤与关联
                var searchResults = new List<PrescriptionSearchResultDto>();

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历（含TCMDiagnosis）
                    if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    {
                        continue; // 找不到关联病历，跳过
                    }

                    // 关联患者
                    if (!patientDict.TryGetValue(medicalCase.PatientId, out var patient))
                    {
                        continue; // 找不到关联患者，跳过
                    }

                    // 按患者姓名筛选
                    if (!string.IsNullOrWhiteSpace(patientName))
                    {
                        if (string.IsNullOrEmpty(patient.Name) || !patient.Name.Contains(patientName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // 患者姓名不匹配，跳过
                        }
                    }

                    // 按症状/诊断关键字筛选
                    if (!string.IsNullOrWhiteSpace(symptomKeyword))
                    {
                        var matchedInDiagnosis = !string.IsNullOrEmpty(medicalCase.TCMDiagnosis) &&
                            medicalCase.TCMDiagnosis.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase);

                        var matchedInIndication = !string.IsNullOrEmpty(prescription.Indication) &&
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
                        PatientName = patient.Name,
                        Indication = prescription.Indication,
                        TCMDiagnosis = medicalCase.TCMDiagnosis,
                        DosageCount = prescription.DosageCount,
                        Advice = prescription.Advice,
                        FormulaSource = prescription.FormulaSource,
                        Remark = prescription.Remark
                    });
                }

                _logger.LogInformation("处方搜索完成，患者姓名：{PatientName}，症状关键字：{SymptomKeyword}，结果数量：{Count}",
                    patientName ?? "(空)", symptomKeyword ?? "(空)", searchResults.Count);

                return Result<List<PrescriptionSearchResultDto>>.Success(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方时发生错误，患者姓名：{PatientName}，症状关键字：{SymptomKeyword}",
                    patientName ?? "(空)", symptomKeyword ?? "(空)");
                return Result<List<PrescriptionSearchResultDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService按需批量查询
        /// MVP实现：内存过滤，适用于小数据量（<1000条处方）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条）</param>
        /// <returns>患者最近处方列表（按日期倒序）</returns>
        public async Task<Result<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            int count = 5)
        {
            try
            {
                // OpenSpec: decouple-server-modules - 使用CrossModuleQueryService验证患者
                var patient = await _crossModuleQuery.GetPatientBasicInfoAsync(patientId);
                if (patient == null)
                {
                    return Result<List<PrescriptionSearchResultDto>>.Failure("患者不存在");
                }

                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();

                // OpenSpec: decouple-server-modules - 批量加载关联数据
                var medicalCaseIds = allPrescriptions.Select(p => p.MedicalCaseId).Distinct().ToList();
                var medicalCaseDict = await LoadMedicalCasesAsync(medicalCaseIds);

                // 内存过滤：找到该患者的处方
                var targetPrescriptionIds = new List<Guid>();

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历（含TCMDiagnosis）
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

                // 批量查询处方详情，解决N+1查询问题
                var prescriptionsWithItems = await _repository.GetByIdsWithItemsAsync(targetPrescriptionIds);
                var prescriptionsDict = prescriptionsWithItems.ToDictionary(p => p.Id);

                // 构建搜索结果
                var patientPrescriptions = new List<PrescriptionSearchResultDto>();

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

                    // 从批量查询结果中获取处方详情
                    var prescriptionWithItems = prescriptionsDict.GetValueOrDefault(prescription.Id);
                    var herbCount = prescriptionWithItems?.Items?.Count ?? 0;

                    // 构建搜索结果 - TCMDiagnosis从MedicalCaseBasicDto获取
                    var prescriptionDto = new PrescriptionSearchResultDto
                    {
                        Id = prescription.Id,
                        CreatedAt = prescription.CreatedAt,
                        PatientId = patient.Id,
                        PatientName = patient.Name ?? string.Empty,
                        Indication = prescription.Indication,
                        TCMDiagnosis = medicalCase.TCMDiagnosis,
                        DosageCount = prescription.DosageCount,
                        Advice = prescription.Advice,
                        FormulaSource = prescription.FormulaSource,
                        Remark = prescription.Remark,
                        HerbCount = herbCount,
                        Items = prescriptionWithItems?.Items != null
                            ? _mapper.Map<List<PrescriptionItemDto>>(prescriptionWithItems.Items)
                            : new List<PrescriptionItemDto>()
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

                return Result<List<PrescriptionSearchResultDto>>.Success(recentPrescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者最近处方时发生错误，患者ID：{PatientId}", patientId);
                return Result<List<PrescriptionSearchResultDto>>.Failure($"获取患者最近处方失败：{ex.Message}");
            }
        }
    }
}
