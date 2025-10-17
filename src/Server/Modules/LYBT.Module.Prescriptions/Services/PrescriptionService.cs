using System.Text;
using AutoMapper;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Formula.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using PrescriptionItemEntity = LYBT.Entities.Prescriptions.PrescriptionItem;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - 简化版，包含基础CRUD、价格计算和打印格式生成
    /// 支持四种开方方式的核心功能，保持价格计算准确性
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IConsultationService _consultationService;
        private readonly IPatientService _patientService;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IFormulaRepository formulaRepository,
            IConsultationService consultationService,
            IPatientService patientService,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _formulaRepository = formulaRepository;
            _consultationService = consultationService;
            _patientService = patientService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Items集合
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);

                // Issue #1163: 应用日期范围筛选（MVP阶段内存过滤）
                var filteredItems = pagedResult.Items.AsEnumerable();

                if (startDate.HasValue)
                {
                    filteredItems = filteredItems.Where(p => p.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // 结束日期包含当天全部（到23:59:59）
                    var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    filteredItems = filteredItems.Where(p => p.CreatedAt <= endOfDay);
                }

                var filteredList = filteredItems.ToList();

                var dto = new PagedResult<PrescriptionDto>
                {
                    Items = _mapper.Map<List<PrescriptionDto>>(filteredList),
                    TotalCount = startDate.HasValue || endDate.HasValue ? filteredList.Count : pagedResult.TotalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方列表失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("获取处方列表失败");
            }
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

        /// <summary>
        /// 创建处方 - 仅在独立创建时使用
        /// 注意：推荐通过MedicalCase聚合根创建完整的诊疗流程
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<PrescriptionEntity>(dto);

                // 注意：处方总价在DTO层计算，实体层不存储

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败");
                return ServiceResult<PrescriptionDto>.Failure("更新处方失败");
            }
        }

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

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败");
                return ServiceResult.Failure("删除处方失败");
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
        /// 重新计算处方价格（价格计算在DTO层处理）
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>带有计算价格的处方DTO</returns>
        public async Task<ServiceResult<PrescriptionDto>> RecalculatePriceAsync(Guid prescriptionId)
        {
            try
            {
                var entity = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                // 注意：实体层不存储总价，价格计算在DTO层进行
                var dto = _mapper.Map<PrescriptionDto>(entity);

                // 如果DTO有TotalAmount属性，可以在这里计算
                if (entity.Items?.Any() == true)
                {
                    var calculatedTotal = CalculateTotalAmount(entity.Items, entity.DosageCount, entity.Discount);
                    // DTO映射时会自动计算总价
                }

                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新计算处方价格失败");
                return ServiceResult<PrescriptionDto>.Failure("重新计算处方价格失败");
            }
        }

        /// <summary>
        /// 生成简化的处方打印格式
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>打印格式字符串</returns>
        public async Task<ServiceResult<string>> GeneratePrintFormatAsync(Guid prescriptionId)
        {
            try
            {
                var entity = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (entity == null)
                    return ServiceResult<string>.Failure("处方不存在");

                var printFormat = GenerateSimplePrintFormat(entity);
                return ServiceResult<string>.Success(printFormat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方打印格式失败");
                return ServiceResult<string>.Failure("生成处方打印格式失败");
            }
        }

        /// <summary>
        /// 生成简单的处方打印格式
        /// </summary>
        private string GenerateSimplePrintFormat(PrescriptionEntity prescription)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"处方编号: {prescription.Id}");
            sb.AppendLine($"开方日期: {prescription.CreatedAt:yyyy-MM-dd}");
            sb.AppendLine($"适应症: {prescription.Indication}");
            sb.AppendLine();
            sb.AppendLine("药材清单:");
            sb.AppendLine("序号	药材名称	数量	单位	单价	小计");
            sb.AppendLine(new string('-', 50));

            int index = 1;
            foreach (var item in prescription.Items ?? [])
            {
                var subtotal = item.UnitPrice * item.Quantity * prescription.DosageCount;
                sb.AppendLine($"{index}	{item.HerbName}	{item.Quantity}	{item.Unit}	{item.UnitPrice:F2}	{subtotal:F2}");
                index++;
            }

            // 计算总金额
            decimal totalAmount = 0;
            foreach (var item in prescription.Items ?? [])
            {
                totalAmount += item.UnitPrice * item.Quantity * prescription.DosageCount;
            }

            // 应用折扣
            var finalAmount = totalAmount * prescription.Discount;

            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"帖数: {prescription.DosageCount} 帖");
            if (prescription.Discount < 1.0m)
            {
                sb.AppendLine($"小计: {totalAmount:F2} 元");
                sb.AppendLine($"折扣: {prescription.Discount:P0}");
                sb.AppendLine($"总金额: {finalAmount:F2} 元");
            }
            else
            {
                sb.AppendLine($"总金额: {finalAmount:F2} 元");
            }

            if (!string.IsNullOrEmpty(prescription.Advice))
            {
                sb.AppendLine($"医嘱: {prescription.Advice}");
            }

            return sb.ToString();
        }

        #region Issue #1163: 新增功能

        /// <summary>
        /// 生成处方编号 (Issue #1163)
        /// 格式：RX + YYYYMMDD + 4位序号
        /// </summary>
        public async Task<ServiceResult<string>> GeneratePrescriptionNoAsync()
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                var prefix = "RX";

                // MVP阶段：简单数据库计数方案
                // 注意：高并发场景需要使用 Redis 计数器或数据库序列
                var allPrescriptions = await _repository.GetAllAsync();
                var todayPrescriptions = allPrescriptions
                    .Where(p => p.CreatedAt.Date == DateTime.Today)
                    .ToList();

                var sequence = todayPrescriptions.Count + 1;
                var prescriptionNo = $"{prefix}{today}{sequence:D4}";

                _logger.LogInformation("生成处方编号: {PrescriptionNo}", prescriptionNo);
                return ServiceResult<string>.Success(prescriptionNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成处方编号失败");
                return ServiceResult<string>.Failure("生成处方编号失败");
            }
        }

        /// <summary>
        /// 获取处方统计数据 (Issue #1163)
        /// </summary>
        public async Task<ServiceResult<PrescriptionMainStatisticsDto>> GetStatisticsAsync()
        {
            try
            {
                var allPrescriptions = await _repository.GetAllAsync();
                var today = DateTime.Today;

                // 包含处方项以计算金额
                var todayPrescriptionsWithItems = new List<PrescriptionEntity>();
                foreach (var p in allPrescriptions.Where(p => p.CreatedAt.Date == today))
                {
                    var withItems = await _repository.GetByIdWithItemsAsync(p.Id);
                    if (withItems != null)
                        todayPrescriptionsWithItems.Add(withItems);
                }

                // 计算今日总金额
                decimal todayTotalAmount = 0;
                foreach (var prescription in todayPrescriptionsWithItems)
                {
                    var itemsTotal = (prescription.Items ?? [])
                        .Sum(item => item.UnitPrice * item.Quantity * prescription.DosageCount);
                    todayTotalAmount += itemsTotal * prescription.Discount;
                }

                var statistics = new PrescriptionMainStatisticsDto
                {
                    TotalCount = allPrescriptions.Count(),
                    TodayCount = todayPrescriptionsWithItems.Count,
                    TodayTotalAmount = todayTotalAmount
                };

                return ServiceResult<PrescriptionMainStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方统计失败");
                return ServiceResult<PrescriptionMainStatisticsDto>.Failure("获取处方统计失败");
            }
        }

        /// <summary>
        /// 获取日期范围统计 (Issue #1163)
        /// </summary>
        public async Task<ServiceResult<PrescriptionRangeStatisticsDto>> GetRangeStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var allPrescriptions = await _repository.GetAllAsync();

                // 结束日期包含当天全部
                var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

                var rangePrescriptions = allPrescriptions
                    .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endOfDay)
                    .ToList();

                // 包含处方项以计算金额
                var prescriptionsWithItems = new List<PrescriptionEntity>();
                foreach (var p in rangePrescriptions)
                {
                    var withItems = await _repository.GetByIdWithItemsAsync(p.Id);
                    if (withItems != null)
                        prescriptionsWithItems.Add(withItems);
                }

                // 计算总金额
                decimal totalAmount = 0;
                foreach (var prescription in prescriptionsWithItems)
                {
                    var itemsTotal = (prescription.Items ?? [])
                        .Sum(item => item.UnitPrice * item.Quantity * prescription.DosageCount);
                    totalAmount += itemsTotal * prescription.Discount;
                }

                var statistics = new PrescriptionRangeStatisticsDto
                {
                    Count = prescriptionsWithItems.Count,
                    TotalAmount = totalAmount,
                    AvgAmount = prescriptionsWithItems.Count > 0 ? totalAmount / prescriptionsWithItems.Count : 0
                };

                return ServiceResult<PrescriptionRangeStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日期范围统计失败");
                return ServiceResult<PrescriptionRangeStatisticsDto>.Failure("获取日期范围统计失败");
            }
        }


        /// <summary>
        /// 克隆处方 - 复制处方并创建新实例 (Issue #1167)
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CloneAsync(Guid prescriptionId)
        {
            try
            {
                // 获取原始处方（包含药材项）
                var originalPrescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (originalPrescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("未找到要克隆的处方");
                }

                // 创建克隆处方
                var clonedPrescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = originalPrescription.MedicalCaseId,
                    PatientId = originalPrescription.PatientId,
                    UserId = originalPrescription.UserId,
                    Indication = originalPrescription.Indication,
                    DosageCount = originalPrescription.DosageCount,
                    Discount = originalPrescription.Discount,
                    Advice = originalPrescription.Advice,
                    FormulaSource = originalPrescription.FormulaSource,
                    Status = PrescriptionStatus.Draft, // 克隆的处方默认为草稿状态
                    Remark = originalPrescription.Remark,
                    PrintVersion = 1, // 重置打印版本
                    LastPrintedAt = null, // 清空打印时间
                    PrintCount = 0, // 重置打印次数
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    
                    // 复制药材项（稍后设置PrescriptionId）
                    Items = new List<PrescriptionItemEntity>()
                };

                var savedPrescription = await _repository.AddAsync(clonedPrescription);
                
                // 复制药材项
                if (originalPrescription.Items != null && originalPrescription.Items.Any())
                {
                    foreach (var item in originalPrescription.Items)
                    {
                        savedPrescription.Items.Add(new PrescriptionItemEntity
                        {
                            Id = Guid.NewGuid(),
                            PrescriptionId = savedPrescription.Id,
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Usage = item.Usage,
                            Remark = item.Remark
                        });
                    }
                }
                
                await _repository.SaveChangesAsync();

                var prescriptionDto = _mapper.Map<PrescriptionDto>(savedPrescription);
                return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆处方时发生错误，处方ID：{PrescriptionId}", prescriptionId);
                return ServiceResult<PrescriptionDto>.Failure($"克隆处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 导入验方到处方 - 校验验方状态 (Issue #1350)
        /// </summary>
        public async Task<ServiceResult> ImportFormulaIntoPrescriptionAsync(
            Guid prescriptionId,
            Guid formulaId)
        {
            try
            {
                // 获取验方
                var formula = await _formulaRepository.GetByIdAsync(formulaId);
                if (formula == null)
                {
                    return ServiceResult.Failure("验方不存在");
                }

                // 检查验方状态
                if (formula.ValidationStatus == FormulaValidationStatus.Draft)
                {
                    var unvalidatedHerbs = formula.Herbs
                        .Where(h => !h.IsValidated)
                        .Select(h => h.OriginalHerbName)
                        .ToList();

                    return ServiceResult.Failure(
                        $"验方\"{formula.Name}\"包含未校验的药材，请先在验方管理中完成校验。未校验药材：{string.Join("、", unvalidatedHerbs)}");
                }

                // 获取处方
                var prescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (prescription == null)
                {
                    return ServiceResult.Failure("处方不存在");
                }

                // 导入药材到处方
                foreach (var herbItem in formula.Herbs)
                {
                    var prescriptionItem = new PrescriptionItemEntity
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = prescriptionId,
                        HerbId = herbItem.HerbId!.Value,  // Validated状态下HerbId必有值
                        HerbName = herbItem.OriginalHerbName,
                        Quantity = herbItem.Quantity,
                        Unit = herbItem.Unit,
                        UnitPrice = 0,  // 价格需要单独查询herb表或后续设置
                        Usage = herbItem.Usage ?? string.Empty,
                        Remark = string.Empty
                    };

                    prescription.Items.Add(prescriptionItem);
                }

                await _repository.UpdateAsync(prescription);

                return ServiceResult.Success($"验方\"{formula.Name}\"已导入到处方，共{formula.Herbs.Count}味药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方到处方时发生错误，处方ID：{PrescriptionId}，验方ID：{FormulaId}", prescriptionId, formulaId);
                return ServiceResult.Failure($"导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者最近处方列表 (ENTRY-13)
        /// 按日期倒序，默认返回5条
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            int count = 5)
        {
            try
            {
                _logger.LogInformation("开始获取患者最近处方列表：PatientId={PatientId}, Count={Count}", patientId, count);

                // 获取患者所有处方（包含处方项）
                var allPrescriptions = await _repository.GetAllAsync();
                var patientPrescriptions = allPrescriptions
                    .Where(p => p.PatientId == patientId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToList();

                // 构建搜索结果列表
                var results = new List<PrescriptionSearchResultDto>();

                foreach (var prescription in patientPrescriptions)
                {
                    // 获取处方详细信息（包含Items）
                    var prescriptionWithItems = await _repository.GetByIdWithItemsAsync(prescription.Id);
                    if (prescriptionWithItems == null) continue;

                    // 获取诊断信息（通过MedicalCaseId关联）
                    string? tcmDiagnosis = null;
                    if (prescription.MedicalCaseId != Guid.Empty)
                    {
                        // 通过 MedicalCaseId 获取诊断信息
                        // 注意：需要调用 Consultation Service 获取该 MedicalCase 的诊断信息
                        var consultationsResult = await _consultationService.GetByMedicalCaseIdAsync(prescription.MedicalCaseId);
                        if (consultationsResult.IsSuccess && consultationsResult.Data != null && consultationsResult.Data.Any())
                        {
                            // 获取第一个诊疗记录的中医诊断
                            tcmDiagnosis = consultationsResult.Data.First().TCMDiagnosis;
                        }
                    }

                    // 转换处方项
                    var itemDtos = _mapper.Map<List<PrescriptionItemDto>>(prescriptionWithItems.Items);

                    // 构建搜索结果DTO
                    var searchResult = new PrescriptionSearchResultDto
                    {
                        PrescriptionId = prescription.Id,
                        PatientName = string.Empty, // 需要从患者信息中获取，暂时留空
                        PrescriptionDate = prescription.CreatedAt,
                        TCMDiagnosis = tcmDiagnosis,
                        HerbCount = prescriptionWithItems.Items?.Count ?? 0,
                        Items = itemDtos
                    };

                    results.Add(searchResult);
                }

                _logger.LogInformation("成功获取患者最近处方列表，共{Count}条", results.Count);
                return ServiceResult<List<PrescriptionSearchResultDto>>.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者最近处方列表失败：PatientId={PatientId}", patientId);
                return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"获取患者最近处方列表失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 搜索处方 (ENTRY-14)
        /// 支持按患者姓名或症状/诊断关键词搜索（OR逻辑）
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
            string? patientName = null,
            string? symptomKeyword = null)
        {
            try
            {
                _logger.LogInformation("开始搜索处方：PatientName={PatientName}, SymptomKeyword={SymptomKeyword}",
                    patientName, symptomKeyword);

                // 如果两个参数都为空，返回空列表
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    _logger.LogWarning("搜索参数均为空，返回空列表");
                    return ServiceResult<List<PrescriptionSearchResultDto>>.Success(new List<PrescriptionSearchResultDto>());
                }

                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();
                var matchedPrescriptions = new List<PrescriptionSearchResultDto>();

                foreach (var prescription in allPrescriptions)
                {
                    bool matchesPatientName = false;
                    bool matchesSymptom = false;

                    // 检查患者姓名匹配
                    if (!string.IsNullOrWhiteSpace(patientName) && prescription.PatientId.HasValue)
                    {
                        var patientResult = await _patientService.GetByIdAsync(prescription.PatientId.Value);
                        if (patientResult.IsSuccess && patientResult.Data != null)
                        {
                            matchesPatientName = patientResult.Data.Name?.Contains(patientName, StringComparison.OrdinalIgnoreCase) ?? false;
                        }
                    }

                    // 检查症状/诊断匹配
                    if (!string.IsNullOrWhiteSpace(symptomKeyword) && prescription.MedicalCaseId != Guid.Empty)
                    {
                        var consultationsResult = await _consultationService.GetByMedicalCaseIdAsync(prescription.MedicalCaseId);
                        if (consultationsResult.IsSuccess && consultationsResult.Data != null && consultationsResult.Data.Any())
                        {
                            var consultation = consultationsResult.Data.First();
                            matchesSymptom = consultation.TCMDiagnosis?.Contains(symptomKeyword, StringComparison.OrdinalIgnoreCase) ?? false;
                        }
                    }

                    // OR逻辑：任一条件满足即可
                    bool matches = (string.IsNullOrWhiteSpace(patientName) || matchesPatientName) &&
                                   (string.IsNullOrWhiteSpace(symptomKeyword) || matchesSymptom);

                    // 如果有任一搜索条件，至少要满足一个
                    if (!string.IsNullOrWhiteSpace(patientName) && !string.IsNullOrWhiteSpace(symptomKeyword))
                    {
                        matches = matchesPatientName || matchesSymptom;
                    }
                    else
                    {
                        matches = matchesPatientName || matchesSymptom;
                    }

                    if (matches)
                    {
                        // 获取处方详细信息
                        var prescriptionWithItems = await _repository.GetByIdWithItemsAsync(prescription.Id);
                        if (prescriptionWithItems == null) continue;

                        // 获取患者姓名
                        string patientNameValue = string.Empty;
                        if (prescription.PatientId.HasValue)
                        {
                            var patientResult = await _patientService.GetByIdAsync(prescription.PatientId.Value);
                            if (patientResult.IsSuccess && patientResult.Data != null)
                            {
                                patientNameValue = patientResult.Data.Name ?? string.Empty;
                            }
                        }

                        // 获取诊断信息
                        string? tcmDiagnosis = null;
                        if (prescription.MedicalCaseId != Guid.Empty)
                        {
                            var consultationsResult = await _consultationService.GetByMedicalCaseIdAsync(prescription.MedicalCaseId);
                            if (consultationsResult.IsSuccess && consultationsResult.Data != null && consultationsResult.Data.Any())
                            {
                                tcmDiagnosis = consultationsResult.Data.First().TCMDiagnosis;
                            }
                        }

                        // 转换处方项
                        var itemDtos = _mapper.Map<List<PrescriptionItemDto>>(prescriptionWithItems.Items);

                        // 构建搜索结果DTO
                        var searchResult = new PrescriptionSearchResultDto
                        {
                            PrescriptionId = prescription.Id,
                            PatientName = patientNameValue,
                            PrescriptionDate = prescription.CreatedAt,
                            TCMDiagnosis = tcmDiagnosis,
                            HerbCount = prescriptionWithItems.Items?.Count ?? 0,
                            Items = itemDtos
                        };

                        matchedPrescriptions.Add(searchResult);
                    }
                }

                // 按日期倒序排列
                var sortedResults = matchedPrescriptions.OrderByDescending(p => p.PrescriptionDate).ToList();

                _logger.LogInformation("搜索处方完成，找到{Count}条匹配结果", sortedResults.Count);
                return ServiceResult<List<PrescriptionSearchResultDto>>.Success(sortedResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方失败：PatientName={PatientName}, SymptomKeyword={SymptomKeyword}",
                    patientName, symptomKeyword);
                return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 复制处方到新处方（支持跨患者）(ENTRY-15)
        /// 将源处方的所有药材复制到目标处方
        /// </summary>
        public async Task<ServiceResult> ClonePrescriptionAsync(
            Guid sourcePrescriptionId,
            Guid targetPrescriptionId)
        {
            try
            {
                _logger.LogInformation("开始复制处方：SourceId={SourceId}, TargetId={TargetId}",
                    sourcePrescriptionId, targetPrescriptionId);

                // 获取源处方（包含药材项）
                var source = await _repository.GetByIdWithItemsAsync(sourcePrescriptionId);
                if (source == null)
                {
                    return ServiceResult.Failure("源处方不存在");
                }

                // 获取目标处方
                var target = await _repository.GetByIdWithItemsAsync(targetPrescriptionId);
                if (target == null)
                {
                    return ServiceResult.Failure("目标处方不存在");
                }

                // 复制所有药材到目标处方
                foreach (var item in source.Items ?? [])
                {
                    var newItem = new PrescriptionItemEntity
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = targetPrescriptionId,
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Usage = item.Usage,
                        Remark = item.Remark
                    };
                    target.Items.Add(newItem);
                }

                // 如果源处方引用了验方，也复制到目标处方
                if (!string.IsNullOrWhiteSpace(source.FormulaSource))
                {
                    if (string.IsNullOrWhiteSpace(target.FormulaSource))
                    {
                        target.FormulaSource = source.FormulaSource;
                    }
                    else
                    {
                        // 合并验方来源，避免重复
                        var existingSources = target.FormulaSource
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToList();

                        var sourceSources = source.FormulaSource
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !existingSources.Contains(s))
                            .ToList();

                        if (sourceSources.Any())
                        {
                            target.FormulaSource = $"{target.FormulaSource},{string.Join(",", sourceSources)}";
                        }
                    }
                }

                // 更新目标处方
                await _repository.UpdateAsync(target);

                var herbCount = source.Items?.Count ?? 0;
                _logger.LogInformation("处方复制成功，共复制{Count}味药材", herbCount);
                return ServiceResult.Success($"已从历史处方复制{herbCount}味药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制处方时发生错误：SourceId={SourceId}, TargetId={TargetId}",
                    sourcePrescriptionId, targetPrescriptionId);
                return ServiceResult.Failure($"复制失败：{ex.Message}");
            }
        }

        #endregion
    }
}
