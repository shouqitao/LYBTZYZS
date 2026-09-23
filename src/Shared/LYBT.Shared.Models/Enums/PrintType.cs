using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 打印类型枚举 —— <c>MedicalCasePrintLog.PrintType</c> / <c>RecordPrintRequest.PrintType</c> 的取值域。
    /// 需求来源：US-PRINT-004 业务规则 2（`Prescription`(0) 已实现；`Formula`(1) 预留）。
    /// </summary>
    /// <remarks>
    /// 持久化/线上传输仍为 <c>int</c>（DB 列 <c>MedicalCasePrintLog.PrintType</c>、API 契约
    /// <c>RecordPrintRequest.PrintType</c> 均为 int）——枚举仅作为代码侧取值语义，赋值处显式转换，
    /// 不改变任何已存储值的含义。
    /// </remarks>
    [Description("打印类型")]
    public enum PrintType
    {
        /// <summary>处方笺打印（US-PRINT-001，已实现；持久化值 0）</summary>
        [Description("处方笺")]
        Prescription = 0,

        /// <summary>验方打印（预留，尚未实现；持久化值 1）</summary>
        [Description("验方")]
        Formula = 1
    }
}
