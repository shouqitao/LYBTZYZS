using LYBT.Entities.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces {

    /// <summary>
    /// 表示IPrescriptionRepository。
    /// </summary>
    public interface IPrescriptionRepository {

        Task<Prescription?> GetByIdAsync(Guid id);

        Task<List<Prescription>> GetListAsync();

        Task<bool> AddAsync(Prescription model);

        Task<bool> UpdateAsync(Prescription model);

        Task<bool> DeleteAsync(Guid id);

        Task<bool> CancelAsync(Guid id);
    }
}
