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

        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid UserId { get; set; }

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

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        [DisplayName("处方项目")]
        public List<PrescriptionItemDto> Items { get; set; } = new();

        /// <summary>单帖价格（计算属性）</summary>
        [DisplayName("单帖价格")]
        public decimal SingleDosePrice => CalculateSingleDosePrice();

        /// <summary>总价格（计算属性）</summary>
        [DisplayName("总价格")]
        public decimal TotalPrice => SingleDosePrice * DosageCount;

        /// <summary>
        /// 总金额（兼容性别名，映射到TotalPrice）
        /// </summary>
        [DisplayName("总金额")]
        public decimal TotalAmount => TotalPrice;

        /// <summary>总重量（计算属性）</summary>
        [DisplayName("总重量")]
        public decimal TotalWeight => CalculateTotalWeight();

        /// <summary>
        /// 计算单帖价格 - 简化逻辑
        /// </summary>
        private decimal CalculateSingleDosePrice()
        {
            if (Items?.Any() != true) return 0m;
            var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
            return subtotal * Discount;
        }

        /// <summary>
        /// 计算总重量 - 简化逻辑
        /// </summary>
        private decimal CalculateTotalWeight()
        {
            if (Items?.Any() != true) return 0m;
            return Items.Sum(item => item.Quantity) * DosageCount;
        }
    }

    /// <summary>
    /// 处方详情DTO
    /// </summary>
    public class PrescriptionDetailDto : PrescriptionDto, IRemarkable
    {

        [DisplayName("方剂来源")]
        public new string? FormulaSource { get; set; }

        [DisplayName("重复用药警告")]
        public string? DuplicateWarning { get; set; }

        [DisplayName("缺药警告")]
        public string? MissingDrugWarning { get; set; }

        [DisplayName("处方编号")]
        public string? PrescriptionNo { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        [DisplayName("折扣")]
        public new decimal Discount { get; set; } = 1.0m;

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public new string? Remark { get; set; }
    }

    /// <summary>
    /// 处方输入基础DTO - 简化版，只保留基本验证
    /// </summary>
    public abstract class PrescriptionInputBaseDto : IRemarkable
    {
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int DosageCount { get; set; } = 7;

        [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
        [DisplayName("用药建议")]
        public string? Advice { get; set; }

        [DisplayName("处方项目")]
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();

        /// <inheritdoc/>
        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 创建处方DTO - 继承处方输入基础DTO
    /// </summary>
    public class PrescriptionCreateDto : PrescriptionInputBaseDto
    {
        /// <summary>
        /// 处方编号
        /// </summary>
        [DisplayName("处方编号")]
        [StringLength(50)]
        public string? PrescriptionNumber { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        [DisplayName("患者姓名")]
        [StringLength(50)]
        public string? PatientName { get; set; }

        /// <summary>
        /// 医生姓名
        /// </summary>
        [DisplayName("医生姓名")]
        [StringLength(50)]
        public string? DoctorName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        [DisplayName("诊疗ID")]
        public Guid? ConsultationId { get; set; }

        [StringLength(50, ErrorMessage = "剂型长度不能超过50个字符")]
        [DisplayName("剂型")]
        public string? DosageForm { get; set; }

        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        [DisplayName("剂数")]
        public int Quantity { get; set; } = 7;

        [StringLength(200, ErrorMessage = "用法说明不能超过200个字符")]
        [DisplayName("用法说明")]
        public string? Usage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "总金额必须大于等于0")]
        [DisplayName("总金额")]
        public decimal TotalAmount { get; set; }

        [StringLength(100, ErrorMessage = "方剂来源不能超过100个字符")]
        [DisplayName("方剂来源")]
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 草药项目列表
        /// </summary>
        public List<PrescriptionItemCreateDto> Items { get; set; } = new();
    }

    /// <summary>
    /// 编辑处方DTO - 继承处方输入基础DTO并添加ID字段
    /// </summary>
    public class PrescriptionEditDto : PrescriptionInputBaseDto, IIdentifiable<Guid>
    {

        /// <inheritdoc/>
        [Required(ErrorMessage = "处方ID不能为空")]
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "患者ID不能为空")]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "医生ID不能为空")]
        [DisplayName("医生ID")]
        public Guid UserId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "总价格必须大于等于0")]
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;
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

        [DisplayName("用量")]
        public decimal Quantity { get; set; }

        [DisplayName("单位")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        [DisplayName("剂量")]
        public decimal Dosage { get; set; }

        [DisplayName("总价")]
        public decimal TotalPrice { get; set; }

        [DisplayName("总重量")]
        public decimal TotalWeight { get; set; }

        [DisplayName("小计金额")]
        public decimal Subtotal { get; set; }

        [DisplayName("用法说明")]
        public string? Usage { get; set; }

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
    /// 创建处方项目DTO
    /// </summary>
    /// <summary>
    /// 处方项更新DTO
    /// </summary>
    public class PrescriptionItemUpdateDto
    {
        /// <summary>
        /// 草药ID
        /// </summary>
        [DisplayName("草药ID")]
        public Guid HerbId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [DisplayName("数量")]
        [Range(0.01, 9999.99)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [DisplayName("单位")]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 剂量
        /// </summary>
        [DisplayName("剂量")]
        public decimal Dosage { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [DisplayName("备注")]
        [StringLength(200)]
        public string? Remark { get; set; }
    }

    public class PrescriptionItemCreateDto
    {

        [Required]
        public Guid HerbId { get; set; }

        [Required]
        [StringLength(100)]
        public string HerbName { get; set; } = string.Empty;

        [Range(0.1, 1000)]
        public decimal Quantity { get; set; }

        [Required]
        [StringLength(10)]
        public string Unit { get; set; } = "g";

        [Range(0, 10000)]
        public decimal UnitPrice { get; set; }

        /// <summary>小计金额</summary>
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        /// <summary>用法说明</summary>
        [StringLength(200)]
        public string? Usage { get; set; }

        /// <summary>备注（Note别名）</summary>
        [StringLength(200)]
        public string? Note { get; set; }

        [StringLength(100)]
        public string? Remark { get; set; }
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
    /// 处方查询DTO - 继承完整分页查询DTO，提供分页、时间范围、关键词搜索功能
    /// </summary>
    public class PrescriptionQueryDto : ExtendedQueryDto
    {

        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        [DisplayName("医生ID")]
        public Guid? DoctorId { get; set; }

        [DisplayName("处方状态")]
        public PrescriptionStatus? PrescriptionStatus { get; set; }

        [DisplayName("排序字段")]
        public string OrderBy { get; set; } = "CreateTime";

        [DisplayName("升序排序")]
        public bool IsAscending { get; set; } = false;
    }

    /// <summary>
    /// 处方高级搜索DTO - 支持多条件搜索
    /// </summary>
    public class PrescriptionSearchDto
    {

        [DisplayName("关键词")]
        public string? Keyword { get; set; }

        [DisplayName("患者姓名")]
        public string? Name { get; set; }

        [DisplayName("医生姓名")]
        public string? DoctorName { get; set; }

        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        [DisplayName("处方状态")]
        public PrescriptionStatus? Status { get; set; }

        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        [DisplayName("最小剂数")]
        public int? MinDosageCount { get; set; }

        [DisplayName("最大剂数")]
        public int? MaxDosageCount { get; set; }

        [DisplayName("药材名称")]
        public string? HerbName { get; set; }
    }

    /// <summary>
    /// 处方复制DTO - 用于复制现有处方
    /// </summary>
    public class PrescriptionCopyDto
    {

        [Required(ErrorMessage = "新处方名称不能为空")]
        [StringLength(200, ErrorMessage = "新处方名称不能超过200个字符")]
        [DisplayName("新处方名称")]
        public string NewName { get; set; } = string.Empty;

        [DisplayName("复制处方项目")]
        public bool CopyItems { get; set; } = true;

        [DisplayName("复制用法用量")]
        public bool CopyUsage { get; set; } = true;

        [DisplayName("复制备注")]
        public bool CopyRemark { get; set; } = false;
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
}
