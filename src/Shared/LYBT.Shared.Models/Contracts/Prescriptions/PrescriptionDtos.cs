using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions
{

    /// <summary>
    /// 处方信息DTO - UltraThink v2.0简化版
    /// 与Prescription实体对齐，价格改为计算属性
    /// </summary>
    public class PrescriptionDto : StatusDto, IRemarkable
    {
        /// <summary>
        /// 处方编号（格式：RX-YYYYMMDD-NNNN）
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        [DisplayName("处方编号")]
        public string? PrescriptionNumber { get; set; }

        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
        // 通过MedicalCaseId关联获取患者和医生信息

        [DisplayName("主治")]
        public string? Indication { get; set; }

        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        [DisplayName("验方来源")]
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 引用的验方名称列表，逗号分隔 (Issue #1365 ENTRY-7)
        /// </summary>
        [DisplayName("引用验方")]
        [StringLength(500, ErrorMessage = "引用验方名称长度不能超过500个字符")]
        public string? ReferencedFormulas { get; set; }

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        [DisplayName("处方项目")]
        public List<PrescriptionItemDto> Items { get; set; } = new();

        /// <summary>单帖价格（由Service层计算）</summary>
        [DisplayName("单帖价格")]
        public decimal SingleDosePrice { get; set; }

        /// <summary>总价格（由Service层计算）</summary>
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 总金额（兼容性别名，映射到TotalPrice）
        /// </summary>
        [DisplayName("总金额")]
        public decimal TotalAmount => TotalPrice;

        /// <summary>总重量（由Service层计算）</summary>
        [DisplayName("总重量")]
        public decimal TotalWeight { get; set; }
    }

    /// <summary>
    /// 创建处方DTO - 用于API接口
    /// OpenSpec: optimize-entity-data-flow - 保留以兼容现有API
    /// </summary>
    public class PrescriptionCreateDto : IRemarkable
    {
        /// <summary>处方编号</summary>
        [DisplayName("处方编号")]
        [StringLength(50)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>诊断</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>剂数</summary>
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int Quantity { get; set; } = 7;

        /// <summary>用法说明</summary>
        [StringLength(200, ErrorMessage = "用法说明不能超过200个字符")]
        [DisplayName("用法说明")]
        public string? Usage { get; set; }

        /// <summary>用药建议</summary>
        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("用药建议")]
        public string? Advice { get; set; }

        /// <summary>验方来源</summary>
        [StringLength(100, ErrorMessage = "方剂来源不能超过100个字符")]
        [DisplayName("方剂来源")]
        public string? FormulaSource { get; set; }

        /// <summary>总金额</summary>
        [Range(0, double.MaxValue, ErrorMessage = "总金额必须大于等于0")]
        [DisplayName("总金额")]
        public decimal TotalAmount { get; set; }

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>处方项目</summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemInputDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 编辑处方DTO - 用于API接口
    /// OpenSpec: optimize-entity-data-flow - 保留以兼容现有API
    /// </summary>
    public class PrescriptionEditDto : IIdentifiable<Guid>, IRemarkable
    {
        /// <inheritdoc/>
        [Required(ErrorMessage = "处方ID不能为空")]
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>诊断</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>剂数</summary>
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>用法</summary>
        [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>总价格</summary>
        [Range(0, double.MaxValue, ErrorMessage = "总价格必须大于等于0")]
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        /// <summary>折扣</summary>
        [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>处方项目</summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemInputDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 处方项目DTO - 继承基础DTO提供ID字段
    /// </summary>
    public class PrescriptionItemDto : BaseDto, IRemarkable
    {
        [DisplayName("中药材ID")]
        public Guid HerbId { get; set; }

        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        [DisplayName("剂量")]
        public int Dosage { get; set; }

        [DisplayName("总价")]
        public decimal TotalPrice { get; set; }

        [DisplayName("总重量")]
        public decimal TotalWeight { get; set; }

        [DisplayName("小计金额")]
        public decimal Subtotal { get; set; }

        [DisplayName("用法说明")]
        public string? Usage { get; set; }

        [DisplayName("煎法")]
        public Enums.DecocteMethod DecocteMethod { get; set; } = Enums.DecocteMethod.Default;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        /// <summary>
        /// 备注(兼容旧代码)
        /// </summary>
        [DisplayName("备注")]
        public string? Notes { get => Remark; set => Remark = value; }
    }

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

    /// <summary>
    /// 处方统计DTO - 继承统计DTO基础类
    /// </summary>
    public class PrescriptionStatisticsDto : StatisticsDto
    {

        [DisplayName("草稿处方数量")]
        public int DraftCount { get; set; }

        [DisplayName("待审核处方数量")]
        public int PendingCount { get; set; }

        [DisplayName("已完成处方数量")]
        public int CompletedCount { get; set; }

        [DisplayName("已取消处方数量")]
        public int CancelledCount { get; set; }

        [DisplayName("总金额")]
        public decimal TotalAmount { get; set; }

        [DisplayName("平均金额")]
        public decimal AverageAmount { get; set; }
    }

    /// <summary>
    /// 处方统计DTO
    /// </summary>
    public class PrescriptionStatsDto
    {
        [DisplayName("总数")]
        public int TotalCount { get; set; }

        [DisplayName("草稿数")]
        public int DraftCount { get; set; }

        [DisplayName("完成数")]
        public int CompletedCount { get; set; }
    }

    /// <summary>
    /// 处方主页统计DTO (Issue #1163)
    /// 为Desktop端PrescriptionsMainViewModel提供统计数据
    /// </summary>
    public class PrescriptionMainStatisticsDto
    {
        /// <summary>总处方数</summary>
        [DisplayName("总处方数")]
        public int TotalCount { get; set; }

        /// <summary>今日处方数</summary>
        [DisplayName("今日处方数")]
        public int TodayCount { get; set; }

        /// <summary>今日总金额</summary>
        [DisplayName("今日总金额")]
        public decimal TodayTotalAmount { get; set; }
    }

    /// <summary>
    /// 处方日期范围统计DTO (Issue #1163)
    /// 为Desktop端提供日期范围统计数据
    /// </summary>
    public class PrescriptionRangeStatisticsDto
    {
        /// <summary>处方数量</summary>
        [DisplayName("处方数量")]
        public int Count { get; set; }

        /// <summary>总金额</summary>
        [DisplayName("总金额")]
        public decimal TotalAmount { get; set; }

        /// <summary>平均金额</summary>
        [DisplayName("平均金额")]
        public decimal AvgAmount { get; set; }
    }
}
