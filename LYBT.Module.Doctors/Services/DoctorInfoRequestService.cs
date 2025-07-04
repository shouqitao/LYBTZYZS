using LYBT.Common.Enums;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;

namespace LYBT.Module.Doctors.Services {
    /// <summary>
    /// 医生信息申请业务实现
    /// </summary>
    public class DoctorInfoRequestService : IDoctorInfoRequestService {
        private readonly IDoctorInfoRequestRepository _repository;
        private readonly IDoctorRepository _doctorRepository;
        public DoctorInfoRequestService(IDoctorInfoRequestRepository repository, IDoctorRepository doctorRepository) {
            _repository = repository;
            _doctorRepository = doctorRepository;
        }

        public async Task<bool> SubmitAsync(DoctorInfoRequestModel model) {
            var existing = model.Id != Guid.Empty ? await _repository.GetByIdAsync(model.Id) : null;
            if (existing == null) {
                model.Id = Guid.NewGuid();
                model.Status = DoctorInfoRequestStatus.Pending;
                model.CreatedTime = DateTime.Now;
                await _repository.AddAsync(model);
                return true;
            } else {
                if (existing.Status != DoctorInfoRequestStatus.Pending)
                    return false;
                existing.Name = model.Name;
                existing.Phone = model.Phone;
                existing.Gender = model.Gender;
                existing.Birthday = model.Birthday;
                existing.PinyinCode = model.PinyinCode;
                existing.LicenseNumber = model.LicenseNumber;
                existing.Title = model.Title;
                existing.DoctorStatus = model.DoctorStatus;
                existing.Remark = model.Remark;
                existing.Status = DoctorInfoRequestStatus.Pending;
                await _repository.UpdateAsync(existing);
                return true;
            }
        }

        public Task<List<DoctorInfoRequestModel>> GetPendingListAsync() {
            return _repository.GetPendingListAsync();
        }

        public async Task<bool> ApproveAsync(Guid id) {
            var request = await _repository.GetByIdAsync(id);
            if (request == null || request.Status != DoctorInfoRequestStatus.Pending)
                return false;
            var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
            if (doctor == null)
                return false;
            doctor.Gender = request.Gender;
            doctor.Birthday = request.Birthday;
            doctor.PinyinCode = request.PinyinCode;
            doctor.LicenseNumber = request.LicenseNumber;
            doctor.Title = request.Title;
            doctor.Status = request.DoctorStatus;
            doctor.Remark = request.Remark;
            doctor.User.RealName = request.Name;
            doctor.User.PhoneNumber = request.Phone;
            await _doctorRepository.UpdateAsync(doctor);
            request.Status = DoctorInfoRequestStatus.Approved;
            await _repository.UpdateAsync(request);
            return true;
        }

        public async Task<bool> RejectAsync(Guid id) {
            var request = await _repository.GetByIdAsync(id);
            if (request == null || request.Status != DoctorInfoRequestStatus.Pending)
                return false;
            request.Status = DoctorInfoRequestStatus.Rejected;
            await _repository.UpdateAsync(request);
            return true;
        }
    }
}
