using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Records.Dtos;

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
        /// 新增病历
        /// </summary>
        Task<bool> AddAsync(RecordCreateDto recordCreateDto, Guid operatorId, string operatorName);

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
    }
}
