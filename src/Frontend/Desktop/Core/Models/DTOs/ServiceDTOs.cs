using System;
using System.Collections.Generic;

// 重要说明：前端DTO重复定义已被共享契约取代
// 请使用以下命名空间中的共享契约模型：
// - LYBT.Shared.Models.Contracts.Herbs.HerbPagedQueryDto
// - LYBT.Shared.Models.Contracts.Herbs.HerbCreateDto  
// - LYBT.Shared.Models.Contracts.Herbs.HerbUpdateDto (替代HerbEditDto)
// - LYBT.Shared.Models.Contracts.Herbs.HerbDetailDto
// - LYBT.Shared.Models.Common.PaginatedResult<T> (替代PagedResultDto)

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    // 注意：本文件中的重复DTO定义已迁移到共享契约
    // 新的开发应使用 LYBT.Shared.Models.Contracts.* 命名空间中的共享模型

    /// <summary>
    /// 分页结果DTO（对应后端PagedResultDto）
    /// 注意：建议使用 LYBT.Shared.Models.Common.PaginatedResult&lt;T&gt;
    /// </summary>
    [Obsolete("请使用 LYBT.Shared.Models.Common.PaginatedResult<T> 替代")]
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// 病历DTO（对应后端RecordDto）
    /// </summary>
    public class RecordDto
    {
        public Guid Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public Guid RegistrationId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public string? PresentIllness { get; set; }
        public string? TreatmentAdvice { get; set; }
        public Guid? PrescriptionId { get; set; }
        public List<string> DiagnosisResults { get; set; } = new();
        public bool IsShared { get; set; }
        public List<string> SharedToDoctorIds { get; set; } = new();
        public string? CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime RecordTime { get; set; }
    }

    /// <summary>
    /// 病历详情DTO（对应后端RecordDetailDto）
    /// </summary>
    public class RecordDetailDto : RecordDto
    {
        public List<HerbItemModel>? HerbalFormula { get; set; }
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }
    }

    /// <summary>
    /// 病历创建DTO（对应后端RecordCreateDto）
    /// </summary>
    public class RecordCreateDto
    {
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public Guid RegistrationId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? ChiefComplaint { get; set; }
        public string? PresentIllness { get; set; }
        public string? TreatmentAdvice { get; set; }
        public Guid? PrescriptionId { get; set; }
        public List<string> DiagnosisResults { get; set; } = new();
        public List<HerbItemModel>? HerbalFormula { get; set; }
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }
    }

    /// <summary>
    /// 病历编辑DTO（对应后端RecordEditDto）
    /// </summary>
    public class RecordEditDto : RecordCreateDto
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 药材条目模型（对应后端HerbItemModel）
    /// </summary>
    public class HerbItemModel
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public decimal Amount => Dosage * UnitPrice;
        public string? Usage { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 治疗项目模型（对应后端TreatmentItemModel）
    /// </summary>
    public class TreatmentItemModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Fee { get; set; }
        public DateTime? ExecuteTime { get; set; }
    }

    /// <summary>
    /// 患者详情DTO（对应后端PatientDetailDto）
    /// </summary>
    public class PatientDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Gender { get; set; } = 0; // 0=未知, 1=男, 2=女
        public int Age { get; set; }
        public string AllergyHistory { get; set; } = string.Empty;
        public string Ethnicity { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string IDType { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string PinyinCode { get; set; } = string.Empty;
        public string WuBiCode { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 患者分页查询DTO（对应后端PatientPagedQueryDto）
    /// </summary>
    public class PatientPagedQueryDto
    {
        public string? Keyword { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IDNumber { get; set; }
        public int? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 患者批量ID操作DTO（对应后端PatientBatchIdsDto）
    /// </summary>
    public class PatientBatchIdsDto
    {
        public List<Guid> Ids { get; set; } = new();
    }
}