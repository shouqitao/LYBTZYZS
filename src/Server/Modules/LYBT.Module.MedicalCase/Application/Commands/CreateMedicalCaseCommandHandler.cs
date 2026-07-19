using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class CreateMedicalCaseCommandHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper,
    IPatientCrossModuleService patientCrossModule,
    IUserCrossModuleService userCrossModule,
    IRegistrationCrossModuleService registrationCrossModule,
    IHerbCrossModuleService herbCrossModule
) : IRequestHandler<CreateMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    public async Task<Result<MedicalCaseDetailDto>> Handle(
        CreateMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var doctorId = input.UserId != Guid.Empty ? input.UserId : request.CurrentUserId;

        var patient = await patientCrossModule.GetPatientBasicInfoAsync(input.PatientId, cancellationToken);
        if (patient == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "患者不存在");

        var doctor = await userCrossModule.GetUserBasicInfoAsync(doctorId, cancellationToken);
        if (doctor == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医生不存在");

        var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
        {
            Id = Guid.NewGuid(),
            PatientId = input.PatientId,
            PatientName = patient.Name,
            UserId = doctorId,
            DoctorName = doctor.RealName,
            CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
            NeedsPrescription = input.Prescription?.NeedsPrescription,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var consultation = new LYBT.Entities.Consultations.Consultation
        {
            Id = medicalCase.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (input.Consultation != null)
        {
            consultation.PresentIllness = input.Consultation.PresentIllness;
            consultation.TongueDiagnosis = input.Consultation.TongueDiagnosis;
            consultation.PulseDiagnosis = input.Consultation.PulseDiagnosis;
            consultation.TcmDiagnosis = input.Consultation.TcmDiagnosis;
        }

        medicalCase.Consultation = consultation;

        if (input.Prescription != null && input.Prescription.NeedsPrescription)
        {
            var prescription = new LYBT.Entities.Prescriptions.Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCase.Id,
                DosageCount = input.Prescription.DosageCount,
                Usage = input.Prescription.Usage,
                Advice = input.Prescription.Advice,
                Discount = input.Prescription.Discount,
                ReferencedFormulas = input.Prescription.ReferencedFormulas,
                Remark = input.Prescription.Remark,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>()
            };

            if (input.Prescription.Items != null)
            {
                var herbIds = input.Prescription.Items.Select(i => i.HerbId).Distinct().ToList();
                var herbPrices = await herbCrossModule.GetHerbPricesAsync(herbIds, cancellationToken);

                foreach (var itemDto in input.Prescription.Items)
                {
                    var unitPrice = herbPrices.TryGetValue(itemDto.HerbId, out var price) ? price : itemDto.UnitPrice;
                    prescription.Items.Add(new LYBT.Entities.Prescriptions.PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = prescription.Id,
                        HerbId = itemDto.HerbId,
                        HerbName = itemDto.HerbName ?? string.Empty,
                        Dosage = itemDto.Dosage,
                        Unit = itemDto.Unit,
                        UnitPrice = unitPrice,
                        Usage = itemDto.Usage,
                        Remark = itemDto.Remark
                    });
                }
            }

            medicalCase.Prescription = prescription;
        }

        var result = await repository.AddAsync(medicalCase, cancellationToken);

        if (input.RegistrationId.HasValue)
        {
            await registrationCrossModule.LinkRegistrationToMedicalCaseAsync(
                input.RegistrationId.Value, result.Id, cancellationToken);
        }

        var dto = mapper.MapToMedicalCaseDetailDto(result);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}


