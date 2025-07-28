using LYBT.Models.Prescriptions;

namespace LYBT.Module.Prescriptions.Services {

    /// <summary>
    /// 处方业务接口定义
    /// </summary>
    public interface IPrescriptionService {

        Task<List<PrescriptionDto>> GetAllAsync();

        Task<PrescriptionDetailDto?> GetByIdAsync(string id);

        Task<bool> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName);

        Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName);

        Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName);

        Task<bool> CancelAsync(string id, Guid operatorId, string operatorName);
    }
}