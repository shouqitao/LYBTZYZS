using LYBT.Module.Prescriptions.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    /// <summary>
    /// 处方服务接口
    /// </summary>
    public interface IPrescriptionService {
        Task<IList<PrescriptionDto>> GetListAsync();
        Task<PrescriptionDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(PrescriptionCreateDto dto);
        Task<bool> UpdateAsync(PrescriptionEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CancelAsync(Guid id);
    }
}
