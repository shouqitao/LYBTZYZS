using LYBT.Module.TreatmentRoom.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class TreatmentRoomService : ITreatmentRoomService {
        private readonly ITreatmentRoomApi _api;

        public TreatmentRoomService(ITreatmentRoomApi api) {
            _api = api;
        }

        public async Task<IList<TreatmentRoomDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(TreatmentRoomCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(TreatmentRoomEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
