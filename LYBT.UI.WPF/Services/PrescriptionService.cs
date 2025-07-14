using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 处方服务实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {
        private readonly IPrescriptionApi _api;

        public PrescriptionService(IPrescriptionApi api) {
            _api = api;
        }

        public async Task<IList<PrescriptionDto>> GetListAsync() => await _api.GetListAsync();

        public async Task<PrescriptionDetailDto?> GetByIdAsync(Guid id) => await _api.GetByIdAsync(id);

        public async Task<bool> AddAsync(PrescriptionDetailDto dto) {
            var create = new PrescriptionCreateDto {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Diagnosis = dto.Diagnosis,
                Remark = dto.Remark,
                Status = dto.Status,
                Items = dto.Items.ConvertAll(i => new PrescriptionItemCreateDto {
                    HerbId = i.HerbId,
                    HerbName = i.HerbName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Usage = i.Usage
                })
            };
            var resp = await _api.AddAsync(create);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(PrescriptionDetailDto dto) {
            var edit = new PrescriptionEditDto {
                Id = dto.Id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Diagnosis = dto.Diagnosis,
                Remark = dto.Remark,
                Status = dto.Status,
                Items = dto.Items.ConvertAll(i => new PrescriptionItemCreateDto {
                    HerbId = i.HerbId,
                    HerbName = i.HerbName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Usage = i.Usage
                })
            };
            var resp = await _api.UpdateAsync(edit);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }

        public async Task<bool> CancelAsync(Guid id) {
            var resp = await _api.CancelAsync(id);
            return resp.Success;
        }
    }
}
