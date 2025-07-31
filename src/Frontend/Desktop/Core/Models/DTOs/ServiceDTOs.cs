using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.DTOs
{
    /// <summary>
    /// 药材分页查询DTO（对应后端HerbPagedQueryDto）
    /// </summary>
    public class HerbPagedQueryDto
    {
        public string? Keyword { get; set; }
        public int? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 药材创建DTO（对应后端HerbCreateDto）
    /// </summary>
    public class HerbCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Pinyin { get; set; }
        public string? WuBi { get; set; }
        public string? Origin { get; set; }
        public string? Spec { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? Effect { get; set; }
    }

    /// <summary>
    /// 药材编辑DTO（对应后端HerbEditDto）
    /// </summary>
    public class HerbEditDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Pinyin { get; set; }
        public string? WuBi { get; set; }
        public string? Origin { get; set; }
        public string? Spec { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? Effect { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 药材详情DTO（对应后端HerbDetailDto）
    /// </summary>
    public class HerbDetailDto : Herbs.HerbInfo
    {
        // 继承HerbInfo，可以添加额外的详情字段
    }

    /// <summary>
    /// 药材导入DTO（对应后端HerbImportDto）
    /// </summary>
    public class HerbImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Pinyin { get; set; }
        public string? Origin { get; set; }
        public string? Spec { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? BatchNo { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? Effect { get; set; }
    }

    /// <summary>
    /// 药材状态更新DTO（对应后端HerbStatusUpdateDto）
    /// </summary>
    public class HerbStatusUpdateDto
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }

    /// <summary>
    /// 分页结果DTO（对应后端PagedResultDto）
    /// </summary>
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