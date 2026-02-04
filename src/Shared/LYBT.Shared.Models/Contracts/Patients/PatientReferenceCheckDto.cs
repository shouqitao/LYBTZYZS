namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者引用检查结果DTO
    /// OpenSpec: implement-data-sync - 删除前引用检查
    /// </summary>
    public class PatientReferenceCheckDto
    {
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>是否被引用</summary>
        public bool HasReferences { get; set; }

        /// <summary>引用次数（医案总数）</summary>
        public int ReferenceCount { get; set; }

        /// <summary>是否可删除（支持软删除，始终为true）</summary>
        public bool CanDelete { get; set; } = true;

        /// <summary>删除提示信息</summary>
        public string? DeleteWarning { get; set; }

        /// <summary>最近引用的医案列表（最多显示5条）</summary>
        public List<MedicalCaseReferenceDto>? RecentMedicalCases { get; set; }
    }

    /// <summary>
    /// 医案引用详情DTO
    /// OpenSpec: implement-data-sync
    /// </summary>
    public class MedicalCaseReferenceDto
    {
        /// <summary>医案ID</summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>医案编号</summary>
        public string CaseNumber { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>医案状态</summary>
        public string Status { get; set; } = string.Empty;
    }
}
