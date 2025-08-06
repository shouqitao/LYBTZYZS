using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.TreatmentPlan.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentPlan;
using LYBT.Models.TreatmentPlan;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.TreatmentPlan.Services
{
    /// <summary>
    /// 治疗方案服务实现
    /// </summary>
    public class TreatmentPlanService : ITreatmentPlanService
    {
        private readonly LYBT.Infrastructure.Data.AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TreatmentPlanService> _logger;

        public TreatmentPlanService(
            AppDbContext context,
            IMapper mapper,
            ILogger<TreatmentPlanService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TreatmentPlanDetailDto?> GetByIdAsync(Guid id)
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .ThenInclude(tpp => tpp.Prescription)
                .Include(tp => tp.TreatmentItems)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (treatmentPlan == null)
                return null;

            var dto = _mapper.Map<TreatmentPlanDetailDto>(treatmentPlan);

            // 获取患者和医生信息
            var medicalCase = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .FirstOrDefaultAsync(mc => mc.Id == treatmentPlan.MedicalCaseId);

            if (medicalCase != null)
            {
                dto.PatientId = medicalCase.PatientId;
                dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
            }

            return dto;
        }

        public async Task<List<TreatmentPlanDto>> GetListAsync()
        {
            var treatmentPlans = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .OrderByDescending(tp => tp.CreateTime)
                .Take(100)
                .ToListAsync();

            var dtos = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans);

            // 批量获取医疗案例信息
            var medicalCaseIds = treatmentPlans.Select(tp => tp.MedicalCaseId).Distinct().ToList();
            var medicalCases = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .Where(mc => medicalCaseIds.Contains(mc.Id))
                .ToDictionaryAsync(mc => mc.Id, mc => mc);

            foreach (var dto in dtos)
            {
                if (medicalCases.TryGetValue(dto.MedicalCaseId, out var medicalCase))
                {
                    dto.PatientId = medicalCase.PatientId;
                    dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                    dto.DoctorId = medicalCase.DoctorId;
                    dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
                }
            }

            return dtos;
        }

        public async Task<PaginatedResult<TreatmentPlanDto>> GetPagedAsync(TreatmentPlanQueryDto query)
        {
            var queryable = _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .AsQueryable();

            // 条件过滤
            if (query.MedicalCaseId.HasValue)
                queryable = queryable.Where(tp => tp.MedicalCaseId == query.MedicalCaseId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(tp => tp.Status == query.Status);

            if (query.StartDate.HasValue)
                queryable = queryable.Where(tp => tp.CreateTime >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                queryable = queryable.Where(tp => tp.CreateTime <= query.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
            {
                var keyword = query.SearchKeyword.Trim().ToLower();
                queryable = queryable.Where(tp =>
                    tp.PlanName.ToLower().Contains(keyword) ||
                    tp.TreatmentObjective.ToLower().Contains(keyword) ||
                    (tp.TreatmentPrinciple != null && tp.TreatmentPrinciple.ToLower().Contains(keyword)));
            }

            // 排序
            queryable = query.OrderBy switch
            {
                "PlanName" => query.IsAscending ? queryable.OrderBy(tp => tp.PlanName) : queryable.OrderByDescending(tp => tp.PlanName),
                "CreateTime" => query.IsAscending ? queryable.OrderBy(tp => tp.CreateTime) : queryable.OrderByDescending(tp => tp.CreateTime),
                "Status" => query.IsAscending ? queryable.OrderBy(tp => tp.Status) : queryable.OrderByDescending(tp => tp.Status),
                _ => queryable.OrderByDescending(tp => tp.CreateTime)
            };

            var totalCount = await queryable.CountAsync();
            var treatmentPlans = await queryable
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans);

            // 批量获取医疗案例信息
            var medicalCaseIds = treatmentPlans.Select(tp => tp.MedicalCaseId).Distinct().ToList();
            var medicalCases = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .Where(mc => medicalCaseIds.Contains(mc.Id))
                .ToDictionaryAsync(mc => mc.Id, mc => mc);

            foreach (var dto in dtos)
            {
                if (medicalCases.TryGetValue(dto.MedicalCaseId, out var medicalCase))
                {
                    dto.PatientId = medicalCase.PatientId;
                    dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                    dto.DoctorId = medicalCase.DoctorId;
                    dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
                }
            }

            return new PaginatedResult<TreatmentPlanDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
            };
        }

        public async Task<TreatmentPlanDetailDto?> CreateAsync(TreatmentPlanCreateDto dto, Guid operatorId, string operatorName)
        {
            // 验证医疗案例是否存在
            var medicalCase = await _context.MedicalCases
                .FirstOrDefaultAsync(mc => mc.Id == dto.MedicalCaseId);

            if (medicalCase == null)
                throw new ArgumentException("医疗案例不存在");

            var treatmentPlan = _mapper.Map<TreatmentPlanModel>(dto);
            treatmentPlan.Id = Guid.NewGuid();
            treatmentPlan.CreateTime = DateTime.Now;
            treatmentPlan.CreatedBy = operatorName;
            treatmentPlan.Status = "草稿";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.TreatmentPlans.Add(treatmentPlan);
                await _context.SaveChangesAsync();

                // 添加处方关联
                var prescriptionItems = new List<TreatmentPlanPrescription>();
                foreach (var prescriptionDto in dto.Prescriptions)
                {
                    prescriptionItems.Add(new TreatmentPlanPrescription
                    {
                        Id = Guid.NewGuid(),
                        TreatmentPlanId = treatmentPlan.Id,
                        PrescriptionId = prescriptionDto.PrescriptionId,
                        IsPrimary = prescriptionDto.IsPrimary,
                        Status = "待执行"
                    });
                }

                // 添加治疗项目
                var treatmentItems = new List<TreatmentPlanItem>();
                foreach (var itemDto in dto.TreatmentItems)
                {
                    treatmentItems.Add(new TreatmentPlanItem
                    {
                        Id = Guid.NewGuid(),
                        TreatmentPlanId = treatmentPlan.Id,
                        ItemName = itemDto.ItemName,
                        ItemType = itemDto.ItemType,
                        Frequency = itemDto.Frequency,
                        FrequencyUnit = itemDto.FrequencyUnit,
                        UnitPrice = itemDto.UnitPrice,
                        TotalAmount = itemDto.UnitPrice * itemDto.Frequency,
                        Requirements = itemDto.Requirements,
                        Status = "待执行",
                        SortOrder = itemDto.SortOrder
                    });
                }

                if (prescriptionItems.Any())
                {
                    _context.TreatmentPlanPrescriptions.AddRange(prescriptionItems);
                }

                if (treatmentItems.Any())
                {
                    _context.TreatmentPlanItems.AddRange(treatmentItems);
                }

                // 更新总金额
                treatmentPlan.TotalAmount = treatmentItems.Sum(ti => ti.TotalAmount);
                await _context.SaveChangesAsync();

                // 更新医疗案例的治疗方案ID
                medicalCase.TreatmentPlanId = treatmentPlan.Id;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("创建治疗方案成功 - 方案ID: {PlanId}, 方案名称: {PlanName}, 操作员: {Operator}",
                    treatmentPlan.Id, treatmentPlan.PlanName, operatorName);

                return await GetByIdAsync(treatmentPlan.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TreatmentPlanDetailDto?> UpdateAsync(Guid id, TreatmentPlanUpdateDto dto, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (treatmentPlan == null)
                return null;

            // 只有草稿状态可以修改
            if (treatmentPlan.Status != "草稿")
                throw new InvalidOperationException("只有草稿状态的治疗方案可以修改");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 更新基本信息
                treatmentPlan.PlanName = dto.PlanName;
                treatmentPlan.TreatmentObjective = dto.TreatmentObjective;
                treatmentPlan.TreatmentPrinciple = dto.TreatmentPrinciple;
                treatmentPlan.Prognosis = dto.Prognosis;
                treatmentPlan.Precautions = dto.Precautions;
                treatmentPlan.FollowUpPlan = dto.FollowUpPlan;
                treatmentPlan.Remark = dto.Remark;
                treatmentPlan.StartDate = dto.StartDate;
                treatmentPlan.EndDate = dto.EndDate;
                treatmentPlan.Duration = dto.Duration;
                treatmentPlan.UpdateTime = DateTime.Now;
                treatmentPlan.UpdatedBy = operatorName;

                if (!string.IsNullOrWhiteSpace(dto.Status))
                {
                    treatmentPlan.Status = dto.Status;
                }

                // 删除现有的处方和治疗项目关联
                _context.TreatmentPlanPrescriptions.RemoveRange(treatmentPlan.Prescriptions);
                _context.TreatmentPlanItems.RemoveRange(treatmentPlan.TreatmentItems);

                // 添加新的处方关联
                var prescriptionItems = new List<TreatmentPlanPrescription>();
                foreach (var prescriptionDto in dto.Prescriptions)
                {
                    prescriptionItems.Add(new TreatmentPlanPrescription
                    {
                        Id = prescriptionDto.Id ?? Guid.NewGuid(),
                        TreatmentPlanId = treatmentPlan.Id,
                        PrescriptionId = prescriptionDto.PrescriptionId,
                        IsPrimary = prescriptionDto.IsPrimary,
                        Status = "待执行"
                    });
                }

                // 添加新的治疗项目
                var treatmentItems = new List<TreatmentPlanItem>();
                foreach (var itemDto in dto.TreatmentItems)
                {
                    treatmentItems.Add(new TreatmentPlanItem
                    {
                        Id = itemDto.Id ?? Guid.NewGuid(),
                        TreatmentPlanId = treatmentPlan.Id,
                        ItemName = itemDto.ItemName,
                        ItemType = itemDto.ItemType,
                        Frequency = itemDto.Frequency,
                        FrequencyUnit = itemDto.FrequencyUnit,
                        UnitPrice = itemDto.UnitPrice,
                        TotalAmount = itemDto.UnitPrice * itemDto.Frequency,
                        Requirements = itemDto.Requirements,
                        Status = itemDto.Status ?? "待执行",
                        SortOrder = itemDto.SortOrder
                    });
                }

                if (prescriptionItems.Any())
                {
                    _context.TreatmentPlanPrescriptions.AddRange(prescriptionItems);
                }

                if (treatmentItems.Any())
                {
                    _context.TreatmentPlanItems.AddRange(treatmentItems);
                }

                // 更新总金额
                treatmentPlan.TotalAmount = treatmentItems.Sum(ti => ti.TotalAmount);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("更新治疗方案成功 - 方案ID: {PlanId}, 方案名称: {PlanName}, 操作员: {Operator}",
                    treatmentPlan.Id, treatmentPlan.PlanName, operatorName);

                return await GetByIdAsync(treatmentPlan.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (treatmentPlan == null)
                return false;

            // 只有草稿状态可以删除
            if (treatmentPlan.Status != "草稿")
                throw new InvalidOperationException("只有草稿状态的治疗方案可以删除");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 删除关联的处方和治疗项目
                _context.TreatmentPlanPrescriptions.RemoveRange(treatmentPlan.Prescriptions);
                _context.TreatmentPlanItems.RemoveRange(treatmentPlan.TreatmentItems);

                // 删除治疗方案
                _context.TreatmentPlans.Remove(treatmentPlan);

                // 更新医疗案例的治疗方案ID
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.TreatmentPlanId == id);
                if (medicalCase != null)
                {
                    medicalCase.TreatmentPlanId = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("删除治疗方案成功 - 方案ID: {PlanId}, 方案名称: {PlanName}, 操作员: {Operator}",
                    treatmentPlan.Id, treatmentPlan.PlanName, operatorName);

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TreatmentPlanDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .ThenInclude(tpp => tpp.Prescription)
                .Include(tp => tp.TreatmentItems)
                .FirstOrDefaultAsync(tp => tp.MedicalCaseId == medicalCaseId);

            if (treatmentPlan == null)
                return null;

            var dto = _mapper.Map<TreatmentPlanDetailDto>(treatmentPlan);

            // 获取患者和医生信息
            var medicalCase = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .FirstOrDefaultAsync(mc => mc.Id == medicalCaseId);

            if (medicalCase != null)
            {
                dto.PatientId = medicalCase.PatientId;
                dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
            }

            return dto;
        }

        public async Task<List<TreatmentPlanDto>> GetByPatientIdAsync(Guid patientId)
        {
            var treatmentPlans = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .Where(tp => _context.MedicalCases
                    .Where(mc => mc.PatientId == patientId)
                    .Select(mc => mc.Id)
                    .Contains(tp.MedicalCaseId))
                .OrderByDescending(tp => tp.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans);

            // 批量获取医疗案例信息
            var medicalCaseIds = treatmentPlans.Select(tp => tp.MedicalCaseId).Distinct().ToList();
            var medicalCases = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .Where(mc => medicalCaseIds.Contains(mc.Id))
                .ToDictionaryAsync(mc => mc.Id, mc => mc);

            foreach (var dto in dtos)
            {
                if (medicalCases.TryGetValue(dto.MedicalCaseId, out var medicalCase))
                {
                    dto.PatientId = medicalCase.PatientId;
                    dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                    dto.DoctorId = medicalCase.DoctorId;
                    dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
                }
            }

            return dtos;
        }

        public async Task<List<TreatmentPlanDto>> GetByDoctorIdAsync(Guid doctorId)
        {
            var treatmentPlans = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .Where(tp => _context.MedicalCases
                    .Where(mc => mc.DoctorId == doctorId)
                    .Select(mc => mc.Id)
                    .Contains(tp.MedicalCaseId))
                .OrderByDescending(tp => tp.CreateTime)
                .Take(100)
                .ToListAsync();

            var dtos = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans);

            // 批量获取医疗案例信息
            var medicalCaseIds = treatmentPlans.Select(tp => tp.MedicalCaseId).Distinct().ToList();
            var medicalCases = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .Where(mc => medicalCaseIds.Contains(mc.Id))
                .ToDictionaryAsync(mc => mc.Id, mc => mc);

            foreach (var dto in dtos)
            {
                if (medicalCases.TryGetValue(dto.MedicalCaseId, out var medicalCase))
                {
                    dto.PatientId = medicalCase.PatientId;
                    dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                    dto.DoctorId = medicalCase.DoctorId;
                    dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
                }
            }

            return dtos;
        }

        public async Task<TreatmentPlanStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? doctorId = null)
        {
            var query = _context.TreatmentPlans.AsQueryable();

            // 如果指定医生，根据医疗案例过滤
            if (doctorId.HasValue)
            {
                query = query.Where(tp => _context.MedicalCases
                    .Where(mc => mc.DoctorId == doctorId.Value)
                    .Select(mc => mc.Id)
                    .Contains(tp.MedicalCaseId));
            }

            var treatmentPlans = await query
                .Where(tp => tp.CreateTime >= startDate && tp.CreateTime <= endDate)
                .ToListAsync();

            var statistics = new TreatmentPlanStatisticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalCount = treatmentPlans.Count,
                ActiveCount = treatmentPlans.Count(tp => tp.Status == "进行中"),
                CompletedCount = treatmentPlans.Count(tp => tp.Status == "已完成"),
                CancelledCount = treatmentPlans.Count(tp => tp.Status == "已取消"),
                TotalAmount = treatmentPlans.Sum(tp => tp.TotalAmount)
            };

            // 状态统计
            var statusStats = treatmentPlans
                .GroupBy(tp => tp.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.StatusStats = statusStats;

            return statistics;
        }

        // ==================== 其他方法的简化实现 ====================

        public async Task<bool> StartPlanAsync(Guid id, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id);
            if (treatmentPlan == null)
                return false;

            treatmentPlan.Status = "进行中";
            treatmentPlan.StartDate = treatmentPlan.StartDate ?? DateTime.Now;
            treatmentPlan.UpdateTime = DateTime.Now;
            treatmentPlan.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompletePlanAsync(Guid id, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id);
            if (treatmentPlan == null)
                return false;

            treatmentPlan.Status = "已完成";
            treatmentPlan.EndDate = treatmentPlan.EndDate ?? DateTime.Now;
            treatmentPlan.UpdateTime = DateTime.Now;
            treatmentPlan.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PausePlanAsync(Guid id, string reason, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id);
            if (treatmentPlan == null)
                return false;

            treatmentPlan.Status = "已暂停";
            treatmentPlan.UpdateTime = DateTime.Now;
            treatmentPlan.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResumePlanAsync(Guid id, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id);
            if (treatmentPlan == null)
                return false;

            treatmentPlan.Status = "进行中";
            treatmentPlan.UpdateTime = DateTime.Now;
            treatmentPlan.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelPlanAsync(Guid id, string reason, Guid operatorId, string operatorName)
        {
            var treatmentPlan = await _context.TreatmentPlans.FirstOrDefaultAsync(tp => tp.Id == id);
            if (treatmentPlan == null)
                return false;

            treatmentPlan.Status = "已取消";
            treatmentPlan.UpdateTime = DateTime.Now;
            treatmentPlan.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddPrescriptionAsync(Guid planId, Guid prescriptionId, bool isPrimary, Guid operatorId, string operatorName)
        {
            var prescriptionLink = new TreatmentPlanPrescription
            {
                Id = Guid.NewGuid(),
                TreatmentPlanId = planId,
                PrescriptionId = prescriptionId,
                IsPrimary = isPrimary,
                Status = "待执行"
            };

            _context.TreatmentPlanPrescriptions.Add(prescriptionLink);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemovePrescriptionAsync(Guid planId, Guid prescriptionId, Guid operatorId, string operatorName)
        {
            var prescriptionLink = await _context.TreatmentPlanPrescriptions
                .FirstOrDefaultAsync(tpp => tpp.TreatmentPlanId == planId && tpp.PrescriptionId == prescriptionId);

            if (prescriptionLink == null)
                return false;

            _context.TreatmentPlanPrescriptions.Remove(prescriptionLink);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddTreatmentItemAsync(Guid planId, TreatmentPlanItemCreateDto item, Guid operatorId, string operatorName)
        {
            var treatmentItem = new TreatmentPlanItem
            {
                Id = Guid.NewGuid(),
                TreatmentPlanId = planId,
                ItemName = item.ItemName,
                ItemType = item.ItemType,
                Frequency = item.Frequency,
                FrequencyUnit = item.FrequencyUnit,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.UnitPrice * item.Frequency,
                Requirements = item.Requirements,
                Status = "待执行",
                SortOrder = item.SortOrder
            };

            _context.TreatmentPlanItems.Add(treatmentItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTreatmentItemAsync(Guid planId, Guid itemId, TreatmentPlanItemUpdateDto item, Guid operatorId, string operatorName)
        {
            var treatmentItem = await _context.TreatmentPlanItems
                .FirstOrDefaultAsync(ti => ti.Id == itemId && ti.TreatmentPlanId == planId);

            if (treatmentItem == null)
                return false;

            treatmentItem.ItemName = item.ItemName;
            treatmentItem.ItemType = item.ItemType;
            treatmentItem.Frequency = item.Frequency;
            treatmentItem.FrequencyUnit = item.FrequencyUnit;
            treatmentItem.UnitPrice = item.UnitPrice;
            treatmentItem.TotalAmount = item.UnitPrice * item.Frequency;
            treatmentItem.Requirements = item.Requirements;
            treatmentItem.Status = item.Status ?? treatmentItem.Status;
            treatmentItem.SortOrder = item.SortOrder;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveTreatmentItemAsync(Guid planId, Guid itemId, Guid operatorId, string operatorName)
        {
            var treatmentItem = await _context.TreatmentPlanItems
                .FirstOrDefaultAsync(ti => ti.Id == itemId && ti.TreatmentPlanId == planId);

            if (treatmentItem == null)
                return false;

            _context.TreatmentPlanItems.Remove(treatmentItem);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== 其他高级功能的简化实现 ====================

        public async Task<bool> RecordExecutionAsync(Guid planId, TreatmentExecutionRecordDto record, Guid operatorId, string operatorName)
        {
            _logger.LogInformation("记录治疗执行 - 方案ID: {PlanId}, 执行类型: {ExecutionType}, 操作员: {Operator}",
                planId, record.ExecutionType, operatorName);
            return true;
        }

        public async Task<List<TreatmentExecutionRecordDto>> GetExecutionRecordsAsync(Guid planId)
        {
            return new List<TreatmentExecutionRecordDto>();
        }

        public async Task<TreatmentProgressDto> GetTreatmentProgressAsync(Guid planId)
        {
            var treatmentPlan = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .FirstOrDefaultAsync(tp => tp.Id == planId);

            if (treatmentPlan == null)
                return new TreatmentProgressDto();

            var totalPrescriptions = treatmentPlan.Prescriptions.Count;
            var completedPrescriptions = treatmentPlan.Prescriptions.Count(p => p.Status == "已完成");
            var totalTreatmentItems = treatmentPlan.TreatmentItems.Count;
            var completedTreatmentItems = treatmentPlan.TreatmentItems.Count(ti => ti.Status == "已完成");

            var totalTasks = totalPrescriptions + totalTreatmentItems;
            var completedTasks = completedPrescriptions + completedTreatmentItems;
            var progressPercentage = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

            return new TreatmentProgressDto
            {
                TreatmentPlanId = planId,
                TotalPrescriptions = totalPrescriptions,
                CompletedPrescriptions = completedPrescriptions,
                TotalTreatmentItems = totalTreatmentItems,
                CompletedTreatmentItems = completedTreatmentItems,
                ProgressPercentage = progressPercentage,
                CurrentPhase = treatmentPlan.Status,
                PendingTasks = new List<string>()
            };
        }

        public async Task<TreatmentPlanDetailDto?> CreateFromTemplateAsync(Guid templateId, Guid medicalCaseId, Guid operatorId, string operatorName)
        {
            throw new NotImplementedException("治疗方案模板功能暂未实现");
        }

        public async Task<TreatmentPlanTemplateDto?> SaveAsTemplateAsync(Guid planId, string templateName, string diseaseCategory, Guid operatorId, string operatorName)
        {
            throw new NotImplementedException("治疗方案模板功能暂未实现");
        }

        public async Task<List<TreatmentPlanTemplateDto>> GetTemplatesAsync(string? diseaseCategory = null)
        {
            return new List<TreatmentPlanTemplateDto>();
        }

        public async Task<List<TreatmentPlanDto>> SearchPlansAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<TreatmentPlanDto>();

            var searchTerm = keyword.Trim().ToLower();

            var treatmentPlans = await _context.TreatmentPlans
                .Include(tp => tp.Prescriptions)
                .Include(tp => tp.TreatmentItems)
                .Where(tp => tp.PlanName.ToLower().Contains(searchTerm) ||
                           tp.TreatmentObjective.ToLower().Contains(searchTerm) ||
                           (tp.TreatmentPrinciple != null && tp.TreatmentPrinciple.ToLower().Contains(searchTerm)))
                .OrderByDescending(tp => tp.CreateTime)
                .Take(maxResults)
                .ToListAsync();

            var dtos = _mapper.Map<List<TreatmentPlanDto>>(treatmentPlans);

            // 批量获取医疗案例信息
            var medicalCaseIds = treatmentPlans.Select(tp => tp.MedicalCaseId).Distinct().ToList();
            var medicalCases = await _context.MedicalCases
                .Include(mc => mc.Patient)
                .Include(mc => mc.Doctor)
                .Where(mc => medicalCaseIds.Contains(mc.Id))
                .ToDictionaryAsync(mc => mc.Id, mc => mc);

            foreach (var dto in dtos)
            {
                if (medicalCases.TryGetValue(dto.MedicalCaseId, out var medicalCase))
                {
                    dto.PatientId = medicalCase.PatientId;
                    dto.PatientName = medicalCase.Patient?.Name ?? "未知患者";
                    dto.DoctorId = medicalCase.DoctorId;
                    dto.DoctorName = medicalCase.Doctor?.Name ?? "未知医生";
                }
            }

            return dtos;
        }
    }
}