using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;

namespace LYBT.Module.Patients.Interfaces {

    /// <summary>
    /// 病人服务接口，负责业务逻辑处理
    /// </summary>
    public interface IPatientService {

        /// <summary>
        /// 新增病人
        /// </summary>
        Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 编辑病人
        /// </summary>
        Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除单个病人
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据Id获取病人信息
        /// </summary>
        Task<PatientDetailDto> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取全部病人信息
        /// </summary>
        Task<List<PatientDetailDto>> GetAllAsync();

        /// <summary>
        /// 分页条件查询
        /// </summary>
        Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query);

        /// <summary>
        /// 批量删除病人
        /// </summary>
        Task<int> BatchDeleteAsync(List<string> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 启用患者
        /// </summary>
        Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据关键词搜索患者
        /// </summary>
        Task<List<PatientDetailDto>> SearchAsync(string keyword);

        /// <summary>
        /// 获取指定医生可访问患者
        /// </summary>
        Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId);

        /// <summary>
        /// 将患者授权给指定医生
        /// </summary>
        Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName);

        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<List<PatientDetailDto>> ExportAsync();

        /// <summary>
        /// 获取患者历史病历
        /// </summary>
        Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId);
    }
}