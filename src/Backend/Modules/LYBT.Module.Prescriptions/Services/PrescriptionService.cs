using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using System.Text.Json;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方业务逻辑实现（现场开处方模式）
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IUnifiedLogService _logService;
        private readonly IMapper _mapper;

        public PrescriptionService(IPrescriptionRepository repository, IUnifiedLogService logService, IMapper mapper)
        {
            _repository = repository;
            _logService = logService;
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
        public async Task<PaginatedResult<PrescriptionDto>> GetPagedAsync(PaginationRequest query)
        {
            var allList = await _repository.GetListAsync();
            var dtoList = _mapper.Map<List<PrescriptionDto>>(allList);

            // 在内存中进行搜索和分页
            var filteredList = dtoList.AsQueryable();

            // 如果有搜索关键字，进行搜索过滤
            if (!string.IsNullOrEmpty(query.SearchKeyword))
            {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    x.PatientId.ToString().Contains(query.SearchKeyword) ||
                    x.DoctorId.ToString().Contains(query.SearchKeyword)
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<PrescriptionDto>(pagedList, total, query.CurrentPage, query.PageSize);
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
            var model = _mapper.Map<LYBT.Models.Prescriptions.PrescriptionModel>(dto);
            model.Id = Guid.NewGuid();
            var success = await _repository.AddAsync(model);
            if (!success)
                return null;

            await _logService.CreateLogAsync(new LogCreateDto
            {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Create,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "新增处方",
                NewValue = JsonSerializer.Serialize(model)
            });

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
            await _logService.CreateLogAsync(new LogCreateDto
            {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "编辑处方",
                OldValue = JsonSerializer.Serialize(old),
                NewValue = JsonSerializer.Serialize(model)
            });
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
            await _logService.CreateLogAsync(new LogCreateDto
            {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = gid,
                ActionType = ActionType.Other,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "删除处方",
                OldValue = JsonSerializer.Serialize(item)
            });
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
            await _logService.CreateLogAsync(new LogCreateDto
            {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = gid,
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "作废处方",
                OldValue = JsonSerializer.Serialize(model)
            });
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
                await _logService.CreateLogAsync(new LogCreateDto
                {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Prescription,
                    ObjectId = prescriptionId,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = "快速保存处方"
                });
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
                await _logService.CreateLogAsync(new LogCreateDto
                {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Prescription,
                    ObjectId = prescriptionId,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = "提交处方"
                });
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