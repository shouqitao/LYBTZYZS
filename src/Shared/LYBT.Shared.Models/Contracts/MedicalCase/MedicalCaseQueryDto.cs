using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 医疗案例统一查询参数DTO
    /// OpenSpec: optimize-medicalcase-api - 统一查询端点
    /// </summary>
    public class MedicalCaseQueryDto
    {
        /// <summary>
        /// 查询类型（默认：All）
        /// </summary>
        public MedicalCaseQueryType QueryType { get; set; } = MedicalCaseQueryType.All;

        /// <summary>
        /// 患者ID（用于ByPatient/Unfinished/Recent查询）
        /// </summary>
        public Guid? PatientId { get; set; }

        /// <summary>
        /// 医生ID（用于Pending查询过滤）
        /// </summary>
        public Guid? DoctorId { get; set; }

        /// <summary>
        /// 关键字搜索
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 是否包含所有医生的案例（用于Unfinished查询）
        /// </summary>
        public bool IncludeAllDoctors { get; set; } = false;

        /// <summary>
        /// 限制返回数量（用于Recent查询）
        /// </summary>
        public int? Limit { get; set; }
    }
}
