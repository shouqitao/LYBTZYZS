using LYBT.Module.TreatmentRoom.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface ITreatmentRoomService {
        Task<IList<TreatmentRoomDto>> GetListAsync();
        Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(TreatmentRoomCreateDto dto);
        Task<bool> UpdateAsync(TreatmentRoomEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IList<TreatmentRoomDto>> GetByStatusAsync(string status);
    }
}
