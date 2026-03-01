using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案打印服务实现
    /// 从 MedicalCaseCommandService 拆分，负责打印回写和打印日志记录
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
        public async Task<MedicalCase?> RecordPrintCompletedAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            Guid printedBy,
            string printedByName,
            string? printerName = null)
        {
            _logger.LogInformation("[SVC] MedicalCase.RecordPrintCompleted - MedicalCaseId={MedicalCaseId} PrintType={PrintType}",
                medicalCaseId, printType);

            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.RecordPrintCompleted -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 更新打印管理字段
            medicalCase.IsPrinted = true;
            medicalCase.PrintCount++;
            medicalCase.LastPrintedAt = DateTime.Now;
            medicalCase.UpdatedAt = DateTime.Now;

            // T2-X8-10: PrintVersion 递增 (每次打印递增版本号)
            medicalCase.PrintVersion++;

            // T2-X8-11 + S4-13: 创建打印日志记录（版本快照）
            var printLog = new MedicalCasePrintLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                PrintType = printType,
                PrintVersion = medicalCase.PrintVersion,
                PrintedAt = DateTime.Now,
                PrintedBy = printedBy,
                PrintedByName = printedByName,
                PrinterName = printerName,
                IsSuccess = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            medicalCase.PrintLogs.Add(printLog);

            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.RecordPrintCompleted -> Success - MedicalCaseId={MedicalCaseId} PrintVersion={PrintVersion} PrintCount={PrintCount}",
                medicalCaseId, medicalCase.PrintVersion, medicalCase.PrintCount);

            return result;
        }

        // ========== T4-S5-02: 打印日志记录 ==========

        /// <inheritdoc />
        public async Task<bool> AddPrintLogAsync(
            Guid medicalCaseId,
            LYBT.Shared.Models.Enums.PrintType printType,
            bool isSuccess,
            Guid printedBy,
            string printedByName,
            string? printerName = null,
            string? errorMessage = null)
        {
            _logger.LogInformation("[SVC] MedicalCase.AddPrintLog - MedicalCaseId={MedicalCaseId} IsSuccess={IsSuccess}",
                medicalCaseId, isSuccess);

            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.AddPrintLog -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return false;
            }

            // 成功时更新打印管理字段
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

            medicalCase.PrintLogs.Add(printLog);
            await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("[SVC] MedicalCase.AddPrintLog -> Success - MedicalCaseId={MedicalCaseId} IsSuccess={IsSuccess}",
                medicalCaseId, isSuccess);

            return true;
        }
    }
}
