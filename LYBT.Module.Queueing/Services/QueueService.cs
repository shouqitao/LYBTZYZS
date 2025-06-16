using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Models;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Models.Queueing;
using LYBT.Common.Enums;
using LYBT.Module.Queueing.Dtos;

namespace LYBT.Module.Queueing.Services {
    /// <summary>
    /// 排队业务服务实现类
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
        /// 新增排队
        /// </summary>
        public async Task<bool> AddAsync(QueueingCreateDto dto) {
            var model = _mapper.Map<QueueingModel>(dto);
            model.Id = Guid.NewGuid();
            model.QueueTime = DateTime.Now;
            model.Status = QueueStatus.Waiting;
            return await _repository.AddAsync(model);
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
    }
}
