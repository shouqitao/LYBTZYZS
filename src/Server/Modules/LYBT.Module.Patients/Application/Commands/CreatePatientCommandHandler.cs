using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
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

        // P2 (US-PAT-003): 需求电话唯一语义——同电话重复拒绝（原仅姓名查重）
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)
            && await _patientRepository.ExistsByPhoneAsync(dto.PhoneNumber, ct: cancellationToken))
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "该手机号已关联其他患者");

        var patient = PatientMapper.ToEntity(dto, request.CurrentUserId);

        await _patientRepository.AddAsync(patient, cancellationToken);

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}


