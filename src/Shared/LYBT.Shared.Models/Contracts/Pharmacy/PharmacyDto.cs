using System;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Pharmacy
{
    /// <summary>
    /// 药房DTO
    /// </summary>
    public class PharmacyDto
    {
        /// <summary>药房单ID</summary>
        [DisplayName("药房单ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>配药状态</summary>
        [DisplayName("配药状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>配药时间</summary>
        [DisplayName("配药时间")]
        public DateTime? DispensingTime { get; set; }

        /// <summary>配药师姓名</summary>
        [DisplayName("配药师姓名")]
        public string PharmacistName { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string Remark { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 药房详情DTO
    /// </summary>
    public class PharmacyDetailDto : PharmacyDto
    {
        /// <summary>处方项目列表</summary>
        [DisplayName("处方项目列表")]
        public List<PharmacyHerbDto> Herbs { get; set; } = [];
    }

    /// <summary>
    /// 药房创建DTO
    /// </summary>
    public class PharmacyCreateDto
    {
        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>处方ID</summary>
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药房编辑DTO
    /// </summary>
    public class PharmacyEditDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>配药状态</summary>
        [DisplayName("配药状态")]
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 药房药材DTO
    /// </summary>
    public class PharmacyHerbDto
    {
        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>数量</summary>
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>小计</summary>
        [DisplayName("小计")]
        public decimal Subtotal => Quantity * Price;
    }

    /// <summary>
    /// 药房排队DTO
    /// </summary>
    public class PharmacyQueueDto
    {
        /// <summary>ID</summary>
        public Guid Id { get; set; }

        /// <summary>排队号</summary>
        public int QueueNumber { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>处方类型</summary>
        public string PrescriptionType { get; set; } = string.Empty;

        /// <summary>剂数</summary>
        public int DosageCount { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>等待时间</summary>
        public string WaitingTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 库存检查结果DTO
    /// </summary>
    public class StockCheckResultDto
    {
        /// <summary>是否有足够库存</summary>
        public bool HasSufficientStock { get; set; }

        /// <summary>缺货项目列表</summary>
        public List<StockShortageItemDto> ShortageItems { get; set; } = [];
    }

    /// <summary>
    /// 缺货项目DTO
    /// </summary>
    public class StockShortageItemDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>需求数量</summary>
        public decimal RequiredQuantity { get; set; }

        /// <summary>库存数量</summary>
        public decimal StockQuantity { get; set; }

        /// <summary>缺口数量</summary>
        public decimal ShortageQuantity => RequiredQuantity - StockQuantity;
    }

    /// <summary>
    /// 药房统计DTO
    /// </summary>
    public class PharmacyStatisticsDto
    {
        /// <summary>开始日期</summary>
        public DateTime StartDate { get; set; }

        /// <summary>结束日期</summary>
        public DateTime EndDate { get; set; }

        /// <summary>总处方数</summary>
        public int TotalPrescriptions { get; set; }

        /// <summary>已配药数</summary>
        public int DispensedCount { get; set; }

        /// <summary>待配药数</summary>
        public int PendingCount { get; set; }

        /// <summary>取消数</summary>
        public int CancelledCount { get; set; }

        /// <summary>药材使用统计</summary>
        public List<HerbUsageStatDto> HerbUsageStats { get; set; } = [];
    }

    /// <summary>
    /// 药材使用统计DTO
    /// </summary>
    public class HerbUsageStatDto
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>使用数量</summary>
        public decimal UsageQuantity { get; set; }

        /// <summary>使用次数</summary>
        public int UsageCount { get; set; }

        /// <summary>金额</summary>
        public decimal Amount { get; set; }
    }
}