using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Interfaces {

    /// <summary>
    /// 病人服务接口，负责业务逻辑处理
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public interface IPatientService {

        /// <summary>
        /// 新增病人
        /// </summary>
        Task<PatientDto?> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 编辑病人
        /// </summary>
        Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据Id获取病人信息
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole);

        /// <summary>
        /// 获取全部病人信息
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<List<PatientDetailDto>> GetAllAsync(UserRole currentUserRole);

        /// <summary>
        /// 分页条件查询
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<PaginatedResult<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole);

        /// <summary>
        /// 启用患者档案
        /// </summary>
        Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 禁用患者档案（软删除）
        /// </summary>
        Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量禁用患者档案
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量启用患者档案
        /// </summary>
        Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据关键词搜索患者档案
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 智能搜索患者档案（支持精确匹配和模糊搜索）
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<List<PatientDetailDto>> SmartSearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 导入患者档案数据
        /// </summary>
        Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName);

        /// <summary>
        /// 导出患者档案数据
        /// 根据当前操作者角色决定是否包含禁用患者档案
        /// </summary>
        Task<List<PatientDetailDto>> ExportAsync(UserRole currentUserRole);

        /// <summary>
        /// 获取患者档案历史病历
        /// </summary>
        Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId);

        /// <summary>
        /// 查询或创建患者档案（用于挂号/看诊场景）
        /// 根据姓名和身份证号查询患者档案，如果不存在则创建新档案
        /// </summary>
        Task<PatientDetailDto> FindOrCreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 验证患者档案数据
        /// </summary>
        Task<ValidationResult> ValidatePatientAsync(PatientDetailDto dto, bool isUpdate = false);

        /// <summary>
        /// 获取启用的患者档案列表
        /// </summary>
        Task<List<PatientDetailDto>> GetActivePatientsAsync();
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; set; } = new();

        public void AddError(string error) {
            Errors.Add(error);
        }
    }
}