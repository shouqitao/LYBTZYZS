using LYBT.Module.Queueing.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IQueueingService {
        Task<IList<QueueingDto>> GetListAsync();
        Task<QueueingDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(QueueingCreateDto dto);
        Task<bool> UpdateAsync(QueueingEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CompleteAsync(Guid id);
        Task<bool> HoldAsync(Guid id);
    }
}
