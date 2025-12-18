using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 快速处方DTO（用于快速保存） - 继承处方输入基础DTO的简化版本
    /// </summary>
    public class QuickPrescriptionDto
    {

        [Required(ErrorMessage = "诊断不能为空")]
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("用药建议")]
        public string? Advice { get; set; }

        [Range(1, 30, ErrorMessage = "剂数必须在1-30之间")]
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;
    }
}
