using LYBT.Models.Doctors;

namespace LYBT.Module.Doctors.Interfaces {
    /// <summary>
    /// 医生信息申请业务接口
    /// </summary>
    public interface IDoctorInfoRequestService {
        Task<bool> SubmitAsync(DoctorInfoRequestModel model);
        Task<List<DoctorInfoRequestModel>> GetPendingListAsync();
        Task<bool> ApproveAsync(Guid id);
        Task<bool> RejectAsync(Guid id);
    }
}
