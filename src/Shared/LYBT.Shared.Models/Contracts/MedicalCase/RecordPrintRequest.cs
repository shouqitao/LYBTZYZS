namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 记录打印完成请求
    /// US-PRINT-004: 更新医案打印状态并记录打印日志
    /// </summary>
    public class RecordPrintRequest
    {
        /// <summary>打印类型</summary>
        public int PrintType { get; set; }

        /// <summary>打印机名称</summary>
        public string? PrinterName { get; set; }
    }
}
