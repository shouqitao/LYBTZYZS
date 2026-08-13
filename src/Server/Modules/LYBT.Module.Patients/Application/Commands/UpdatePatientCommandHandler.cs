using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Utilities.Text;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 更新患者命令处理器。
/// </summary>
public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;

    public UpdatePatientCommandHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, ErrorMessages.Get(ErrorCode.PatientNotFound));

        // P2 (US-PAT-004): 电话唯一——更新时排除自身查重 409
        if (
            !string.IsNullOrWhiteSpace(request.Input.PhoneNumber)
            && await _patientRepository.ExistsByPhoneAsync(
                request.Input.PhoneNumber,
                excludeId: request.Id,
                ct: cancellationToken
            )
        )
            return Result<PatientDetailDto>.Failure(
                ErrorCode.PatientPhoneDuplicate,
                "该手机号已关联其他患者"
            );

        // 拼音码兜底（对齐 B-03：更新未传 PinYinCode 时保留旧值；旧值也空则按新姓名生成）
        if (string.IsNullOrWhiteSpace(request.Input.PinYinCode))
            request.Input.PinYinCode = string.IsNullOrWhiteSpace(patient.PinYinCode)
                ? PinYinHelper.GetPinYinCode(request.Input.Name)
                : patient.PinYinCode;

        patient.UpdateProfile(
            request.Input.Name,
            request.Input.Gender,
            request.Input.BirthDate,
            request.Input.PhoneNumber,
            request.Input.IdNumber,
            request.Input.PinYinCode,
            request.CurrentUserId);

        await _patientRepository.UpdateAsync(patient, cancellationToken);
        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}
