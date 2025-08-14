using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Repositories
{

    /// <summary>
    /// 表示IPrescriptionRepository。
    /// </summary>
    public interface IPrescriptionRepository
    {

        Task<PrescriptionModel?> GetByIdAsync(Guid id);

        Task<List<PrescriptionModel>> GetListAsync();

        Task<bool> AddAsync(PrescriptionModel model);

        Task<bool> UpdateAsync(PrescriptionModel model);

        Task<bool> DeleteAsync(Guid id);

        Task<bool> CancelAsync(Guid id);
    }
}