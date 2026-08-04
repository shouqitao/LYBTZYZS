using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 追加打印日志命令处理器
/// 打印成功时回写医案打印状态并记录日志；失败时仅记录日志。
/// </summary>
public class AddPrintLogCommandHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<AddPrintLogCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        AddPrintLogCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdAsync(request.MedicalCaseId, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        var now = DateTime.UtcNow;

        // 打印成功才回写医案打印状态
        if (request.IsSuccess)
        {
            medicalCase.IsPrinted = true;
            medicalCase.PrintCount += 1;
            medicalCase.LastPrintedAt = now;
            medicalCase.PrintVersion += 1;

            await repository.UpdateAsync(medicalCase, cancellationToken);
        }

        var printLog = new MedicalCasePrintLog
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = request.MedicalCaseId,
            PrintType = request.PrintType,
            PrintVersion = medicalCase.PrintVersion,
            PrinterName = request.PrinterName,
            PrintedBy = request.OperatorName,
            PrintedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddPrintLogAsync(printLog, cancellationToken);

        return Result<bool>.Success(true);
    }
}
