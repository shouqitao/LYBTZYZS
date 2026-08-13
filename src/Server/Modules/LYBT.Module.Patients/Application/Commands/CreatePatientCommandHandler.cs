using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Utilities.Text;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 创建患者命令处理器。
/// </summary>
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;

    public CreatePatientCommandHandler(
        IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _patientRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "患者姓名已存在");

        // P2 (US-PAT-003): 需求电话唯一语义——同电话重复拒绝 409（PatientPhoneDuplicate→ErrorCodeExtensions 409）
        if (
            !string.IsNullOrWhiteSpace(dto.PhoneNumber)
            && await _patientRepository.ExistsByPhoneAsync(dto.PhoneNumber, ct: cancellationToken)
        )
            return Result<PatientDetailDto>.Failure(
                ErrorCode.PatientPhoneDuplicate,
                "该手机号已关联其他患者"
            );

        // 拼音码自动生成兜底（真机：API 直调未传 PinYinCode → null → 拼音搜索 0 条；对齐药材 B-03 先例）
        if (string.IsNullOrWhiteSpace(dto.PinYinCode))
            dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);

        var patient = PatientMapper.ToEntity(dto, request.CurrentUserId);

        await _patientRepository.AddAsync(patient, cancellationToken);

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}


