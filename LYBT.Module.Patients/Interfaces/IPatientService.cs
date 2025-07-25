using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using LYBT.Common.Enums.Users;

namespace LYBT.Module.Patients.Interfaces {

    /// <summary>
    /// 病人服务接口，负责业务逻辑处理
    /// 实现软删除策略：患者只能禁用/启用，不能物理删除
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
        /// 根据Id获取病人信息
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole);

        /// <summary>
        /// 获取全部病人信息
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<List<PatientDetailDto>> GetAllAsync(UserRole currentUserRole);

        /// <summary>
        /// 分页条件查询
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole);

        /// <summary>
        /// 启用患者
        /// </summary>
        Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 禁用患者（软删除）
        /// </summary>
        Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量启用患者
        /// </summary>
        Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据关键词搜索患者
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 智能搜索患者（支持精确匹配和模糊搜索）
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<List<PatientDetailDto>> SmartSearchAsync(string keyword, UserRole currentUserRole);

        /// <summary>
        /// 获取指定医生可访问患者
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId, UserRole currentUserRole);

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
        /// 根据当前操作者角色决定是否包含禁用患者
        /// </summary>
        Task<List<PatientDetailDto>> ExportAsync(UserRole currentUserRole);

        /// <summary>
        /// 获取患者历史病历
        /// </summary>
        Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId);

        /// <summary>
        /// 快速创建患者（用于快速看诊场景）
        /// </summary>
        Task<PatientDetailDto> QuickCreateAsync(QuickPatientCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 验证患者数据
        /// </summary>
        Task<ValidationResult> ValidatePatientAsync(PatientDetailDto dto, bool isUpdate = false);

        /// <summary>
        /// 获取启用的患者列表
        /// </summary>
        Task<List<PatientDetailDto>> GetActivePatientsAsync();
    }

    /// <summary>
    /// 快速患者创建 DTO
    /// </summary>
    public class QuickPatientCreateDto {
        /// <summary>姓名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public Common.Enums.Gender Gender { get; set; }

        /// <summary>手机号（可选）</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>身份证号（可选）</summary>
        public string? IDNumber { get; set; }

        /// <summary>地址（可选）</summary>
        public string? Address { get; set; }

        /// <summary>年龄（如果没有身份证号）</summary>
        public int? Age { get; set; }
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