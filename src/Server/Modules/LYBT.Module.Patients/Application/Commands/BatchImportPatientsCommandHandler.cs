using MediatR;
using LYBT.Module.Patients.Application.Mappers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量导入患者命令处理器 — 与药材批量导入同构（Skip/Update/Error 重复策略）。
/// </summary>
public class BatchImportPatientsCommandHandler : IRequestHandler<BatchImportPatientsCommand, Result<PatientBatchImportResultDto>>
{
    private readonly IPatientRepository _patientRepository;

    public BatchImportPatientsCommandHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientBatchImportResultDto>> Handle(
        BatchImportPatientsCommand request, CancellationToken cancellationToken)
    {
        const int MAX_IMPORT_SIZE = 10000;

        var result = new PatientBatchImportResultDto
        {
            ImportTime = DateTime.UtcNow
        };

        if (request.Patients.Count > MAX_IMPORT_SIZE)
        {
            return Result<PatientBatchImportResultDto>.Failure(ErrorCode.ValidationFailed, $"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
        }

        for (var i = 0; i < request.Patients.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dto = request.Patients[i];
            var rowNumber = i + 2;

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.FailureCount++;
                    result.Failures.Add(new PatientImportFailureDto
                    {
                        OriginalRowNumber = rowNumber,
                        FailureReason = "患者姓名不能为空",
                        FieldName = "Name",
                        OriginalValue = dto.Name,
                        SuggestedFix = "填写患者姓名",
                        DataSnapshot = dto
                    });
                    continue;
                }

                var exists = await _patientRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken);

                if (exists)
                {
                    switch (request.Strategy)
                    {
                        case DuplicateStrategy.Skip:
                            result.SkippedCount++;
                            continue;

                        case DuplicateStrategy.Update:
                            var existing = await _patientRepository.GetExactByNameAsync(dto.Name, cancellationToken);
                            if (existing != null)
                            {
                                existing.UpdateProfile(
                                    dto.Name, dto.Gender, dto.BirthDate,
                                    dto.PhoneNumber, dto.IdNumber, dto.PinYinCode,
                                    request.CurrentUserId);
                                await _patientRepository.UpdateAsync(existing, cancellationToken);
                                result.SuccessCount++;
                            }
                            continue;

                        case DuplicateStrategy.Error:
                            result.FailureCount++;
                            result.Failures.Add(new PatientImportFailureDto
                            {
                                OriginalRowNumber = rowNumber,
                                FailureReason = "患者姓名重复",
                                FieldName = "Name",
                                OriginalValue = dto.Name,
                                SuggestedFix = "修改姓名或调整导入策略",
                                DataSnapshot = dto
                            });
                            continue;
                    }
                }

                var patient = PatientMapper.ToEntity(dto, request.CurrentUserId);
                await _patientRepository.AddAsync(patient, cancellationToken);
                result.SuccessCount++;
            }
            catch
            {
                result.FailureCount++;
                result.Failures.Add(new PatientImportFailureDto
                {
                    OriginalRowNumber = rowNumber,
                    FailureReason = "导入失败",
                    FieldName = "Name",
                    OriginalValue = dto.Name,
                    SuggestedFix = "数据处理异常，请检查数据格式",
                    DataSnapshot = dto
                });
            }
        }

        return Result<PatientBatchImportResultDto>.Success(result);
    }
}
