using LYBT.Module.Prescriptions.Models;

namespace LYBT.Module.Prescriptions.Services {

    /// <summary>
    /// 处方业务接口定义
    /// </summary>
    public interface IPrescriptionService {

        Task<List<PrescriptionModel>> GetAllAsync();

        Task<PrescriptionModel> GetByIdAsync(string id);

        Task<bool> CreateAsync(PrescriptionModel prescription, Guid operatorId, string operatorName);

        Task<bool> UpdateAsync(PrescriptionModel prescription, Guid operatorId, string operatorName);

        Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName);
    }
}