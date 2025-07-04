using LYBT.Models.Doctors;

namespace LYBT.Module.Doctors.Interfaces {
    /// <summary>
    /// 医生信息申请仓储接口
    /// </summary>
    public interface IDoctorInfoRequestRepository {
        Task<DoctorInfoRequestModel?> GetByIdAsync(Guid id);
        Task AddAsync(DoctorInfoRequestModel model);
        Task UpdateAsync(DoctorInfoRequestModel model);
        Task<List<DoctorInfoRequestModel>> GetPendingListAsync();
    }
}
