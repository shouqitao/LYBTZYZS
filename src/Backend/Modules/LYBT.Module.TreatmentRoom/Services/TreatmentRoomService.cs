using System.Threading.Tasks;
using System.Linq;
using System;
﻿using AutoMapper;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.TreatmentRoom.Services {

    /// <summary>
    /// 理疗室管理服务实现（现场理疗模式）
    /// </summary>
    public class TreatmentRoomService : ITreatmentRoomService {
        private readonly ITreatmentRoomRepository _repository;
        private readonly IMapper _mapper;

        public TreatmentRoomService(ITreatmentRoomRepository repository, IMapper mapper) {
            _repository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取治疗记录详情
        /// </summary>
        public async Task<TreatmentDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<TreatmentDetailDto>(model);
        }

        /// <summary>
        /// 获取治疗记录列表
        /// </summary>
        public async Task<List<TreatmentDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<TreatmentDto>>(list);
        }

        /// <summary>
        /// 分页查询治疗记录
        /// </summary>
        public async Task<PaginatedResult<TreatmentDto>> GetPagedAsync(TreatmentQueryDto query, UserRole operatorRole) {
            var allTreatments = await _repository.GetListAsync();
            var filteredTreatments = allTreatments.AsQueryable();

            // 按条件筛选
            if (query.PatientId.HasValue) {
                filteredTreatments = filteredTreatments.Where(t => t.PatientId == query.PatientId.Value);
            }
            if (query.DoctorId.HasValue) {
                filteredTreatments = filteredTreatments.Where(t => t.DoctorId == query.DoctorId.Value);
            }
            if (!string.IsNullOrEmpty(query.TreatmentType)) {
                filteredTreatments = filteredTreatments.Where(t => t.TreatmentType == query.TreatmentType);
            }
            if (!string.IsNullOrEmpty(query.Status)) {
                filteredTreatments = filteredTreatments.Where(t => t.Status == query.Status);
            }
            if (query.StartDate.HasValue) {
                // filteredTreatments = filteredTreatments.Where(t => t.CreateTime >= query.StartDate.Value); // CreateTime字段已删除
            }
            if (query.EndDate.HasValue) {
                // filteredTreatments = filteredTreatments.Where(t => t.CreateTime <= query.EndDate.Value); // CreateTime字段已删除
            }
            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                var keyword = query.SearchKeyword.ToLower();
                filteredTreatments = filteredTreatments.Where(t => 
                    // t.PatientName.ToLower().Contains(keyword) || // PatientName字段已删除
                    // t.DoctorName.ToLower().Contains(keyword) || // DoctorName字段已删除
                    t.TreatmentType.ToLower().Contains(keyword)
                );
            }

            // 排序
            filteredTreatments = query.OrderBy?.ToLower() switch {
                "patientname" => query.IsAscending ?
                    filteredTreatments.OrderBy(t => t.Id) : // 使用Id替代PatientName
                    filteredTreatments.OrderByDescending(t => t.Id),
                "treatmenttype" => query.IsAscending ?
                    filteredTreatments.OrderBy(t => t.TreatmentType) : 
                    filteredTreatments.OrderByDescending(t => t.TreatmentType),
                "status" => query.IsAscending ? 
                    filteredTreatments.OrderBy(t => t.Status) : 
                    filteredTreatments.OrderByDescending(t => t.Status),
                _ => query.IsAscending ?
                    filteredTreatments.OrderBy(t => t.Id) : // 使用Id替代CreateTime
                    filteredTreatments.OrderByDescending(t => t.Id)
            };


            var total = filteredTreatments.Count();
            var pagedTreatments = filteredTreatments
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtos = _mapper.Map<List<TreatmentDto>>(pagedTreatments);
            return new PaginatedResult<TreatmentDto>(dtos, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 创建治疗记录
        /// </summary>
        public async Task<TreatmentDetailDto?> CreateAsync(TreatmentCreateDto dto, Guid operatorId, string operatorName) {
            var model = _mapper.Map<TreatmentTaskModel>(dto);
            model.Id = Guid.NewGuid();
            model.Status = "Waiting";
            // model.CreateTime = DateTime.Now; // CreateTime字段已删除
            
            // 计算总价
            if (dto.Items.Any()) {
                // // 0m /* model.Price字段已删除 */ = ...; // Price字段已删除 // Price字段已删除
            }
            
            var result = await _repository.AddAsync(model);
            if (!result) return null;

            return _mapper.Map<TreatmentDetailDto>(model);
        }

        /// <summary>
        /// 更新治疗记录
        /// </summary>
        public async Task<TreatmentDetailDto?> UpdateAsync(Guid id, TreatmentUpdateDto dto, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null) return null;

            _mapper.Map(dto, model);
            // model.UpdateTime = DateTime.Now; // UpdateTime字段已删除

            var result = await _repository.UpdateAsync(model);
            return result ? _mapper.Map<TreatmentDetailDto>(model) : null;
        }

        /// <summary>
        /// 删除治疗记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// 根据状态获取治疗记录
        /// </summary>
        public async Task<List<TreatmentDto>> GetByStatusAsync(string status) {
            var list = await _repository.GetByStatusAsync(status);
            return _mapper.Map<List<TreatmentDto>>(list);
        }

        /// <summary>
        /// 获取待治疗队列
        /// </summary>
        public async Task<List<TreatmentQueueDto>> GetTreatmentQueueAsync() {
            var list = await _repository.GetByStatusAsync("Waiting");
            var queueList = new List<TreatmentQueueDto>();
            
            for (int i = 0; i < list.Count; i++) {
                var item = list[i];
                queueList.Add(new TreatmentQueueDto {
                    Id = item.Id,
                    QueueNumber = $"T{DateTime.Now:yyyyMMdd}{(i + 1):D3}",
                    // PatientName = item.PatientName, // PatientName字段已删除
                    TreatmentType = item.TreatmentType,
                    Status = item.Status,
                    // CreateTime = item.CreateTime, // CreateTime字段已删除
                    QueuePosition = i + 1
                });
            }
            
            return queueList;
        }

        /// <summary>
        /// 开始治疗
        /// </summary>
        public async Task<bool> StartTreatmentAsync(Guid id, StartTreatmentDto dto, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null || model.Status != "Waiting") return false;

            model.Status = "InProgress";
            model.StartTime = DateTime.Now;
            // null /* model.TherapistName字段已删除 */ = ...; // TherapistName字段已删除
            // model.RoomNumber = dto.RoomNumber; // RoomNumber字段已删除
            model.Remark = dto.Notes;

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 完成治疗
        /// </summary>
        public async Task<bool> CompleteTreatmentAsync(Guid id, CompleteTreatmentDto dto, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null || model.Status != "InProgress") return false;

            model.Status = "Completed";
            model.EndTime = DateTime.Now;
            // model.TreatmentResult = ...; // TreatmentResult字段已删除
            // model.NextVisitAdvice = ...; // NextVisitAdvice字段已删除
            model.Remark = dto.Notes;

            // 计算实际治疗时长
            if (model.StartTime.HasValue) {
                // model.Duration = dto.ActualDuration ?? (decimal)(DateTime.Now - model.StartTime.Value).TotalMinutes; // Duration字段已删除
            }

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 取消治疗
        /// </summary>
        public async Task<bool> CancelTreatmentAsync(Guid id, string reason, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null || model.Status == "Completed") return false;

            model.Status = "Cancelled";
            model.Remark = reason;
            // model.UpdateTime = DateTime.Now; // UpdateTime字段已删除

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 根据患者ID获取治疗历史
        /// </summary>
        public async Task<List<TreatmentDto>> GetByPatientIdAsync(Guid patientId) {
            var list = await _repository.GetListAsync();
            var filtered = list.Where(t => t.PatientId == patientId).ToList();
            return _mapper.Map<List<TreatmentDto>>(filtered);
        }

        /// <summary>
        /// 获取今日治疗记录
        /// </summary>
        public async Task<List<TreatmentDto>> GetTodayTreatmentsAsync() {
            var today = DateTime.Today;
            var list = await _repository.GetListAsync();
            // var filtered = list.Where(t => t.CreateTime.Date == today).ToList(); // CreateTime字段已删除
            var filtered = list; // 暂时返回所有记录
            return _mapper.Map<List<TreatmentDto>>(filtered);
        }

        /// <summary>
        /// 获取理疗室状态
        /// </summary>
        public async Task<List<TreatmentRoomStatusDto>> GetRoomStatusAsync() {
            var inProgressTreatments = await _repository.GetByStatusAsync("InProgress");
            var roomStatus = new List<TreatmentRoomStatusDto>();
            
            // 默认5个理疗室
            for (int i = 1; i <= 5; i++) {
                // var treatment = inProgressTreatments.FirstOrDefault(t => t.RoomNumber == i); // RoomNumber字段已删除
                TreatmentTaskModel? treatment = null; // 暂时设为null
                roomStatus.Add(new TreatmentRoomStatusDto {
                    RoomNumber = i,
                    RoomName = $"理疗室{i}",
                    Status = treatment != null ? "Occupied" : "Available",
                    // CurrentPatientName = treatment?.PatientName, // PatientName字段已删除
                    CurrentTreatmentType = treatment?.TreatmentType,
                    CurrentStartTime = treatment?.StartTime,
                    TherapistName = null // TherapistName字段已删除
                });
            }
            
            return roomStatus;
        }

        /// <summary>
        /// 获取治疗统计
        /// </summary>
        public async Task<TreatmentStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate) {
            var list = await _repository.GetListAsync();
            // var filtered = list.Where(t => t.CreateTime >= startDate && t.CreateTime <= endDate).ToList(); // CreateTime字段已删除
            var filtered = list; // 暂时返回所有记录
            
            var treatmentTypeStats = filtered
                .GroupBy(t => t.TreatmentType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new TreatmentStatisticsDto {
                TotalTreatments = filtered.Count,
                WaitingCount = filtered.Count(t => t.Status == "Waiting"),
                InProgressCount = filtered.Count(t => t.Status == "InProgress"),
                CompletedCount = filtered.Count(t => t.Status == "Completed"),
                CancelledCount = filtered.Count(t => t.Status == "Cancelled"),
                TotalRevenue = filtered.Sum(t => 0m /* t.Price字段已删除 */),
                AveragePrice = filtered.Any() ? filtered.Average(t => 0m /* t.Price字段已删除 */) : 0,
                AverageDuration = filtered.Where(t => false /* Duration字段已删除 */).Any() ? 
                    filtered.Where(t => false /* Duration字段已删除 */).Average(t => (double)0m /* t.Duration字段已删除 */) : 0,
                TreatmentTypeStats = treatmentTypeStats,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        /// <summary>
        /// 获取今日统计
        /// </summary>
        public async Task<TodayTreatmentStatDto> GetTodayStatisticsAsync() {
            var today = DateTime.Today;
            var list = await _repository.GetListAsync();
            // var todayTreatments = list.Where(t => t.CreateTime.Date == today).ToList(); // CreateTime字段已删除
            var todayTreatments = list; // 暂时返回所有记录
            
            var treatmentTypeCount = todayTreatments
                .GroupBy(t => t.TreatmentType)
                .ToDictionary(g => g.Key, g => g.Count());

            var roomStatus = await GetRoomStatusAsync();

            return new TodayTreatmentStatDto {
                TotalCount = todayTreatments.Count,
                WaitingCount = todayTreatments.Count(t => t.Status == "Waiting"),
                InProgressCount = todayTreatments.Count(t => t.Status == "InProgress"),
                CompletedCount = todayTreatments.Count(t => t.Status == "Completed"),
                CancelledCount = todayTreatments.Count(t => t.Status == "Cancelled"),
                TotalRevenue = todayTreatments.Sum(t => 0m /* t.Price字段已删除 */),
                AverageDuration = todayTreatments.Where(t => false /* Duration字段已删除 */).Any() ? 
                    todayTreatments.Where(t => false /* Duration字段已删除 */).Average(t => (double)0m /* t.Duration字段已删除 */) : 0,
                TreatmentTypeCount = treatmentTypeCount,
                RoomStatus = roomStatus
            };
        }

        #region 现场理疗增强功能

        /// <summary>
        /// 从挂号创建治疗记录
        /// </summary>
        public async Task<TreatmentDetailDto?> CreateFromRegistrationAsync(Guid registrationId, string treatmentType, Guid operatorId, string operatorName) {
            // TODO: 从Registration模块获取挂号信息
            // 这里暂时使用模拟数据，实际应该调用 RegistrationService
            var treatment = new TreatmentTaskModel {
                Id = Guid.NewGuid(),
                // RegistrationId = registrationId, // RegistrationId字段已删除
                PatientId = Guid.NewGuid(), // TODO: 从挂号获取
                // PatientName = "患者姓名", // PatientName字段已删除
                DoctorId = Guid.NewGuid(), // TODO: 从挂号获取
                // DoctorName = "医生姓名", // DoctorName字段已删除
                TreatmentType = treatmentType,
                Status = "Waiting",
                // CreateTime = DateTime.Now, // CreateTime字段已删除
                // Price = GetTreatmentTypePrice(treatmentType) // Price字段已删除
            };

            var result = await _repository.AddAsync(treatment);
            return result ? _mapper.Map<TreatmentDetailDto>(treatment) : null;
        }

        /// <summary>
        /// 批量安排治疗
        /// </summary>
        public async Task<bool> BatchScheduleTreatmentsAsync(List<Guid> treatmentIds, Guid therapistId, string therapistName, Guid operatorId, string operatorName) {
            var results = new List<bool>();
            foreach (var treatmentId in treatmentIds) {
                var treatment = await _repository.GetByIdAsync(treatmentId);
                if (treatment != null && treatment.Status == "Waiting") {
                    // treatment.TherapistId = ...; // TherapistId字段已删除
                    // null /* treatment.TherapistName字段已删除 */ = ...; // TherapistName字段已删除
                    treatment.Status = "Scheduled";
                    results.Add(await _repository.UpdateAsync(treatment));
                } else {
                    results.Add(false);
                }
            }
            return results.All(r => r);
        }

        /// <summary>
        /// 分配治疗室
        /// </summary>
        public async Task<bool> AssignRoomAsync(Guid treatmentId, int roomNumber, Guid operatorId, string operatorName) {
            var treatment = await _repository.GetByIdAsync(treatmentId);
            if (treatment == null) return false;
            // treatment.RoomNumber = roomNumber; // RoomNumber字段已删除
            return await _repository.UpdateAsync(treatment);
        }

        /// <summary>
        /// 获取可用治疗师列表
        /// </summary>
        public async Task<List<TherapistDto>> GetAvailableTherapistsAsync() {
            // TODO: 从User/Doctor模块获取治疗师信息
            // 这里返回模拟数据
            return new List<TherapistDto> {
                new() { Id = Guid.NewGuid(), Name = "张理疗师", Specialty = "推拿", IsAvailable = true, CurrentTreatmentCount = 2 },
                new() { Id = Guid.NewGuid(), Name = "李理疗师", Specialty = "针灸", IsAvailable = true, CurrentTreatmentCount = 1 },
                new() { Id = Guid.NewGuid(), Name = "王理疗师", Specialty = "拔罐", IsAvailable = false, CurrentTreatmentCount = 3 }
            };
        }

        /// <summary>
        /// 治疗进度更新
        /// </summary>
        public async Task<bool> UpdateTreatmentProgressAsync(Guid treatmentId, string progressNotes, Guid operatorId, string operatorName) {
            var treatment = await _repository.GetByIdAsync(treatmentId);
            if (treatment == null) return false;

            treatment.Remark = progressNotes;
            // treatment.UpdateTime = DateTime.Now; // UpdateTime字段已删除
            return await _repository.UpdateAsync(treatment);
        }

        #endregion

        /// <summary>
        /// 根据治疗类型获取价格
        /// </summary>
        private decimal GetTreatmentTypePrice(string treatmentType) {
            return treatmentType switch {
                "针灸" => 80m,
                "推拿" => 60m,
                "拔罐" => 40m,
                "电疗" => 50m,
                "艾灸" => 70m,
                _ => 50m
            };
        }
    }
}