using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 RecordService 的说明
    /// </summary>
    public class RecordService : IRecordService {
        private readonly IRecordApi _api;
        public RecordService(IRecordApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<RecordDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByPatientIdAsync 的说明
        /// </summary>
        public async Task<IList<RecordDto>> GetByPatientIdAsync(Guid patientId) {
            return await _api.GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<RecordDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(RecordCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(RecordEditDto dto) {
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

        /// <summary>
        /// 方法 MarkAsSharedAsync 的说明
        /// </summary>
        public async Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds) {
            var resp = await _api.MarkAsSharedAsync(id, doctorIds);
            return resp.Success;
        }

        /// <summary>
        /// 方法 RevokeSharingAsync 的说明
        /// </summary>
        public async Task<bool> RevokeSharingAsync(Guid id) {
            var resp = await _api.RevokeSharingAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetSharedRecordsAsync 的说明
        /// </summary>
        public async Task<IList<RecordDto>> GetSharedRecordsAsync(Guid doctorId) {
            return await _api.GetSharedRecordsAsync(doctorId);
        }
    }
}
