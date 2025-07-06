using LYBT.Module.Settings.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface ISettingsService {
        Task<IList<SettingsDto>> GetListAsync();
        Task<SettingsDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(SettingsCreateDto dto);
        Task<bool> UpdateAsync(SettingsEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
