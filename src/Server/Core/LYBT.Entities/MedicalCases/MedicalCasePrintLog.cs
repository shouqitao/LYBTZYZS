using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.MedicalCases;

/// <summary>
/// 医案打印日志 - 记录每次打印操作
/// </summary>
[Table("MedicalCasePrintLogs")]
public class MedicalCasePrintLog : BaseEntity
{
    /// <summary>医案ID</summary>
    [Required]
    [DisplayName("医案ID")]
    public Guid MedicalCaseId { get; set; }

    /// <summary>打印类型 0=处方</summary>
    [DisplayName("打印类型")]
    public int PrintType { get; set; }

    /// <summary>打印版本号</summary>
    [DisplayName("打印版本号")]
    public int PrintVersion { get; set; }

    /// <summary>打印机名称</summary>
    [StringLength(100)]
    [DisplayName("打印机名称")]
    public string? PrinterName { get; set; }

    /// <summary>打印人（用户名）</summary>
    [StringLength(50)]
    [DisplayName("打印人")]
    public string? PrintedBy { get; set; }

    /// <summary>打印时间</summary>
    [DisplayName("打印时间")]
    public DateTime PrintedAt { get; set; }
}
