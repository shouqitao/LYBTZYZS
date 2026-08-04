namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 记录打印日志请求
    /// US-PRINT-004: 记录打印成功或失败事件
    /// </summary>
    public class PrintLogRequest
    {
        /// <summary>打印类型</summary>
        public int PrintType { get; set; }

        /// <summary>是否打印成功</summary>
        public bool IsSuccess { get; set; }

        /// <summary>打印机名称</summary>
        public string? PrinterName { get; set; }

        /// <summary>失败原因</summary>
        public string? ErrorMessage { get; set; }
    }
}
