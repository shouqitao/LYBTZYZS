using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医案审计日志分页结果DTO
    /// </summary>
    public class MedicalCaseAuditLogPagedResultDto
    {
        /// <summary>审计日志列表</summary>
        [DisplayName("日志列表")]
        public List<MedicalCaseAuditLogDto> Logs { get; set; } = new();

        /// <summary>总记录数</summary>
        [DisplayName("总记录数")]
        public int TotalCount { get; set; }

        /// <summary>当前页码</summary>
        [DisplayName("当前页")]
        public int CurrentPage { get; set; }

        /// <summary>每页大小</summary>
        [DisplayName("每页大小")]
        public int PageSize { get; set; }

        /// <summary>总页数</summary>
        [DisplayName("总页数")]
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
