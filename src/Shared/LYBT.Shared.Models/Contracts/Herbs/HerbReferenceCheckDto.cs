namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材引用检查结果DTO
    /// Epic #1962 Task 4.2: 删除前引用检查
    /// </summary>
    public class HerbReferenceCheckDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>是否被引用</summary>
        public bool HasReferences { get; set; }

        /// <summary>引用次数（处方中的引用总数）</summary>
        public int ReferenceCount { get; set; }

        /// <summary>是否可删除（BR-007: 始终为true，支持软删除）</summary>
        public bool CanDelete { get; set; } = true;

        /// <summary>删除提示信息</summary>
        public string? DeleteWarning { get; set; }

        /// <summary>最近引用的处方列表（最多显示5条）</summary>
        public List<PrescriptionReferenceDto>? RecentReferences { get; set; }
    }

    /// <summary>
    /// 处方引用详情DTO
    /// Epic #1962 Task 4.2
    /// </summary>
    public class PrescriptionReferenceDto
    {
        /// <summary>处方ID</summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>处方编号</summary>
        public string PrescriptionNumber { get; set; } = string.Empty;

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>处方状态</summary>
        public string Status { get; set; } = string.Empty;
    }
}
