using LYBT.Module.Records.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IRecordService {
        Task<IList<RecordDto>> GetListAsync();
        Task<IList<RecordDto>> GetByPatientIdAsync(Guid patientId);
        Task<RecordDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(RecordCreateDto dto);
        Task<bool> UpdateAsync(RecordEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds);
        Task<bool> RevokeSharingAsync(Guid id);
        Task<IList<RecordDto>> GetSharedRecordsAsync(Guid doctorId);
    }
}
