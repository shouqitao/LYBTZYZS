using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、CRUD操作、状态转换、事件处理
/// </summary>
public interface IPatientBusinessService
{
    #region CRUD业务操作
    
    /// <summary>
    /// 创建患者（完整业务流程）
    /// </summary>
    Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto createDto);
    
    /// <summary>
    /// 更新患者信息（完整业务流程）
    /// </summary>
    Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto updateDto);
    
    /// <summary>
    /// 删除患者（软删除业务流程）
    /// </summary>
    Task<ServiceResult<bool>> DeletePatientAsync(Guid id);
    
    /// <summary>
    /// 批量删除患者
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchDeletePatientsAsync(List<Guid> patientIds);
    
    /// <summary>
    /// 恢复已删除患者
    /// </summary>
    Task<ServiceResult<PatientDto>> RestorePatientAsync(Guid id);
    
    #endregion
    
    #region 患者状态管理业务
    
    /// <summary>
    /// 启用患者
    /// </summary>
    Task<ServiceResult<bool>> EnablePatientAsync(Guid patientId);
    
    /// <summary>
    /// 禁用患者
    /// </summary>
    Task<ServiceResult<bool>> DisablePatientAsync(Guid patientId);
    
    /// <summary>
    /// 切换患者状态
    /// </summary>
    Task<ServiceResult<bool>> TogglePatientStatusAsync(Guid patientId);
    
    /// <summary>
    /// 批量更新患者状态
    /// </summary>
    Task<ServiceResult<BatchOperationResult>> BatchUpdatePatientStatusAsync(List<Guid> patientIds, bool isEnabled);
    
    #endregion
    
    #region 患者档案管理
    
    /// <summary>
    /// 完善患者档案
    /// </summary>
    Task<ServiceResult<PatientDto>> CompletePatientProfileAsync(Guid patientId, PatientProfileDto profileDto);
    
    /// <summary>
    /// 更新患者医疗信息
    /// </summary>
    Task<ServiceResult<bool>> UpdateMedicalInfoAsync(Guid patientId, PatientMedicalInfoDto medicalInfo);
    
    /// <summary>
    /// 更新患者联系信息
    /// </summary>
    Task<ServiceResult<bool>> UpdateContactInfoAsync(Guid patientId, PatientContactInfoDto contactInfo);
    
    /// <summary>
    /// 添加患者备注
    /// </summary>
    Task<ServiceResult<bool>> AddPatientRemarksAsync(Guid patientId, string remarks);
    
    #endregion
    
    #region 就诊记录管理
    
    /// <summary>
    /// 记录患者就诊
    /// </summary>
    Task<ServiceResult> RecordPatientVisitAsync(Guid patientId, PatientVisitDto visitInfo);
    
    /// <summary>
    /// 更新最后就诊时间
    /// </summary>
    Task<ServiceResult> UpdateLastVisitTimeAsync(Guid patientId, DateTime visitTime);
    
    /// <summary>
    /// 获取患者就诊历史
    /// </summary>
    Task<ServiceResult<List<PatientVisitHistoryDto>>> GetPatientVisitHistoryAsync(Guid patientId);
    
    #endregion
    
    #region 数据导入导出
    
    /// <summary>
    /// 导入患者数据
    /// </summary>
    Task<ServiceResult<PatientImportResultDto>> ImportPatientsAsync(PatientImportDto importDto);
    
    /// <summary>
    /// 导出患者数据
    /// </summary>
    Task<ServiceResult<PatientExportResultDto>> ExportPatientsAsync(PatientExportQueryDto exportQuery);
    
    /// <summary>
    /// 验证导入数据
    /// </summary>
    ServiceResult<PatientImportValidationDto> ValidateImportData(List<PatientImportRecordDto> records);
    
    #endregion
    
    #region 业务规则和验证
    
    /// <summary>
    /// 应用业务规则验证
    /// </summary>
    ServiceResult ApplyBusinessRules(PatientBusinessRuleDto rules);
    
    /// <summary>
    /// 验证患者业务约束
    /// </summary>
    Task<ServiceResult<bool>> ValidatePatientConstraintsAsync(Guid patientId);
    
    /// <summary>
    /// 检查手机号重复性
    /// </summary>
    Task<ServiceResult<bool>> CheckPhoneAvailabilityAsync(string phone, Guid? excludePatientId = null);
    
    /// <summary>
    /// 检查身份证号重复性
    /// </summary>
    Task<ServiceResult<bool>> CheckIdCardAvailabilityAsync(string idCard, Guid? excludePatientId = null);
    
    /// <summary>
    /// 验证患者年龄合理性
    /// </summary>
    ServiceResult ValidatePatientAge(DateTime birthDate);
    
    #endregion
    
    #region 患者关系管理
    
    /// <summary>
    /// 添加患者关系（家庭成员）
    /// </summary>
    Task<ServiceResult<bool>> AddPatientRelationshipAsync(Guid patientId, PatientRelationshipDto relationship);
    
    /// <summary>
    /// 获取患者家庭成员
    /// </summary>
    Task<ServiceResult<List<PatientRelationshipDto>>> GetPatientFamilyMembersAsync(Guid patientId);
    
    /// <summary>
    /// 移除患者关系
    /// </summary>
    Task<ServiceResult<bool>> RemovePatientRelationshipAsync(Guid relationshipId);
    
    #endregion
    
    #region 患者标签管理
    
    /// <summary>
    /// 添加患者标签
    /// </summary>
    Task<ServiceResult<bool>> AddPatientTagAsync(Guid patientId, string tag);
    
    /// <summary>
    /// 移除患者标签
    /// </summary>
    Task<ServiceResult<bool>> RemovePatientTagAsync(Guid patientId, string tag);
    
    /// <summary>
    /// 获取患者所有标签
    /// </summary>
    Task<ServiceResult<List<string>>> GetPatientTagsAsync(Guid patientId);
    
    #endregion
    
    #region 审计和监控
    
    /// <summary>
    /// 记录患者操作审计
    /// </summary>
    Task<ServiceResult> RecordPatientAuditAsync(PatientAuditDto auditInfo);
    
    /// <summary>
    /// 生成患者档案报告
    /// </summary>
    Task<ServiceResult<PatientProfileReportDto>> GeneratePatientProfileReportAsync(Guid patientId);
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 患者状态变更事件
    /// </summary>
    event EventHandler<PatientStatusChangedEventArgs>? PatientStatusChanged;
    
    /// <summary>
    /// 患者操作事件
    /// </summary>
    event EventHandler<PatientOperationEventArgs>? PatientOperation;
    
    /// <summary>
    /// 患者就诊事件
    /// </summary>
    event EventHandler<PatientVisitEventArgs>? PatientVisit;
    
    #endregion
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BatchOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationError> Errors { get; set; } = new();
}

/// <summary>
/// 批量操作错误
/// </summary>
public class BatchOperationError
{
    public Guid PatientId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// 患者档案DTO
/// </summary>
public class PatientProfileDto
{
    public string? Profession { get; set; }
    public string? MaritalStatus { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? FamilyHistory { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 患者医疗信息DTO
/// </summary>
public class PatientMedicalInfoDto
{
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? FamilyHistory { get; set; }
    public string? CurrentMedications { get; set; }
    public string? MedicalRemarks { get; set; }
}

/// <summary>
/// 患者联系信息DTO
/// </summary>
public class PatientContactInfoDto
{
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
}

/// <summary>
/// 患者就诊DTO
/// </summary>
public class PatientVisitDto
{
    public DateTime VisitTime { get; set; } = DateTime.Now;
    public string? VisitType { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 患者就诊历史DTO
/// </summary>
public class PatientVisitHistoryDto
{
    public Guid Id { get; set; }
    public DateTime VisitTime { get; set; }
    public string? VisitType { get; set; }
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Doctor { get; set; }
}

/// <summary>
/// 患者导入结果DTO
/// </summary>
public class PatientImportResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public List<PatientDto> ImportedPatients { get; set; } = new();
}

/// <summary>
/// 患者导出结果DTO
/// </summary>
public class PatientExportResultDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

/// <summary>
/// 患者导入DTO
/// </summary>
public class PatientImportDto
{
    public List<PatientImportRecordDto> Records { get; set; } = new();
    public bool SkipDuplicates { get; set; } = true;
    public bool ValidateData { get; set; } = true;
}

/// <summary>
/// 患者导入记录DTO
/// </summary>
public class PatientImportRecordDto
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? IdCard { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
}

/// <summary>
/// 患者导入验证DTO
/// </summary>
public class PatientImportValidationDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<PatientImportRecordDto> ValidRecords { get; set; } = new();
    public List<PatientImportRecordDto> InvalidRecords { get; set; } = new();
}

/// <summary>
/// 患者业务规则DTO
/// </summary>
public class PatientBusinessRuleDto
{
    public bool RequirePhoneVerification { get; set; }
    public bool RequireIdCardVerification { get; set; }
    public int MinAge { get; set; } = 0;
    public int MaxAge { get; set; } = 150;
    public bool AllowDuplicatePhone { get; set; } = false;
    public bool AllowDuplicateIdCard { get; set; } = false;
}

/// <summary>
/// 患者关系DTO
/// </summary>
public class PatientRelationshipDto
{
    public Guid Id { get; set; }
    public Guid RelatedPatientId { get; set; }
    public string RelationshipType { get; set; } = string.Empty; // 父子、夫妻、兄弟姐妹等
    public string RelatedPatientName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>
/// 患者审计DTO
/// </summary>
public class PatientAuditDto
{
    public Guid PatientId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// 患者档案报告DTO
/// </summary>
public class PatientProfileReportDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime GeneratedTime { get; set; }
    public PatientDto BasicInfo { get; set; } = new();
    public List<PatientVisitHistoryDto> VisitHistory { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<PatientRelationshipDto> FamilyMembers { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// 患者状态变更事件参数
/// </summary>
public class PatientStatusChangedEventArgs : EventArgs
{
    public Guid PatientId { get; set; }
    public bool IsEnabled { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 患者操作事件参数
/// </summary>
public class PatientOperationEventArgs : EventArgs
{
    public Guid PatientId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 患者就诊事件参数
/// </summary>
public class PatientVisitEventArgs : EventArgs
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime VisitTime { get; set; }
    public string? VisitType { get; set; }
    public string? Doctor { get; set; }
}