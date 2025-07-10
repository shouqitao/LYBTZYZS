using LYBT.Models.Prescriptions;

namespace LYBT.Module.Prescriptions.Repositories {
    public interface IPrescriptionRepository {
        Task<PrescriptionModel?> GetByIdAsync(Guid id);
        Task<List<PrescriptionModel>> GetListAsync();
        Task<bool> AddAsync(PrescriptionModel model);
        Task<bool> UpdateAsync(PrescriptionModel model);
        Task<bool> DeleteAsync(Guid id);
    }
}
