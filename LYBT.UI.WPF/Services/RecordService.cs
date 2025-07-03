using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class RecordService : IRecordService {
        private readonly IRecordApi _api;
        public RecordService(IRecordApi api) {
            _api = api;
        }

        public async Task<IList<RecordDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<IList<RecordDto>> GetByPatientIdAsync(Guid patientId) {
            return await _api.GetByPatientIdAsync(patientId);
        }

        public async Task<RecordDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(RecordCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(RecordEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }

        public async Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds) {
            var resp = await _api.MarkAsSharedAsync(id, doctorIds);
            return resp.Success;
        }

        public async Task<bool> RevokeSharingAsync(Guid id) {
            var resp = await _api.RevokeSharingAsync(id);
            return resp.Success;
        }

        public async Task<IList<RecordDto>> GetSharedRecordsAsync(Guid doctorId) {
            return await _api.GetSharedRecordsAsync(doctorId);
        }
    }
}
