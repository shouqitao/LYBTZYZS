using LYBT.Module.Queueing.Dtos;
using LYBT.UI.WPF.Apis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 QueueingService 的说明
    /// </summary>
    public class QueueingService : IQueueingService {
        private readonly IQueueingApi _api;

        public QueueingService(IQueueingApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<QueueingDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<QueueingDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(QueueingCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(QueueingEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteAsync 的说明
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
