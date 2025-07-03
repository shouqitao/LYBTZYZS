using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 PatientService 的说明
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientApi _api;
        public PatientService(IPatientApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(PatientCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(PatientEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 EnableAsync 的说明
        /// </summary>
        public async Task<bool> EnableAsync(Guid id) {
            var resp = await _api.EnableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DisableAsync 的说明
        /// </summary>
        public async Task<bool> DisableAsync(Guid id) {
            var resp = await _api.DisableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 GetAllAsync 的说明
        /// </summary>
        public async Task<IList<PatientDto>> GetAllAsync() {
            return await _api.GetAllAsync();
        }

        /// <summary>
        /// 方法 GetPagedAsync 的说明
        /// </summary>
        public async Task<PagedResultDto<PatientDto>> GetPagedAsync(PatientPagedQueryDto query) {
            return await _api.GetPagedAsync(query);
        }

        /// <summary>
        /// 方法 BatchDeleteAsync 的说明
        /// </summary>
        public async Task<int> BatchDeleteAsync(List<string> ids) {
            var resp = await _api.BatchDeleteAsync(ids);
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 BatchDisableAsync 的说明
        /// </summary>
        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var resp = await _api.BatchDisableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 SearchAsync 的说明
        /// </summary>
        public async Task<IList<PatientDto>> SearchAsync(string keyword) {
            return await _api.SearchAsync(keyword);
        }

        /// <summary>
        /// 方法 GetForDoctorAsync 的说明
        /// </summary>
        public async Task<IList<PatientDto>> GetForDoctorAsync(Guid doctorId) {
            return await _api.GetForDoctorAsync(doctorId);
        }

        /// <summary>
        /// 方法 AssignDoctorAsync 的说明
        /// </summary>
        public async Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId) {
            var resp = await _api.AssignDoctorAsync(patientId, new AssignDoctorDto { DoctorId = doctorId });
            return resp.Success;
        }

        /// <summary>
        /// 方法 ImportAsync 的说明
        /// </summary>
        public async Task<int> ImportAsync(List<PatientCreateDto> dtos) {
            var resp = await _api.ImportAsync(dtos);
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 ExportAsync 的说明
        /// </summary>
        public async Task<IList<PatientDto>> ExportAsync() {
            return await _api.ExportAsync();
        }

        /// <summary>
        /// 方法 GetHistoryAsync 的说明
        /// </summary>
        public async Task<IList<RecordDto>> GetHistoryAsync(Guid patientId) {
            return await _api.GetHistoryAsync(patientId);
        }
    }
}
