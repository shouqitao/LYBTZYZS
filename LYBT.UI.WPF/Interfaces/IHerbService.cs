using LYBT.Module.Herbs.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IHerbService {
        Task<IList<HerbDto>> GetListAsync();
        Task<HerbDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(HerbDetailDto dto);
        Task<bool> UpdateAsync(HerbDetailDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
