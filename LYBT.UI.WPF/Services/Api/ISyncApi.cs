using LYBT.Common.Enums;
using LYBT.Module.Sync.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services.Api {
    public interface ISyncApi {
        // ======== Log related ========
        [Get("/api/Sync/logs")]
        Task<List<SyncLogDto>> GetLogListAsync();

        [Get("/api/Sync/logs/last")]
        Task<SyncLogDto?> GetLastLogAsync();

        [Get("/api/Sync/logs/paged")]
        Task<List<SyncLogDto>> GetLogPagedAsync([Query] int page = 1, [Query] int pageSize = 20);

        [Post("/api/Sync/logs")]
        Task<ApiSuccessResponse> AddLogAsync([Body] SyncLogCreateDto dto);

        [Delete("/api/Sync/logs/{id}")]
        Task<ApiSuccessResponse> DeleteLogAsync(string id);

        // ======== Connection and mode ========
        [Get("/api/Sync/connection-status")]
        Task<bool> CheckConnectionAsync();

        [Post("/api/Sync/manual-sync")]
        Task<ApiSuccessResponse> ManualSyncAsync();

        [Get("/api/Sync/mode")]
        Task<SyncMode> GetSyncModeAsync();

        [Post("/api/Sync/mode")]
        Task<ApiSuccessResponse> SetSyncModeAsync([Body] SyncMode mode);

        // ======== Task related ========
        [Get("/api/Sync/tasks")]
        Task<List<SyncTaskDto>> GetTaskListAsync();

        [Get("/api/Sync/tasks/{id}")]
        Task<SyncTaskDetailDto> GetTaskDetailAsync(Guid id);

        [Post("/api/Sync/tasks")]
        Task<ApiSuccessResponse> AddTaskAsync([Body] SyncTaskCreateDto dto);

        [Put("/api/Sync/tasks")]
        Task<ApiSuccessResponse> UpdateTaskAsync([Body] SyncTaskEditDto dto);

        [Delete("/api/Sync/tasks/{id}")]
        Task<ApiSuccessResponse> DeleteTaskAsync(Guid id);
    }
}
