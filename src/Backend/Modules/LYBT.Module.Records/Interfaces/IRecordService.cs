using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Records.Interfaces {

    /// <summary>
    /// 病历业务服务接口
    /// </summary>
    public interface IRecordService {

        /// <summary>
        /// 根据ID获取病历详情
        /// </summary>
        Task<RecordDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取病历列表
        /// </summary>
        Task<List<RecordDto>> GetListAsync();

        /// <summary>
        /// 分页查询病历列表
        /// </summary>
        Task<PaginatedResult<RecordDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增病历
        /// </summary>
        Task<RecordDto?> AddAsync(RecordCreateDto recordCreateDto, Guid operatorId, string operatorName);

        /// <summary>
        /// 编辑病历
        /// </summary>
        Task<bool> UpdateAsync(RecordEditDto recordEditDto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除病历
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        Task<List<RecordDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 标记共享病历
        /// </summary>
        Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds);

        /// <summary>
        /// 撤销病历共享
        /// </summary>
        Task<bool> RevokeSharingAsync(Guid id);

        /// <summary>
        /// 获取共享给当前医生的病历
        /// </summary>
        Task<List<RecordDto>> GetSharedRecordsAsync(Guid doctorId);
    }
}