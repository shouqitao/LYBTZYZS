using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方输入DTO - 统一创建、编辑和聚合保存
/// OpenSpec: refactor-dto-simplification, simplify-medicalcase-dataflow
/// </summary>
/// <remarks>
/// InputDto设计原则：
/// - 只含可写字段，排除系统字段（CreatedAt/UpdatedAt）
/// - 排除计算字段（SingleDosePrice/TotalWeight由Service计算）
/// - 排除状态字段（Status通过专用API修改）
/// - Id可空：null=创建，有值=更新
///
/// 使用场景：
/// - 独立创建处方：MedicalCaseId必填
/// - 作为MedicalCaseInputDto嵌套：MedicalCaseId从父对象推导
/// - 不需要处方：NeedsPrescription=false
/// </remarks>
public class PrescriptionInputDto
{
    /// <summary>处方ID（更新时必填，创建时为null）</summary>
    [DisplayName("处方ID")]
    public Guid? Id { get; set; }

    /// <summary>医疗案例ID（独立调用时必填，嵌套调用时从父对象推导）</summary>
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    /// <summary>
    /// 是否需要开处方
    /// OpenSpec: simplify-medicalcase-dataflow - 从PrescriptionAggregateInputDto迁移
    /// 当NeedsPrescription=false时，不创建空的处方记录
    /// </summary>
    [DisplayName("是否开处方")]
    public bool NeedsPrescription { get; set; } = true;

    // Diagnosis已删除 - 冗余字段
    // Indication已删除 - 打印时从Consultation.TcmDiagnosis获取

    /// <summary>剂数</summary>
    [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
    [DisplayName("剂数")]
    public int DosageCount { get; set; } = 7;

    /// <summary>用法</summary>
    [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>医嘱/用药建议</summary>
    [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
    [DisplayName("医嘱")]
    public string? Advice { get; set; }

    // FormulaSource已删除 - 与ReferencedFormulas功能重复

    /// <summary>引用的验方名称列表，逗号分隔</summary>
    [StringLength(500, ErrorMessage = "引用验方名称长度不能超过500个字符")]
    [DisplayName("引用验方")]
    public string? ReferencedFormulas { get; set; }

    /// <summary>折扣（0-1之间）</summary>
    [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
    [DisplayName("折扣")]
    public decimal Discount { get; set; } = 1.0m;

    /// <summary>总价格</summary>
    [Range(0, double.MaxValue, ErrorMessage = "总价格必须大于等于0")]
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>备注</summary>
    [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>处方项目列表</summary>
    [Required(ErrorMessage = "必须包含至少一个处方项目")]
    [DisplayName("处方项目")]
    public List<PrescriptionItemInputDto> Items { get; set; } = new();
}
