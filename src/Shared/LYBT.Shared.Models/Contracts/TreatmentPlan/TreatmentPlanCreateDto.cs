using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.TreatmentPlan
{
    /// <summary>
    /// 创建治疗方案DTO
    /// </summary>
    public class TreatmentPlanCreateDto
    {
        /// <summary>看诊ID</summary>
        [Required(ErrorMessage = "看诊ID不能为空")]
        public Guid ConsultationId { get; set; }

        /// <summary>处方信息</summary>
        public PrescriptionCreateDto? Prescription { get; set; }

        /// <summary>理疗项目列表</summary>
        public List<PhysiotherapyItemCreateDto> PhysiotherapyItems { get; set; } = new();

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建处方DTO
    /// </summary>
    public class PrescriptionCreateDto
    {
        /// <summary>处方药材列表</summary>
        [Required(ErrorMessage = "处方药材不能为空")]
        public List<PrescriptionHerbCreateDto> Herbs { get; set; } = new();

        /// <summary>付数</summary>
        [Range(1, 99, ErrorMessage = "付数必须在1-99之间")]
        public int DosageCount { get; set; } = 1;

        /// <summary>用法说明</summary>
        [StringLength(200, ErrorMessage = "用法说明长度不能超过200个字符")]
        public string? Instructions { get; set; }

        /// <summary>特殊煎法</summary>
        [StringLength(100, ErrorMessage = "特殊煎法长度不能超过100个字符")]
        public string? SpecialInstructions { get; set; }
    }

    /// <summary>
    /// 创建处方药材DTO
    /// </summary>
    public class PrescriptionHerbCreateDto
    {
        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        public Guid HerbId { get; set; }

        /// <summary>数量</summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(0.01, 9999.99, ErrorMessage = "数量必须在0.01-9999.99之间")]
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit { get; set; } = "g";

        /// <summary>特殊用法</summary>
        [StringLength(50, ErrorMessage = "特殊用法长度不能超过50个字符")]
        public string? SpecialUsage { get; set; }
    }

    /// <summary>
    /// 创建理疗项目DTO
    /// </summary>
    public class PhysiotherapyItemCreateDto
    {
        /// <summary>项目名称</summary>
        [Required(ErrorMessage = "项目名称不能为空")]
        [StringLength(50, ErrorMessage = "项目名称长度不能超过50个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目类型</summary>
        [Required(ErrorMessage = "项目类型不能为空")]
        [StringLength(20, ErrorMessage = "项目类型长度不能超过20个字符")]
        public string Type { get; set; } = string.Empty;

        /// <summary>治疗部位</summary>
        [StringLength(100, ErrorMessage = "治疗部位长度不能超过100个字符")]
        public string? TreatmentArea { get; set; }

        /// <summary>次数</summary>
        [Range(1, 99, ErrorMessage = "次数必须在1-99之间")]
        public int Count { get; set; } = 1;

        /// <summary>单价</summary>
        [Range(0.01, 99999.99, ErrorMessage = "单价必须在0.01-99999.99之间")]
        public decimal UnitPrice { get; set; }

        /// <summary>备注</summary>
        [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
        public string? Remark { get; set; }
    }
}