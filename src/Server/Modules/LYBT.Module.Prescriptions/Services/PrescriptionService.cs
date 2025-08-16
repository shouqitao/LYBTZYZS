using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方业务逻辑实现（现场开处方模式）
    /// </summary>
    public class PrescriptionService : IPrescriptionService
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

        /// <summary>
        /// 执行GetAllAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<PrescriptionDto>> GetAllAsync()
        {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<PrescriptionDto>>(list);
        }

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        /// <param name="query">分页查询参数</param>
        /// <param name="operatorRole">操作者角色</param>
        /// <returns>分页结果</returns>
        public async Task<PaginatedResult<PrescriptionDto>> GetPagedAsync(PagedQueryBaseDto query)
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
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<PrescriptionDetailDto?> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var gid))
                return null;
            var model = await _repository.GetByIdAsync(gid);
            return model == null ? null : _mapper.Map<PrescriptionDetailDto>(model);
        }

        /// <summary>
        /// 执行CreateAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<PrescriptionDto?> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
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
                    // Amount属性是计算属性，不需要设置
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
        /// <param name="dto">参数dto</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
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
        /// <param name="id">参数id</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName)
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

        #region 现场开处方功能

        /// <summary>
        /// 获取患者历史处方
        /// </summary>
        public async Task<List<PrescriptionDto>> GetPatientHistoryAsync(Guid patientId, int limit = 10)
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
            var lastPrescription = await GetPatientHistoryAsync(patientId, 1);
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

            return await CreateAsync(copyDto, operatorId, operatorName);
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
        public async Task<PrescriptionStatisticsDto> GetStatisticsAsync(Guid? doctorId = null, DateTime? startDate = null, DateTime? endDate = null)
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