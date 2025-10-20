using System.Text;
using AutoMapper;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Consultation.Interfaces;
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
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IFormulaRepository formulaRepository,
            IMedicalCaseRepository medicalCaseRepository,
            IPatientRepository patientRepository,
            IConsultationRepository consultationRepository,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _formulaRepository = formulaRepository;
            _medicalCaseRepository = medicalCaseRepository;
            _patientRepository = patientRepository;
            _consultationRepository = consultationRepository;
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
        /// Issue #1423: RULE-2 - 一诊断一处方约束
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                // RULE-2: 检查该诊断是否已有处方（一诊断一处方约束）
                // 注意：Consultation使用共享主键（ConsultationId == MedicalCaseId）
                if (dto.ConsultationId.HasValue)
                {
                    var existingPrescriptions = await _repository.GetByMedicalCaseIdAsync(dto.ConsultationId.Value);
                    if (existingPrescriptions.Any())
                    {
                        _logger.LogWarning("创建处方失败：诊断 {ConsultationId} 已有处方", dto.ConsultationId.Value);
                        return ServiceResult<PrescriptionDto>.Failure("该诊断已有处方，不可重复创建");
                    }
                }

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
        /// 克隆处方 - 复制处方到指定诊疗记录 (Issue #1373 ENTRY-15)
        /// 支持从历史处方复制到新的诊疗记录/病历
        /// </summary>
        /// <param name="sourcePrescriptionId">源处方ID</param>
        /// <param name="targetConsultationId">目标诊疗记录ID（与MedicalCase共享主键）</param>
        /// <returns>新创建的处方DTO</returns>
        public async Task<ServiceResult<PrescriptionDto>> ClonePrescriptionAsync(
            Guid sourcePrescriptionId,
            Guid targetConsultationId)
        {
            try
            {
                // 获取源处方（包含药材项）
                var sourcePrescription = await _repository.GetByIdWithItemsAsync(sourcePrescriptionId);
                if (sourcePrescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("未找到要克隆的处方");
                }

                // 根据targetConsultationId找到目标MedicalCase（Consultation与MedicalCase共享主键）
                var targetMedicalCase = await _medicalCaseRepository.GetByIdAsync(targetConsultationId);
                if (targetMedicalCase == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure($"未找到目标诊疗记录对应的病历（ID: {targetConsultationId}）");
                }

                // 创建克隆处方，关联到目标病历
                var clonedPrescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = targetMedicalCase.Id, // 关联到目标病历
                    PatientId = targetMedicalCase.PatientId, // 使用目标病历的患者
                    UserId = sourcePrescription.UserId, // 保留原开方医生
                    Indication = sourcePrescription.Indication, // 保留适应症
                    DosageCount = sourcePrescription.DosageCount,
                    Discount = sourcePrescription.Discount,
                    Advice = sourcePrescription.Advice,

                    // TODO: ENTRY-7完成后，这里改为ReferencedFormulas字段
                    FormulaSource = sourcePrescription.FormulaSource, // 保留验方来源

                    Status = PrescriptionStatus.Draft, // 克隆的处方默认为草稿状态
                    Remark = sourcePrescription.Remark,
                    PrintVersion = 1, // 重置打印版本
                    LastPrintedAt = null, // 清空打印时间
                    PrintCount = 0, // 重置打印次数
                    IsPrinted = false, // 重置打印状态
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,

                    Items = new List<PrescriptionItemEntity>()
                };

                var savedPrescription = await _repository.AddAsync(clonedPrescription);

                // 克隆所有药材项
                if (sourcePrescription.Items != null && sourcePrescription.Items.Any())
                {
                    foreach (var item in sourcePrescription.Items)
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

                _logger.LogInformation("处方克隆成功，源处方ID：{SourceId}，目标诊疗ID：{TargetConsultationId}，新处方ID：{NewId}，药材数量：{ItemCount}",
                    sourcePrescriptionId, targetConsultationId, savedPrescription.Id, savedPrescription.Items.Count);

                var prescriptionDto = _mapper.Map<PrescriptionDto>(savedPrescription);
                return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆处方时发生错误，源处方ID：{SourceId}，目标诊疗ID：{TargetConsultationId}",
                    sourcePrescriptionId, targetConsultationId);
                return ServiceResult<PrescriptionDto>.Failure($"克隆处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 克隆处方（旧版） - 复制处方到同一病历 (Issue #1167)
        /// 已弃用，请使用 ClonePrescriptionAsync
        /// </summary>
        [Obsolete("请使用 ClonePrescriptionAsync(Guid sourcePrescriptionId, Guid targetConsultationId) 替代")]
        public async Task<ServiceResult<PrescriptionDto>> CloneAsync(Guid prescriptionId)
        {
            try
            {
                // 获取原始处方
                var originalPrescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (originalPrescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("未找到要克隆的处方");
                }

                // 调用新版克隆方法，克隆到同一MedicalCase
                return await ClonePrescriptionAsync(prescriptionId, originalPrescription.MedicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆处方时发生错误，处方ID：{PrescriptionId}", prescriptionId);
                return ServiceResult<PrescriptionDto>.Failure($"克隆处方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 导入验方到处方 - 校验验方状态 (Issue #1350, Issue #1366 ENTRY-8)
        /// 从已验证的验方批量导入药材，并记录引用的验方名称
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> ImportFormulaIntoPrescriptionAsync(
            Guid prescriptionId,
            Guid formulaId)
        {
            try
            {
                // 获取验方
                var formula = await _formulaRepository.GetByIdAsync(formulaId);
                if (formula == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("验方不存在");
                }

                // 检查验方状态
                if (formula.ValidationStatus == FormulaValidationStatus.Draft)
                {
                    var unvalidatedHerbs = formula.Herbs
                        .Where(h => !h.IsValidated)
                        .Select(h => h.OriginalHerbName)
                        .ToList();

                    return ServiceResult<PrescriptionDto>.Failure(
                        $"验方\"{formula.Name}\"包含未校验的药材，请先在验方管理中完成校验。未校验药材：{string.Join("、", unvalidatedHerbs)}");
                }

                // 获取处方
                var prescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
                if (prescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");
                }

                // 导入药材到处方
                foreach (var herbItem in formula.Herbs)
                {
                    var prescriptionItem = new PrescriptionItemEntity
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = prescriptionId,
                        HerbId = herbItem.HerbId!.Value,  // Validated状态下HerbId必有值
                        HerbName = herbItem.OriginalHerbName ?? string.Empty,  // Issue #1366: null安全
                        Quantity = herbItem.Quantity,
                        Unit = herbItem.Unit,
                        UnitPrice = 0,  // 价格需要单独查询herb表或后续设置
                        Usage = herbItem.Usage ?? string.Empty,
                        Remark = string.Empty
                    };

                    prescription.Items.Add(prescriptionItem);
                }

                // 更新ReferencedFormulas字段（追加验方名称，逗号分隔）(Issue #1366 ENTRY-8)
                if (string.IsNullOrWhiteSpace(prescription.ReferencedFormulas))
                {
                    prescription.ReferencedFormulas = formula.Name;
                }
                else if (!prescription.ReferencedFormulas.Split(',').Contains(formula.Name))
                {
                    // 避免重复添加相同验方名称
                    prescription.ReferencedFormulas += $",{formula.Name}";
                }

                await _repository.UpdateAsync(prescription);

                // 返回更新后的处方DTO (Issue #1366 ENTRY-8)
                var updatedPrescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
                var prescriptionDto = _mapper.Map<PrescriptionDto>(updatedPrescription);

                _logger.LogInformation("验方\"{FormulaName}\"已导入到处方，共{Count}味药材", formula.Name, formula.Herbs.Count);
                return ServiceResult<PrescriptionDto>.Success(
                    prescriptionDto,
                    $"验方\"{formula.Name}\"已导入到处方，共{formula.Herbs.Count}味药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方到处方时发生错误，处方ID：{PrescriptionId}，验方ID：{FormulaId}", prescriptionId, formulaId);
                return ServiceResult<PrescriptionDto>.Failure($"导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// MVP实现：内存过滤，适用于小数据量（<1000条处方）
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

                // 获取所有病历（用于关联患者）
                var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
                var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

                // 获取所有诊疗记录（用于获取 TCMDiagnosis）
                var allConsultations = await _consultationRepository.GetAllAsync();
                var consultationDict = allConsultations.ToDictionary(c => c.Id);

                // 获取所有患者（用于关联 PatientName）
                var allPatients = await _patientRepository.GetAllAsync();
                var patientDict = allPatients.ToDictionary(p => p.Id);

                // 内存过滤与关联
                var searchResults = new List<PrescriptionSearchResultDto>();

                foreach (var prescription in allPrescriptions)
                {
                    // 关联病历
                    if (!medicalCaseDict.TryGetValue(prescription.MedicalCaseId, out var medicalCase))
                    {
                        continue; // 找不到关联病历，跳过
                    }

                    // 关联患者
                    if (!patientDict.TryGetValue(medicalCase.PatientId, out var patient))
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
                // 获取所有处方
                var allPrescriptions = await _repository.GetAllAsync();

                // 获取所有病历（用于关联患者）
                var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
                var medicalCaseDict = allMedicalCases.ToDictionary(mc => mc.Id);

                // 获取所有诊疗记录（用于获取 TCMDiagnosis）
                var allConsultations = await _consultationRepository.GetAllAsync();
                var consultationDict = allConsultations.ToDictionary(c => c.Id);

                // 获取患者信息
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    return ServiceResult<List<PrescriptionSearchResultDto>>.Failure("患者不存在");
                }

                // 内存过滤：找到该患者的所有处方
                var patientPrescriptions = new List<PrescriptionSearchResultDto>();

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

                    // 关联诊疗记录（MedicalCase 与 Consultation 共享主键）
                    consultationDict.TryGetValue(medicalCase.Id, out var consultation);

                    // 获取处方项以计算药材数量（Issue #1370 ENTRY-12 新增需求）
                    var prescriptionWithItems = await _repository.GetByIdWithItemsAsync(prescription.Id);
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

        #endregion
    }
}
