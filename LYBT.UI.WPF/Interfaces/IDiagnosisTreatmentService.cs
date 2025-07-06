using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IDiagnosisTreatmentService {
        Task<IList<DiagnosisTreatmentDto>> GetListAsync();
        Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto);
        Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
