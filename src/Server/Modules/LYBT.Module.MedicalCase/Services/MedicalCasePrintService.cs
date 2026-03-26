using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案打印服务实现
    /// �?MedicalCaseCommandService 拆分，负责打印回写和打印日志记录
    /// AD-04 Fix: 使用 GetByIdWithDetailsFreshAsync 获取最�?RowVersion�?
    /// 通过 AddPrintLogAndSaveAsync 显式标记 PrintLog �?Added 状态，
    /// 避免 EF Core 将预�?Guid 的新实体通过导航属性添加时错误标记�?Modified�?
    /// </summary>
    public class MedicalCasePrintService : BaseService<MedicalCase>, IMedicalCasePrintService
    {
        private readonly IMedicalCaseRepository _repository;

        public MedicalCasePrintService(
            IMedicalCaseRepository repository,
            ILogger<MedicalCasePrintService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // ========== T2-X8-04~08: 打印回写 ==========

        /// <inheritdoc />
        public async Task\u003cMedicalCase?\u003e RecordPrintCompletedAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            Guid printedBy,
            string printedByName,
            string? printerName = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.RecordPrintCompleted - MedicalCaseId={MedicalCaseId} PrintType={PrintType}",
                medicalCaseId, printType);

            // AD-04 Fix: 使用 FreshAsync 获取最�?RowVersion
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.RecordPrintCompleted -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 更新打印管理字段
            medicalCase.IsPrinted = true;
            medicalCase.PrintCount++;
            medicalCase.LastPrintedAt = DateTime.UtcNow;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // T2-X8-10: PrintVersion 递增 (每次打印递增版本�?
            medicalCase.PrintVersion++;

            // T2-X8-11 + S4-13: 创建打印日志记录（版本快照）
            var printLog = new MedicalCasePrintLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                PrintType = printType,
                PrintVersion = medicalCase.PrintVersion,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = printedBy,
                PrintedByName = printedByName,
                PrinterName = printerName,
                IsSuccess = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // AD-04 Fix: 通过 Repository 显式 Add，确�?PrintLog 被标记为 Added 状态�?
            // 不使�?medicalCase.PrintLogs.Add(printLog)，因�?EF Core �?DetectChanges
            // 会将有预�?Guid 的新实体通过导航属性添加时错误标记�?Modified�?
            await _repository.AddPrintLogAndSaveAsync(printLog);

            _logger.LogInformation("[SVC] MedicalCase.RecordPrintCompleted -> Success - MedicalCaseId={MedicalCaseId} PrintVersion={PrintVersion} PrintCount={PrintCount}",
                medicalCaseId, medicalCase.PrintVersion, medicalCase.PrintCount);

            return medicalCase;
        }

        // ========== T4-S5-02: 打印日志记录 ==========

        /// <inheritdoc />
        public async Task\u003cbool\u003e AddPrintLogAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            bool isSuccess,
            Guid printedBy,
            string printedByName,
            string? printerName = null,
            string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.AddPrintLog - MedicalCaseId={MedicalCaseId} IsSuccess={IsSuccess}",
                medicalCaseId, isSuccess);

            // AD-04 Fix: 使用 FreshAsync 获取最�?RowVersion
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.AddPrintLog -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return false;
            }

            // 成功时更新打印管理字�?
            if (isSuccess)
            {
                medicalCase.IsPrinted = true;
                medicalCase.PrintCount++;
                medicalCase.LastPrintedAt = DateTime.UtcNow;
                medicalCase.PrintVersion++;
            }

            // 创建打印日志记录
            var printLog = new MedicalCasePrintLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                PrintType = printType,
                PrintVersion = medicalCase.PrintVersion,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = printedBy,
                PrintedByName = printedByName,
                PrinterName = printerName,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            // AD-04 Fix: 显式 Add，确�?PrintLog 被标记为 Added 状�?
            await _repository.AddPrintLogAndSaveAsync(printLog);

            _logger.LogInformation("[SVC] MedicalCase.AddPrintLog -> Success - MedicalCaseId={MedicalCaseId} IsSuccess={IsSuccess}",
                medicalCaseId, isSuccess);

            return true;
        }
    }
}
