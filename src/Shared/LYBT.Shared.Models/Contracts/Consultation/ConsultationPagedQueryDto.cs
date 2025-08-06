using LYBT.Shared.Models.Contracts.Common;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 看诊分页查询DTO
    /// </summary>
    public class ConsultationPagedQueryDto : PagedQueryBaseDto
    {
        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        /// <summary>开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        /// <summary>诊断关键词</summary>
        [DisplayName("诊断关键词")]
        public string? DiagnosisKeyword { get; set; }

        /// <summary>搜索关键词（重新映射）</summary>
        public string? SearchKeyword => Keyword;

        /// <summary>当前页码（重新映射）</summary>
        public int CurrentPage => PageIndex;
    }
}