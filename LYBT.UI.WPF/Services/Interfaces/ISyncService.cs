using LYBT.Common.Enums;
using LYBT.Module.Sync.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface ISyncService {
        // log related
        Task<IList<SyncLogDto>> GetLogListAsync();
        Task<SyncLogDto?> GetLastLogAsync();
        Task<IList<SyncLogDto>> GetLogPagedAsync(int page = 1, int pageSize = 20);
        Task<bool> AddLogAsync(SyncLogCreateDto dto);
        Task<bool> DeleteLogAsync(string id);

        // connection & manual sync
        Task<bool> CheckConnectionAsync();
        Task<bool> ManualSyncAsync();
        Task<SyncMode> GetSyncModeAsync();
        Task<bool> SetSyncModeAsync(SyncMode mode);

        // tasks
        Task<IList<SyncTaskDto>> GetTaskListAsync();
        Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id);
        Task<bool> AddTaskAsync(SyncTaskCreateDto dto);
        Task<bool> UpdateTaskAsync(SyncTaskEditDto dto);
        Task<bool> DeleteTaskAsync(Guid id);
    }
}
