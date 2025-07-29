using AutoMapper;
using LYBT.Common.Enums.System;
using LYBT.Models.Sync;
using LYBT.Module.Sync.Interfaces;

namespace LYBT.Module.Sync.Services {

    /// <summary>
    /// 数据同步业务服务实现类，封装同步任务与同步日志业务逻辑
    /// </summary>
    public class SyncService : ISyncService {
        private readonly ISyncRepository _syncRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储和对象映射器
        /// </summary>
        public SyncService(ISyncRepository syncRepository, IMapper mapper) {
            _syncRepository = syncRepository;
            _mapper = mapper;
        }

        // ========== 日志相关 ==========

        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        public async Task<List<SyncLogDto>> GetLogListAsync() {
            var list = await _syncRepository.GetLogListAsync();
            return _mapper.Map<List<SyncLogDto>>(list);
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        public async Task<bool> AddLogAsync(SyncLogCreateDto syncLogCreateDto) {
            var model = _mapper.Map<SyncLogModel>(syncLogCreateDto);
            model.Id = Guid.NewGuid().ToString();
            model.SyncTime = DateTime.Now;
            return await _syncRepository.AddLogAsync(model);
        }

        /// <summary>
        /// 删除同步日志
        /// </summary>
        public async Task<bool> DeleteLogAsync(string id) {
            return await _syncRepository.DeleteLogAsync(id);
        }

        /// <summary>
        /// 获取最近一次同步信息
        /// </summary>
        public async Task<SyncLogDto?> GetLastSyncInfoAsync() {
            var model = await _syncRepository.GetLastLogAsync();
            return model == null ? null : _mapper.Map<SyncLogDto>(model);
        }

        /// <summary>
        /// 分页获取同步日志
        /// </summary>
        public async Task<List<SyncLogDto>> GetSyncLogPagedAsync(int page, int pageSize) {
            var list = await _syncRepository.GetLogPagedAsync(page, pageSize);
            return _mapper.Map<List<SyncLogDto>>(list);
        }

        /// <summary>
        /// 检测中心数据库是否可连接
        /// </summary>
        public async Task<bool> CheckConnectionStatusAsync() {
            return await _syncRepository.CanConnectAsync();
        }

        /// <summary>
        /// 手动触发同步操作
        /// </summary>
        public async Task<bool> TriggerManualSyncAsync() {
            var log = new SyncLogCreateDto {
                Mode = SyncMode.Manual,
                Status = SyncStatus.Completed,
                Message = "Manual sync triggered"
            };
            return await AddLogAsync(log);
        }

        // ========== 任务相关 ==========

        /// <summary>
        /// 获取同步任务列表
        /// </summary>
        public async Task<List<SyncTaskDto>> GetTaskListAsync() {
            var list = await _syncRepository.GetTaskListAsync();
            return _mapper.Map<List<SyncTaskDto>>(list);
        }

        /// <summary>
        /// 获取同步任务详情
        /// </summary>
        public async Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id) {
            var model = await _syncRepository.GetTaskByIdAsync(id);
            return model == null ? null : _mapper.Map<SyncTaskDetailDto>(model);
        }

        /// <summary>
        /// 新增同步任务
        /// </summary>
        public async Task<bool> AddTaskAsync(SyncTaskCreateDto syncTaskCreateDto) {
            var model = _mapper.Map<SyncTaskModel>(syncTaskCreateDto);
            model.Id = Guid.NewGuid();
            model.TriggerTime = DateTime.Now;
            return await _syncRepository.AddTaskAsync(model);
        }

        /// <summary>
        /// 更新同步任务
        /// </summary>
        public async Task<bool> UpdateTaskAsync(SyncTaskEditDto syncTaskEditDto) {
            var model = await _syncRepository.GetTaskByIdAsync(syncTaskEditDto.Id);
            if (model == null)
                return false;
            model.Status = syncTaskEditDto.Status;
            model.ExecuteTime = syncTaskEditDto.ExecuteTime;
            model.Remark = syncTaskEditDto.Remark;
            return await _syncRepository.UpdateTaskAsync(model);
        }

        /// <summary>
        /// 删除同步任务
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id) {
            return await _syncRepository.DeleteTaskAsync(id);
        }
    }
}