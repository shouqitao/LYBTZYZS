using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Compatibility
{
    /// <summary>
    /// 配伍记录响应DTO
    /// </summary>
    public class CompatibilityNoteDto : BaseDto
    {
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        [DisplayName("药材组合")]
        public string HerbCombination { get; set; } = string.Empty;

        [DisplayName("配伍类型")]
        public CompatibilityType CompatibilityType { get; set; }

        [DisplayName("严重程度")]
        public CompatibilitySeverity SeverityLevel { get; set; }

        [DisplayName("配伍说明")]
        public string? CompatibilityNote { get; set; }

        [DisplayName("参考来源")]
        public string? ReferenceSource { get; set; }

        [DisplayName("医生建议")]
        public string? DoctorRecommendation { get; set; }
    }

    /// <summary>
    /// 创建配伍记录DTO
    /// </summary>
    public class CompatibilityNoteCreateDto
    {
        [Required(ErrorMessage = "药材组合不能为空")]
        [StringLength(200, ErrorMessage = "药材组合长度不能超过200字符")]
        [DisplayName("药材组合")]
        public string HerbCombination { get; set; } = string.Empty;

        [DisplayName("配伍类型")]
        public CompatibilityType CompatibilityType { get; set; } = CompatibilityType.Unknown;

        [DisplayName("严重程度")]
        public CompatibilitySeverity SeverityLevel { get; set; } = CompatibilitySeverity.Low;

        [StringLength(1000, ErrorMessage = "配伍说明不能超过1000字符")]
        [DisplayName("配伍说明")]
        public string? CompatibilityNote { get; set; }

        [StringLength(200, ErrorMessage = "参考来源不能超过200字符")]
        [DisplayName("参考来源")]
        public string? ReferenceSource { get; set; }

        [StringLength(500, ErrorMessage = "医生建议不能超过500字符")]
        [DisplayName("医生建议")]
        public string? DoctorRecommendation { get; set; }
    }

    /// <summary>
    /// 更新配伍记录DTO
    /// </summary>
    public class CompatibilityNoteUpdateDto
    {
        [StringLength(1000, ErrorMessage = "配伍说明不能超过1000字符")]
        [DisplayName("配伍说明")]
        public string? CompatibilityNote { get; set; }

        [StringLength(500, ErrorMessage = "医生建议不能超过500字符")]
        [DisplayName("医生建议")]
        public string? DoctorRecommendation { get; set; }
    }
}
