using System.Threading.Tasks;
using System.Linq;
using System;
﻿using AutoMapper;
using LYBT.Models.Queueing;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Queueing.Services {

    /// <summary>
    /// 排队业务服务实现类（现场叫号模式）
    /// </summary>
    public class QueueingService : IQueueingService {
        private readonly IQueueingRepository _repository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public QueueingService(IQueueingRepository repository, IMapper mapper) {
            _repository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取排队详情
        /// </summary>
        public async Task<QueueingDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<QueueingDetailDto>(model);
        }

        /// <summary>
        /// 获取排队列表
        /// </summary>
        public async Task<List<QueueingDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<QueueingDto>>(list);
        }

        /// <summary>
        /// 分页获取排队列表
        /// </summary>
        public async Task<PaginatedResult<QueueingDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var allList = await _repository.GetListAsync();
            var dtoList = _mapper.Map<List<QueueingDto>>(allList);

            var filteredList = dtoList.AsQueryable();

            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    (x.PatientName != null && x.PatientName.Contains(query.SearchKeyword)) ||
                    (x.DoctorName != null && x.DoctorName.Contains(query.SearchKeyword)) ||
                    (x.QueueType != null && x.QueueType.Contains(query.SearchKeyword))
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<QueueingDto>(pagedList, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 新增排队（通常由挂号时自动创建）
        /// </summary>
        public async Task<QueueingDto?> AddAsync(QueueingCreateDto dto) {
            var model = _mapper.Map<QueueingModel>(dto);
            model.Id = Guid.NewGuid();
            model.QueueTime = DateTime.Now;
            model.Status = QueueStatus.Waiting;
            model.CreateTime = DateTime.Now;
            
            // 获取当天该医生的排队号
            if (model.QueueNumber == 0) {
                model.QueueNumber = await GetNextQueueNumberAsync(dto.DoctorId);
            }
            
            var result = await _repository.AddAsync(model);
            if (!result)
                return null;
            
            return _mapper.Map<QueueingDto>(model);
        }

        /// <summary>
        /// 编辑排队信息
        /// </summary>
        public async Task<bool> UpdateAsync(QueueingEditDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.QueueType = dto.QueueType;
            model.Remark = dto.Remark;
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除排队信息
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// 取消排队，更新状态为已取消
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            return await _repository.CancelAsync(id);
        }

        /// <summary>
        /// 执行CompleteAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> CompleteAsync(Guid id) {
            return await _repository.CompleteAsync(id);
        }

        /// <summary>
        /// 暂停排队（暂时离开）
        /// </summary>
        public async Task<bool> HoldAsync(Guid id) {
            return await _repository.HoldAsync(id);
        }

        #region 现场叫号特有功能

        /// <summary>
        /// 获取下一个排队号
        /// </summary>
        private async Task<int> GetNextQueueNumberAsync(Guid doctorId) {
            var today = DateTime.Today;
            var todayQueues = await GetTodayQueuesAsync(doctorId);
            return todayQueues.Count + 1;
        }

        /// <summary>
        /// 获取今日排队列表
        /// </summary>
        public async Task<List<QueueingDto>> GetTodayQueuesAsync(Guid? doctorId = null) {
            var allQueues = await _repository.GetListAsync();
            var today = DateTime.Today;
            
            var todayQueues = allQueues.Where(q => 
                q.QueueTime.Date == today &&
                (doctorId == null || q.DoctorId == doctorId)
            ).OrderBy(q => q.QueueNumber).ToList();
            
            return _mapper.Map<List<QueueingDto>>(todayQueues);
        }

        /// <summary>
        /// 获取当前正在就诊的排队
        /// </summary>
        public async Task<QueueingDto?> GetCurrentQueueAsync(Guid doctorId) {
            var todayQueues = await GetTodayQueuesAsync(doctorId);
            var current = todayQueues.FirstOrDefault(q => q.Status == QueueStatus.InProgress);
            return current;
        }

        /// <summary>
        /// 获取下一个等待的排队
        /// </summary>
        public async Task<QueueingDto?> GetNextWaitingQueueAsync(Guid doctorId) {
            var todayQueues = await GetTodayQueuesAsync(doctorId);
            var next = todayQueues
                .Where(q => q.Status == QueueStatus.Waiting)
                .OrderBy(q => q.QueueNumber)
                .FirstOrDefault();
            return next;
        }

        /// <summary>
        /// 叫号（开始就诊）
        /// </summary>
        public async Task<bool> CallNextAsync(Guid doctorId, Guid operatorId, string operatorName) {
            // 先完成当前就诊
            var current = await GetCurrentQueueAsync(doctorId);
            if (current != null) {
                await CompleteAsync(current.Id);
            }
            
            // 叫下一位
            var next = await GetNextWaitingQueueAsync(doctorId);
            if (next == null) {
                return false; // 没有等待的患者
            }
            
            // 更新状态为就诊中
            var model = await _repository.GetByIdAsync(next.Id);
            if (model != null) {
                model.Status = QueueStatus.InProgress;
                model.ActualTime = DateTime.Now;
                model.UpdateTime = DateTime.Now;
                return await _repository.UpdateAsync(model);
            }
            
            return false;
        }

        /// <summary>
        /// 重新排队（过号重排）
        /// </summary>
        public async Task<bool> RequeueAsync(Guid queueId, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(queueId);
            if (model == null || model.Status != QueueStatus.Missed) {
                return false;
            }
            
            // 重新设置为等待状态，但保留原排队号
            model.Status = QueueStatus.Waiting;
            model.UpdateTime = DateTime.Now;
            
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 过号处理
        /// </summary>
        public async Task<bool> MarkAsMissedAsync(Guid queueId, Guid operatorId, string operatorName) {
            var model = await _repository.GetByIdAsync(queueId);
            if (model == null) {
                return false;
            }
            
            model.Status = QueueStatus.Missed;
            model.UpdateTime = DateTime.Now;
            
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 获取排队统计
        /// </summary>
        public async Task<QueueStatisticsDto> GetStatisticsAsync(Guid? doctorId = null) {
            var todayQueues = await GetTodayQueuesAsync(doctorId);
            
            return new QueueStatisticsDto {
                TotalCount = todayQueues.Count,
                WaitingCount = todayQueues.Count(q => q.Status == QueueStatus.Waiting),
                InProgressCount = todayQueues.Count(q => q.Status == QueueStatus.InProgress),
                CompletedCount = todayQueues.Count(q => q.Status == QueueStatus.Completed),
                MissedCount = todayQueues.Count(q => q.Status == QueueStatus.Missed),
                CancelledCount = todayQueues.Count(q => q.Status == QueueStatus.Cancelled),
                AverageWaitingMinutes = CalculateAverageWaitingTime(todayQueues)
            };
        }

        /// <summary>
        /// 计算平均等待时间
        /// </summary>
        private double CalculateAverageWaitingTime(List<QueueingDto> queues) {
            var completed = queues.Where(q => 
                q.Status == QueueStatus.Completed && 
                q.ActualTime.HasValue
            ).ToList();
            
            if (!completed.Any()) {
                return 0;
            }
            
            var totalMinutes = completed.Sum(q => 
                (q.ActualTime!.Value - q.QueueTime).TotalMinutes
            );
            
            return totalMinutes / completed.Count;
        }

        /// <summary>
        /// 插队（VIP或加急）
        /// </summary>
        public async Task<bool> InsertQueueAsync(Guid queueId, int position, Guid operatorId, string operatorName) {
            // TODO: 实现插队逻辑
            await Task.CompletedTask;
            return true;
        }

        #endregion
    }
}