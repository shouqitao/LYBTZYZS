using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Compatibility
{
    /// <summary>
    /// 配伍禁忌记录实体 - MVP版本
    /// 记录医生对处方中药材配伍的注意事项和建议
    /// </summary>
    [Table("HerbCompatibilityNotes")]
    public class HerbCompatibilityNote
    {
        [Key]
        [DisplayName("记录ID")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        [Required]
        [StringLength(200)]
        [DisplayName("药材组合")]
        public string HerbCombination { get; set; } = string.Empty;

        [DisplayName("配伍类型")]
        public CompatibilityType CompatibilityType { get; set; }

        [DisplayName("严重程度")]
        public CompatibilitySeverity SeverityLevel { get; set; }

        [StringLength(1000)]
        [DisplayName("配伍说明")]
        public string? CompatibilityNote { get; set; }

        [StringLength(200)]
        [DisplayName("参考来源")]
        public string? ReferenceSource { get; set; }

        [StringLength(500)]
        [DisplayName("医生建议")]
        public string? DoctorRecommendation { get; set; }

        // 审计字段
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        [DisplayName("创建者ID")]
        public Guid CreatedBy { get; set; }

        [DisplayName("是否删除")]
        public bool IsDeleted { get; set; } = false;
    }
}
