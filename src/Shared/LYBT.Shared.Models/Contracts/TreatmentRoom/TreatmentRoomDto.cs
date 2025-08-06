using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.TreatmentRoom
{
    /// <summary>
    /// 治疗室DTO
    /// </summary>
    public class TreatmentRoomDto
    {
        /// <summary>治疗记录ID</summary>
        [DisplayName("治疗记录ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>理疗项目ID列表</summary>
        [DisplayName("理疗项目ID列表")]
        public List<Guid> PhysiotherapyItemIds { get; set; } = [];

        /// <summary>理疗项目名称列表</summary>
        [DisplayName("理疗项目名称列表")]
        public List<string> PhysiotherapyItemNames { get; set; } = [];

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public TreatmentRoomStatus Status { get; set; }

        /// <summary>治疗师ID</summary>
        [DisplayName("治疗师ID")]
        public Guid? TherapistId { get; set; }

        /// <summary>治疗师姓名</summary>
        [DisplayName("治疗师姓名")]
        public string? TherapistName { get; set; }

        /// <summary>治疗室号</summary>
        [DisplayName("治疗室号")]
        public string? RoomNumber { get; set; }

        /// <summary>开始时间</summary>
        [DisplayName("开始时间")]
        public DateTime? StartTime { get; set; }

        /// <summary>结束时间</summary>
        [DisplayName("结束时间")]
        public DateTime? EndTime { get; set; }

        /// <summary>治疗记录</summary>
        [DisplayName("治疗记录")]
        public string? TreatmentNotes { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 治疗室状态枚举
    /// </summary>
    public enum TreatmentRoomStatus
    {
        /// <summary>待治疗</summary>
        Pending = 0,

        /// <summary>排队中</summary>
        Queuing = 1,

        /// <summary>治疗中</summary>
        InProgress = 2,

        /// <summary>已完成</summary>
        Completed = 3,

        /// <summary>已取消</summary>
        Cancelled = 4
    }

    /// <summary>
    /// 治疗室详情DTO
    /// </summary>
    public class TreatmentRoomDetailDto : TreatmentRoomDto
    {
        /// <summary>理疗项目详情列表</summary>
        [DisplayName("理疗项目详情列表")]
        public List<PhysiotherapyItemDetailDto> PhysiotherapyItems { get; set; } = [];

        /// <summary>治疗进度记录</summary>
        [DisplayName("治疗进度记录")]
        public List<TreatmentProgressDto> ProgressRecords { get; set; } = [];
    }

    /// <summary>
    /// 理疗项目详情DTO
    /// </summary>
    public class PhysiotherapyItemDetailDto
    {
        /// <summary>项目ID</summary>
        public Guid Id { get; set; }

        /// <summary>项目名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>项目类型</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>部位</summary>
        public string BodyPart { get; set; } = string.Empty;

        /// <summary>时长（分钟）</summary>
        public int Duration { get; set; }

        /// <summary>次数</summary>
        public int Quantity { get; set; }

        /// <summary>已完成次数</summary>
        public int CompletedSessions { get; set; }

        /// <summary>单价</summary>
        public decimal Price { get; set; }
    }

    /// <summary>
    /// 治疗进度DTO
    /// </summary>
    public class TreatmentProgressDto
    {
        /// <summary>记录时间</summary>
        public DateTime RecordTime { get; set; }

        /// <summary>治疗师</summary>
        public string TherapistName { get; set; } = string.Empty;

        /// <summary>进度说明</summary>
        public string ProgressNote { get; set; } = string.Empty;

        /// <summary>完成的项目</summary>
        public string CompletedItems { get; set; } = string.Empty;
    }

    /// <summary>
    /// 治疗室创建DTO
    /// </summary>
    public class TreatmentRoomCreateDto
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>理疗项目ID列表</summary>
        [DisplayName("理疗项目ID列表")]
        public List<Guid> PhysiotherapyItemIds { get; set; } = [];

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 治疗室编辑DTO
    /// </summary>
    public class TreatmentRoomEditDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public TreatmentRoomStatus Status { get; set; }

        /// <summary>治疗师ID</summary>
        [DisplayName("治疗师ID")]
        public Guid? TherapistId { get; set; }

        /// <summary>治疗室号</summary>
        [DisplayName("治疗室号")]
        public string? RoomNumber { get; set; }

        /// <summary>治疗记录</summary>
        [DisplayName("治疗记录")]
        public string? TreatmentNotes { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 治疗室排队DTO
    /// </summary>
    public class TreatmentRoomQueueDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>排队号</summary>
        public int QueueNumber { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>理疗项目</summary>
        public string TreatmentItems { get; set; } = string.Empty;

        /// <summary>预计时长</summary>
        public int EstimatedDuration { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>等待时间</summary>
        public string WaitingTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 治疗室使用情况DTO
    /// </summary>
    public class TreatmentRoomUsageDto
    {
        /// <summary>治疗室ID</summary>
        public Guid RoomId { get; set; }

        /// <summary>治疗室号</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>当前患者</summary>
        public string? CurrentPatient { get; set; }

        /// <summary>当前治疗师</summary>
        public string? CurrentTherapist { get; set; }

        /// <summary>开始时间</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>预计结束时间</summary>
        public DateTime? EstimatedEndTime { get; set; }
    }

    /// <summary>
    /// 可用治疗室DTO
    /// </summary>
    public class AvailableRoomDto
    {
        /// <summary>治疗室ID</summary>
        public Guid RoomId { get; set; }

        /// <summary>治疗室号</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>可用时间</summary>
        public DateTime AvailableFrom { get; set; }
    }

    /// <summary>
    /// 治疗室统计DTO
    /// </summary>
    public class TreatmentRoomStatisticsDto
    {
        /// <summary>开始日期</summary>
        public DateTime StartDate { get; set; }

        /// <summary>结束日期</summary>
        public DateTime EndDate { get; set; }

        /// <summary>总治疗人数</summary>
        public int TotalPatients { get; set; }

        /// <summary>总治疗次数</summary>
        public int TotalSessions { get; set; }

        /// <summary>已完成次数</summary>
        public int CompletedSessions { get; set; }

        /// <summary>取消次数</summary>
        public int CancelledSessions { get; set; }

        /// <summary>平均治疗时长（分钟）</summary>
        public double AverageDuration { get; set; }

        /// <summary>治疗师工作量统计</summary>
        public List<TherapistWorkloadDto> TherapistWorkloads { get; set; } = [];

        /// <summary>项目使用统计</summary>
        public List<TreatmentItemUsageDto> ItemUsageStats { get; set; } = [];
    }

    /// <summary>
    /// 治疗师工作量DTO
    /// </summary>
    public class TherapistWorkloadDto
    {
        /// <summary>治疗师ID</summary>
        public Guid TherapistId { get; set; }

        /// <summary>治疗师姓名</summary>
        public string TherapistName { get; set; } = string.Empty;

        /// <summary>治疗人数</summary>
        public int PatientCount { get; set; }

        /// <summary>治疗次数</summary>
        public int SessionCount { get; set; }

        /// <summary>总时长（分钟）</summary>
        public int TotalDuration { get; set; }
    }

    /// <summary>
    /// 治疗项目使用统计DTO
    /// </summary>
    public class TreatmentItemUsageDto
    {
        /// <summary>项目名称</summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>项目类型</summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>使用次数</summary>
        public int UsageCount { get; set; }

        /// <summary>总时长（分钟）</summary>
        public int TotalDuration { get; set; }

        /// <summary>总收入</summary>
        public decimal TotalRevenue { get; set; }
    }
}