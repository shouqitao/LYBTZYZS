using System;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Registration
{
    /// <summary>
    /// 挂号分页查询DTO
    /// </summary>
    public class RegistrationPagedQueryDto : PaginationRequest
    {
        /// <summary>
        /// 通用搜索关键词
        /// </summary>
        public new string? SearchKeyword { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string? DoctorName { get; set; }

        /// <summary>
        /// 挂号编号
        /// </summary>
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// 挂号类型
        /// </summary>
        public RegistrationType? RegistrationType { get; set; }

        /// <summary>
        /// 挂号状态
        /// </summary>
        public RegistrationStatus? Status { get; set; }

        /// <summary>
        /// 就诊日期
        /// </summary>
        public DateTime? VisitDate { get; set; }

        /// <summary>
        /// 预约日期起始
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 预约日期结束
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}