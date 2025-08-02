using LYBT.Models.Records;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Records.Interfaces {

    /// <summary>
    /// 病历仓储接口，定义病历数据操作方法
    /// </summary>
    public interface IRecordRepository {

        /// <summary>
        /// 根据病历ID获取病历记录
        /// </summary>
        Task<RecordModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有病历记录列表
        /// </summary>
        Task<List<RecordModel>> GetListAsync();

        /// <summary>
        /// 分页查询病历列表
        /// </summary>
        Task<(List<RecordModel> list, int total)> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增病历记录
        /// </summary>
        Task<bool> AddAsync(RecordModel recordModel);

        /// <summary>
        /// 更新病历记录
        /// </summary>
        Task<bool> UpdateAsync(RecordModel recordModel);

        /// <summary>
        /// 删除病历记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        Task<List<RecordModel>> GetListByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取共享给某医生的病历
        /// </summary>
        Task<List<RecordModel>> GetSharedRecordsAsync(Guid doctorId);
    }
}