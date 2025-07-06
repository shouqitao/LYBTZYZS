using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 患者服务实现（WPF前端）
    /// </summary>
    public class PatientService : Interfaces.IPatientService {
        private readonly IPatientApi _api;
        public PatientService(IPatientApi api) {
            _api = api;
        }

        public async Task<bool> AddAsync(PatientDetailDto dto) {
            return await _api.AddAsync(dto);
        }

        public async Task<bool> UpdateAsync(PatientDetailDto dto) {
            return await _api.UpdateAsync(dto);
        }

        public async Task<PatientDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<IList<PatientDetailDto>> GetAllAsync() {
            return await _api.GetAllAsync();
        }

        public async Task<IList<PatientDetailDto>> SearchAsync(string keyword) {
            return await _api.SearchAsync(keyword);
        }

        public async Task<bool> DeleteAsync(Guid id) {
            // 可根据后端API实际情况实现
            // return await _api.DeleteAsync(id);
            return false;
        }
    }
}