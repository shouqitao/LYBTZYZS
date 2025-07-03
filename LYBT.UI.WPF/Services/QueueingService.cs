using LYBT.Module.Queueing.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class QueueingService : IQueueingService {
        private readonly IQueueingApi _api;

        public QueueingService(IQueueingApi api) {
            _api = api;
        }

        public async Task<IList<QueueingDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<QueueingDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(QueueingCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(QueueingEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
