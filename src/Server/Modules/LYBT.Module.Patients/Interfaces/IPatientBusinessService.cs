using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{

    /// <summary>
    /// 患者业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// 职责：患者业务逻辑、CRUD操作、状态管理
    /// </summary>
    public interface IPatientBusinessService
    {

        /// <summary>
        /// 创建患者
        /// </summary>
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid patientId, PatientUpdateDto updateDto);

        /// <summary>
        /// 删除患者
        /// </summary>
        Task<ServiceResult<PatientDto>> DeleteAsync(Guid patientId);

        /// <summary>
        /// 批量删除患者
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(List<Guid> patientIds);

        /// <summary>
        /// 设置患者状态
        /// </summary>
        Task<ServiceResult<bool>> SetStatusAsync(List<Guid> patientIds, string status);

        /// <summary>
        /// 启用患者
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(List<Guid> patientIds);

        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(List<Guid> patientIds);

        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(List<PatientImportDto> importDtos);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync(PatientExportDto exportDto);

        /// <summary>
        /// 验证患者数据
        /// </summary>
        Task<ServiceResult<List<string>>> ValidatePatientAsync(PatientCreateDto createDto);

        /// <summary>
        /// 获取导入模板
        /// </summary>
        Task<ServiceResult<object>> GetImportTemplate();
    }
}
