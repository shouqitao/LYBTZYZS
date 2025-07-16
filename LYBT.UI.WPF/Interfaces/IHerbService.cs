using LYBT.Module.Herbs.Dtos;
using LYBT.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IHerbService {
        Task<IList<HerbDto>> GetListAsync();
        Task<PagedResultDto<HerbDto>> GetPagedAsync(HerbPagedQueryDto query);
        Task<HerbDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(HerbDetailDto dto);
        Task<bool> UpdateAsync(HerbDetailDto dto);
        Task<bool> DeleteAsync(Guid id);

        Task<int> ImportAsync(IList<HerbDetailDto> dtos);

        Task<IList<HerbDetailDto>> ExportAsync();

        Task<int> ImportFromExcelAsync(string path);

        Task<int> ExportToExcelAsync(string path);
    }
}
