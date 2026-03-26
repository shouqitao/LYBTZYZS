using System.Threading;
using LYBT.Entities.MedicalCases;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医案打印服务接口
    /// 职责：打印回写 + 打印日志记录
    /// 从 IMedicalCaseCommandService 拆分，降低 CommandService 行数
    /// </summary>
    public interface IMedicalCasePrintService
    {
        /// <summary>
        /// 记录打印完成 -- 更新 IsPrinted/PrintCount/LastPrintedAt，创建 PrintLog
        /// T2-X8-04~08
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="printType">打印类型</param>
        /// <param name="printedBy">打印人ID</param>
        /// <param name="printedByName">打印人姓名</param>
        /// <param name="printerName">打印机名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的医案实体</returns>
        Task<MedicalCase?> RecordPrintCompletedAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            Guid printedBy,
            string printedByName,
            string? printerName = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加打印日志（支持成功/失败记录）
        /// T4-S5-02: 比 RecordPrintCompletedAsync 更通用，支持失败日志
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="printType">打印类型</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="printedBy">打印人ID</param>
        /// <param name="printedByName">打印人姓名</param>
        /// <param name="printerName">打印机名称</param>
        /// <param name="errorMessage">错误信息（失败时）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否记录成功</returns>
        Task<bool> AddPrintLogAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            bool isSuccess,
            Guid printedBy,
            string printedByName,
            string? printerName = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default);
    }
}
