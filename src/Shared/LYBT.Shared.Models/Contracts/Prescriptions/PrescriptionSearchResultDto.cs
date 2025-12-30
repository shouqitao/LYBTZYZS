using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{
    /// <summary>
    /// 处方搜索结果DTO (Issue #1372 ENTRY-14, Issue #1370 ENTRY-12)
    /// 用于全局处方搜索和历史查询功能，包含患者和诊断信息
    /// </summary>
    public class PrescriptionSearchResultDto
    {
        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>处方ID（别名，与Id相同）</summary>
        [DisplayName("处方ID")]
        public Guid PrescriptionId
        {
            get => Id;
            set => Id = value;
        }

        /// <summary>
        /// 处方编号（服务端自动生成，格式：RX-YYYYMMDD-NNNN）
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        [DisplayName("处方编号")]
        [StringLength(20)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>处方创建日期</summary>
        [DisplayName("创建日期")]
        public DateTime CreatedAt { get; set; }

        /// <summary>处方日期（别名，与CreatedAt相同）</summary>
        [DisplayName("处方日期")]
        public DateTime PrescriptionDate
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        [StringLength(100)]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>主治（适应症）</summary>
        [DisplayName("主治")]
        [StringLength(500)]
        public string? Indication { get; set; }

        /// <summary>中医诊断（从Consultation查询）</summary>
        [DisplayName("中医诊断")]
        [StringLength(500)]
        public string? TcmDiagnosis { get; set; }

        /// <summary>剂数</summary>
        [DisplayName("剂数")]
        public int DosageCount { get; set; }

        /// <summary>医嘱</summary>
        [DisplayName("医嘱")]
        [StringLength(500)]
        public string? Advice { get; set; }

        /// <summary>验方来源</summary>
        [DisplayName("验方来源")]
        [StringLength(200)]
        public string? FormulaSource { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>药材数量 (Issue #1370 ENTRY-12)</summary>
        [DisplayName("药材数量")]
        public int HerbCount { get; set; }

        /// <summary>处方项目列表 (Issue #1370 ENTRY-12)</summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }
}
