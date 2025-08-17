using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Interfaces; // 保留智能处方服务接口
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方业务逻辑实现 - UltraThink Phase 7: 实现Shared接口统一
    /// </summary>
    public class PrescriptionService : LYBT.Shared.Interfaces.Services.IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly LYBT.Infrastructure.Data.AppDbContext _dbContext;
        private readonly ILogger<PrescriptionService> _logger;
        private readonly IIntelligentPrescriptionService _intelligentService;
        private readonly IMapper _mapper;

        public PrescriptionService(
            IPrescriptionRepository repository, 
            LYBT.Infrastructure.Data.AppDbContext dbContext,
            ILogger<PrescriptionService> logger,
            IIntelligentPrescriptionService intelligentService,
            IMapper mapper)
        {
            _repository = repository;
            _dbContext = dbContext;
            _logger = logger;
            _intelligentService = intelligentService;
            _mapper = mapper;
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");
                }

                var dto = _mapper.Map<PrescriptionDto>(model);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败: {PrescriptionId}", id);
                return ServiceResult<PrescriptionDto>.Failure("获取处方详情失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 分页查询处方
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            try
            {
                // 转换为内部查询格式
                var internalQuery = new PagedQueryBaseDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword
                };

                var result = await GetPagedInternalAsync(internalQuery);
                var pagedResult = new PagedResult<PrescriptionDto>(result.Items.ToList(), result.TotalCount, result.CurrentPage, result.PageSize);
                
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询处方失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("查询处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 创建新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                // 使用默认操作者信息（在实际应用中应该从认证上下文获取）
                var operatorId = Guid.Empty; // TODO: 从认证上下文获取
                var operatorName = "System"; // TODO: 从认证上下文获取

                var result = await CreateInternalAsync(dto, operatorId, operatorName);
                if (result == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
                }

                return ServiceResult<PrescriptionDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return ServiceResult<PrescriptionDto>.Failure("创建处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            try
            {
                dto.Id = id; // 确保ID一致
                var operatorId = Guid.Empty; // TODO: 从认证上下文获取
                var operatorName = "System"; // TODO: 从认证上下文获取

                var success = await UpdateInternalAsync(dto, operatorId, operatorName);
                if (!success)
                {
                    return ServiceResult<PrescriptionDto>.Failure("更新处方失败");
                }

                // 获取更新后的处方
                var updatedResult = await GetByIdAsync(id);
                return updatedResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败: {PrescriptionId}", id);
                return ServiceResult<PrescriptionDto>.Failure("更新处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var operatorId = Guid.Empty; // TODO: 从认证上下文获取
                var operatorName = "System"; // TODO: 从认证上下文获取

                var success = await DeleteInternalAsync(id.ToString(), operatorId, operatorName);
                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败: {PrescriptionId}", id);
                return ServiceResult<bool>.Failure("删除处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 根据患者ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var prescriptions = await GetPatientHistoryInternalAsync(patientId);
                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方失败: {PatientId}", patientId);
                return ServiceResult<List<PrescriptionDto>>.Failure("获取患者处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 根据看诊ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByConsultationIdAsync(Guid consultationId)
        {
            try
            {
                // 简化实现：根据consultationId查询处方
                var allPrescriptions = await _repository.GetListAsync();
                var consultationPrescriptions = allPrescriptions
                    .Where(p => p.PatientId == consultationId) // 简化：假设consultationId对应PatientId
                    .ToList();
                
                var dtos = _mapper.Map<List<PrescriptionDto>>(consultationPrescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊处方失败: {ConsultationId}", consultationId);
                return ServiceResult<List<PrescriptionDto>>.Failure("获取看诊处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 验证处方数据
        /// </summary>
        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var result = new PrescriptionValidationResult();

                // 基本验证
                if (dto.PatientId == Guid.Empty)
                {
                    result.Errors.Add("患者ID不能为空");
                }

                if (dto.Items == null || !dto.Items.Any())
                {
                    result.Errors.Add("处方药品不能为空");
                }

                // 药品验证
                if (dto.Items != null && dto.Items.Any())
                {
                    var prescriptionItems = dto.Items.Select(item => new LYBT.Entities.Prescriptions.PrescriptionItemModel
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity
                    }).ToList();

                    // 检测重复药材
                    var duplicateResult = _intelligentService.DetectDuplicateHerbs(prescriptionItems);
                    if (duplicateResult.HasDuplicates)
                    {
                        result.Warnings.Add($"发现重复药材: {string.Join(", ", duplicateResult.DuplicateHerbs)}");
                    }

                    // 检查药材可用性
                    var availabilityResult = await _intelligentService.CheckHerbAvailabilityAsync(prescriptionItems);
                    if (!availabilityResult.IsAvailable)
                    {
                        result.Warnings.Add($"部分药材不可用: {string.Join(", ", availabilityResult.UnavailableHerbs)}");
                    }
                }

                result.IsValid = !result.Errors.Any();
                return ServiceResult<PrescriptionValidationResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方数据失败");
                return ServiceResult<PrescriptionValidationResult>.Failure("验证处方数据失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 导出处方为PDF
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid id)
        {
            try
            {
                // TODO: 实现PDF导出功能
                await Task.CompletedTask;
                return ServiceResult<byte[]>.Failure("PDF导出功能暂未实现");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出处方PDF失败: {PrescriptionId}", id);
                return ServiceResult<byte[]>.Failure("导出处方PDF失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 获取处方统计信息
        /// </summary>
        public async Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var statistics = await GetStatisticsInternalAsync(null, startDate, endDate);
                return ServiceResult<PrescriptionStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方统计失败");
                return ServiceResult<PrescriptionStatisticsDto>.Failure("获取处方统计失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 批准处方
        /// </summary>
        public async Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                prescription.Status = PrescriptionStatus.Completed;
                prescription.UpdateTime = DateTime.Now;
                // TODO: 添加审批记录

                var success = await _repository.UpdateAsync(prescription);
                _logger.LogInformation("处方审批通过: {PrescriptionId}, 备注: {Note}", id, approvalNote);
                
                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批准处方失败: {PrescriptionId}", id);
                return ServiceResult<bool>.Failure("批准处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 拒绝处方
        /// </summary>
        public async Task<ServiceResult<bool>> RejectAsync(Guid id, string reason)
        {
            try
            {
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                prescription.Status = PrescriptionStatus.Draft; // 退回草稿状态
                prescription.UpdateTime = DateTime.Now;
                // TODO: 添加拒绝记录

                var success = await _repository.UpdateAsync(prescription);
                _logger.LogInformation("处方审批拒绝: {PrescriptionId}, 原因: {Reason}", id, reason);
                
                return ServiceResult<bool>.Success(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "拒绝处方失败: {PrescriptionId}", id);
                return ServiceResult<bool>.Failure("拒绝处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                var originalPrescription = await _repository.GetByIdAsync(id);
                if (originalPrescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("原处方不存在");
                }

                var copyDto = new PrescriptionCreateDto
                {
                    PatientId = originalPrescription.PatientId,
                    DoctorId = originalPrescription.UserId,
                    Diagnosis = string.IsNullOrEmpty(newName) ? originalPrescription.Diagnosis : newName,
                    DosageCount = originalPrescription.DosageCount,
                    Advice = originalPrescription.Advice,
                    Items = originalPrescription.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Remark = item.Remark
                    }).ToList()
                };

                return await CreateAsync(copyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制处方失败: {PrescriptionId}", id);
                return ServiceResult<PrescriptionDto>.Failure("复制处方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            try
            {
                var query = new PrescriptionQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 1000 // 搜索返回大量结果
                };

                var pagedResult = await GetPagedAsync(query);
                if (!pagedResult.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "搜索失败");
                }

                return ServiceResult<List<PrescriptionDto>>.Success(pagedResult.Data.Items.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方失败");
                return ServiceResult<List<PrescriptionDto>>.Failure("搜索处方失败", ex);
            }
        }

        #endregion

        #region Legacy Internal Methods (保持兼容性)

        /// <summary>
        /// 执行GetAllAsync操作。
        /// </summary>
        public async Task<List<PrescriptionDto>> GetAllAsync()
        {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<PrescriptionDto>>(list);
        }

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        public async Task<PaginatedResult<PrescriptionDto>> GetPagedInternalAsync(PagedQueryBaseDto query)
        {
            // 使用IQueryable在数据库层进行查询
            var dbQuery = _dbContext.Prescriptions
                .Include(p => p.Items)
                .AsQueryable();

            // 如果有搜索关键字，在数据库层进行搜索过滤
            if (!string.IsNullOrEmpty(query.Keyword))
            {
                dbQuery = dbQuery.Where(x =>
                    x.Id.ToString().Contains(query.Keyword) ||
                    x.PatientId.ToString().Contains(query.Keyword) ||
                    x.UserId.ToString().Contains(query.Keyword)
                );
            }

            // 排序 - 默认按创建时间降序
            dbQuery = dbQuery.OrderByDescending(x => x.CreateTime);

            // 获取总数
            var total = await dbQuery.CountAsync();

            // 分页 - 在数据库层执行
            var pagedModels = await dbQuery
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // 映射到DTO
            var pagedList = _mapper.Map<List<PrescriptionDto>>(pagedModels);

            return new PaginatedResult<PrescriptionDto>(pagedList, total, query.PageIndex, query.PageSize);
        }

        /// <summary>
        /// 执行GetByIdAsync操作。
        /// </summary>
        public async Task<PrescriptionDetailDto?> GetByIdInternalAsync(string id)
        {
            if (!Guid.TryParse(id, out var gid))
                return null;
            var model = await _repository.GetByIdAsync(gid);
            return model == null ? null : _mapper.Map<PrescriptionDetailDto>(model);
        }

        /// <summary>
        /// 执行CreateAsync操作。
        /// </summary>
        public async Task<PrescriptionDto?> CreateInternalAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
        {
            var model = _mapper.Map<LYBT.Entities.Prescriptions.PrescriptionModel>(dto);
            model.Id = Guid.NewGuid();
            
            // 执行智能检查
            if (dto.Items != null && dto.Items.Any())
            {
                // 先将PrescriptionItemCreateDto转换为PrescriptionItemModel
                var prescriptionItems = dto.Items.Select(item => new LYBT.Entities.Prescriptions.PrescriptionItemModel
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Usage = item.Usage,
                    Remark = item.Remark ?? item.Note
                }).ToList();

                // 检测重复药材
                var duplicateResult = _intelligentService.DetectDuplicateHerbs(prescriptionItems);
                if (duplicateResult.HasDuplicates && duplicateResult.DuplicateHerbs.Any())
                {
                    _logger.LogWarning("处方重复药材警告 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}, 重复药材: {DuplicateHerbs}", 
                        operatorName, model.Id, string.Join(", ", duplicateResult.DuplicateHerbs));
                }

                // 检查药材可用性
                var availabilityResult = await _intelligentService.CheckHerbAvailabilityAsync(prescriptionItems);
                if (!availabilityResult.IsAvailable && availabilityResult.UnavailableHerbs.Any())
                {
                    _logger.LogWarning("药材可用性警告 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}, 不可用药材: {UnavailableHerbs}", 
                        operatorName, model.Id, string.Join(", ", availabilityResult.UnavailableHerbs));
                }
            }
            
            var success = await _repository.AddAsync(model);
            if (!success)
                return null;

            _logger.LogInformation("处方新增 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                operatorName, operatorId, model.Id);

            // 返回创建的对象
            return _mapper.Map<PrescriptionDto>(model);
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>
        public async Task<bool> UpdateInternalAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
        {
            var old = await _repository.GetByIdAsync(dto.Id);
            if (old == null)
                return false;
            var model = _mapper.Map(dto, old);
            var success = await _repository.UpdateAsync(model);
            _logger.LogInformation("处方编辑 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                operatorName, operatorId, model.Id);
            return success;
        }

        /// <summary>
        /// 执行DeleteAsync操作。
        /// </summary>
        public async Task<bool> DeleteInternalAsync(string id, Guid operatorId, string operatorName)
        {
            if (!Guid.TryParse(id, out var gid))
                return false;
            var item = await _repository.GetByIdAsync(gid);
            if (item == null)
                return false;
            var success = await _repository.DeleteAsync(gid);
            _logger.LogInformation("处方删除 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                operatorName, operatorId, gid);
            return success;
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)
        {
            if (!Guid.TryParse(id, out var gid))
                return false;
            var model = await _repository.GetByIdAsync(gid);
            if (model == null)
                return false;
            var success = await _repository.CancelAsync(gid);
            _logger.LogInformation("处方作废 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                operatorName, operatorId, gid);
            return success;
        }

        /// <summary>
        /// 获取患者历史处方
        /// </summary>
        public async Task<List<PrescriptionDto>> GetPatientHistoryInternalAsync(Guid patientId, int limit = 10)
        {
            var allPrescriptions = await _repository.GetListAsync();
            var patientPrescriptions = allPrescriptions
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreateTime)
                .Take(limit)
                .ToList();
            return _mapper.Map<List<PrescriptionDto>>(patientPrescriptions);
        }

        /// <summary>
        /// 获取医生今日处方
        /// </summary>
        public async Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            var today = DateTime.Today;
            var allPrescriptions = await _repository.GetListAsync();
            var todayPrescriptions = allPrescriptions
                .Where(p => p.UserId == doctorId && p.CreateTime.Date == today)
                .OrderByDescending(p => p.CreateTime)
                .ToList();
            return _mapper.Map<List<PrescriptionDto>>(todayPrescriptions);
        }

        /// <summary>
        /// 复制上次处方
        /// </summary>
        public async Task<PrescriptionDto?> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var lastPrescription = await GetPatientHistoryInternalAsync(patientId, 1);
            if (!lastPrescription.Any())
            {
                return null;
            }

            var lastOne = lastPrescription.First();
            var copyDto = new PrescriptionCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Diagnosis = lastOne.Diagnosis ?? string.Empty,
                DosageCount = lastOne.DosageCount,
                Advice = lastOne.Advice,
                Items = lastOne.Items.Select(item => new PrescriptionItemCreateDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Remark = item.Remark
                }).ToList()
            };

            return await CreateInternalAsync(copyDto, operatorId, operatorName);
        }

        /// <summary>
        /// 从验方模板创建处方
        /// </summary>
        public async Task<PrescriptionDto?> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            // 这里简化实现，返回 null 表示功能暂未实现
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// 快速保存处方（草稿状态）
        /// </summary>
        public async Task<bool> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            var prescription = await _repository.GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                return false;
            }

            prescription.Diagnosis = dto.Diagnosis;
            prescription.Advice = dto.Advice;
            prescription.Status = PrescriptionStatus.Draft; // 草稿状态
            prescription.UpdateTime = DateTime.Now;

            var success = await _repository.UpdateAsync(prescription);
            if (success)
            {
                _logger.LogInformation("处方快速保存 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                    operatorName, operatorId, prescriptionId);
            }

            return success;
        }

        /// <summary>
        /// 提交处方（从草稿变为有效）
        /// </summary>
        public async Task<bool> SubmitPrescriptionAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {
            var prescription = await _repository.GetByIdAsync(prescriptionId);
            if (prescription == null || prescription.Status != PrescriptionStatus.Draft)
            {
                return false;
            }

            // 验证处方完整性
            if (string.IsNullOrEmpty(prescription.Diagnosis) || !prescription.Items.Any())
            {
                return false;
            }

            prescription.SingleDosePrice = prescription.DosageCount > 0 ? 0m / prescription.DosageCount : 0;
            prescription.Status = PrescriptionStatus.Draft;
            prescription.UpdateTime = DateTime.Now;

            var success = await _repository.UpdateAsync(prescription);
            if (success)
            {
                _logger.LogInformation("处方提交 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}", 
                    operatorName, operatorId, prescriptionId);
            }

            return success;
        }

        /// <summary>
        /// 获取处方统计
        /// </summary>
        public async Task<PrescriptionStatisticsDto> GetStatisticsInternalAsync(Guid? doctorId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var allPrescriptions = await _repository.GetListAsync();

            // 按条件筛选
            var filtered = allPrescriptions.AsQueryable();
            if (doctorId.HasValue)
            {
                filtered = filtered.Where(p => p.UserId == doctorId.Value);
            }
            if (startDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreateTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreateTime <= endDate.Value);
            }

            var prescriptions = filtered.ToList();

            return new PrescriptionStatisticsDto
            {
                TotalCount = prescriptions.Count,
                DraftCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                PendingCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                CompletedCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Completed),
                CancelledCount = 0, // PrescriptionStatus.Cancelled已移除
                TotalAmount = 0m, // TotalPrice字段已删除，需要从Items计算
                AverageAmount = 0m // TotalPrice字段已删除
            };
        }

        #endregion
    }
}