using LYBT.Common.Enums;
using LYBT.Module.Sync.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 SyncService 的说明
    /// </summary>
    public class SyncService : ISyncService {
        private readonly ISyncApi _api;

        public SyncService(ISyncApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetLogListAsync 的说明
        /// </summary>
        public async Task<IList<SyncLogDto>> GetLogListAsync() {
            return await _api.GetLogListAsync();
        }

        /// <summary>
        /// 方法 GetLastLogAsync 的说明
        /// </summary>
        public async Task<SyncLogDto?> GetLastLogAsync() {
            return await _api.GetLastLogAsync();
        }

        /// <summary>
        /// 方法 GetLogPagedAsync 的说明
        /// </summary>
        public async Task<IList<SyncLogDto>> GetLogPagedAsync(int page = 1, int pageSize = 20) {
            return await _api.GetLogPagedAsync(page, pageSize);
        }

        /// <summary>
        /// 方法 AddLogAsync 的说明
        /// </summary>
        public async Task<bool> AddLogAsync(SyncLogCreateDto dto) {
            var resp = await _api.AddLogAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteLogAsync 的说明
        /// </summary>
        public async Task<bool> DeleteLogAsync(string id) {
            var resp = await _api.DeleteLogAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 CheckConnectionAsync 的说明
        /// </summary>
        public async Task<bool> CheckConnectionAsync() {
            return await _api.CheckConnectionAsync();
        }

        /// <summary>
        /// 方法 ManualSyncAsync 的说明
        /// </summary>
        public async Task<bool> ManualSyncAsync() {
            var resp = await _api.ManualSyncAsync();
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetSyncModeAsync 的说明
        /// </summary>
        public async Task<SyncMode> GetSyncModeAsync() {
            return await _api.GetSyncModeAsync();
        }

        /// <summary>
        /// 方法 SetSyncModeAsync 的说明
        /// </summary>
        public async Task<bool> SetSyncModeAsync(SyncMode mode) {
            var resp = await _api.SetSyncModeAsync(mode);
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetTaskListAsync 的说明
        /// </summary>
        public async Task<IList<SyncTaskDto>> GetTaskListAsync() {
            return await _api.GetTaskListAsync();
        }

        /// <summary>
        /// 方法 GetTaskDetailAsync 的说明
        /// </summary>
        public async Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id) {
            return await _api.GetTaskDetailAsync(id);
        }

        /// <summary>
        /// 方法 AddTaskAsync 的说明
        /// </summary>
        public async Task<bool> AddTaskAsync(SyncTaskCreateDto dto) {
            var resp = await _api.AddTaskAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateTaskAsync 的说明
        /// </summary>
        public async Task<bool> UpdateTaskAsync(SyncTaskEditDto dto) {
            var resp = await _api.UpdateTaskAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteTaskAsync 的说明
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id) {
            var resp = await _api.DeleteTaskAsync(id);
            return resp.Success;
        }
    }
}
