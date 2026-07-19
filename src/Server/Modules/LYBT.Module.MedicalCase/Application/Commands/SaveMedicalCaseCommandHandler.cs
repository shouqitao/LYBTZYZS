using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class SaveMedicalCaseCommandHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper,
    IPatientCrossModuleService patientCrossModule,
    IUserCrossModuleService userCrossModule,
    IRegistrationCrossModuleService registrationCrossModule,
    IHerbCrossModuleService herbCrossModule,
    ILogger<SaveMedicalCaseCommandHandler> logger
) : IRequestHandler<SaveMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    private readonly ILogger<SaveMedicalCaseCommandHandler> _logger = logger;

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        SaveMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;

        if (!input.Id.HasValue)
        {
            return await HandleCreateAsync(input, request.OperatorId, request.IsAdmin, cancellationToken);
        }

        return await HandleUpdateAsync(input, input.Id.Value, request.OperatorId, request.IsAdmin, cancellationToken);
    }

    private async Task<Result<MedicalCaseDetailDto>> HandleCreateAsync(
        MedicalCaseInputDto input, Guid operatorId, bool isAdmin, CancellationToken ct)
    {
        var doctorId = input.UserId != Guid.Empty ? input.UserId : operatorId;

        var patient = await patientCrossModule.GetPatientBasicInfoAsync(input.PatientId, ct);
        if (patient == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "患者不存在");

        var doctor = await userCrossModule.GetUserBasicInfoAsync(doctorId, ct);
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
                var herbPrices = await herbCrossModule.GetHerbPricesAsync(herbIds, ct);

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

        var result = await repository.AddAsync(medicalCase, ct);

        if (input.RegistrationId.HasValue)
        {
            await registrationCrossModule.LinkRegistrationToMedicalCaseAsync(
                input.RegistrationId.Value, result.Id, ct);
        }

        var dto = mapper.MapToMedicalCaseDetailDto(result);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }

    private async Task<Result<MedicalCaseDetailDto>> HandleUpdateAsync(
        MedicalCaseInputDto input, Guid medicalCaseId, Guid operatorId, bool isAdmin, CancellationToken ct)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(medicalCaseId, ct);
        if (medicalCase == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.UpdatedAt = DateTime.UtcNow;

        if (input.Consultation != null && medicalCase.Consultation != null)
        {
            medicalCase.UpdateConsultation(
                input.Consultation.PresentIllness,
                input.Consultation.TongueDiagnosis,
                input.Consultation.PulseDiagnosis,
                input.Consultation.TcmDiagnosis);
        }

        if (input.Prescription != null)
        {
            medicalCase.NeedsPrescription = input.Prescription.NeedsPrescription;

            if (!input.Prescription.NeedsPrescription)
            {
                if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
                {
                    medicalCase.Prescription.IsDeleted = true;
                    medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
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
                    var herbPrices = await herbCrossModule.GetHerbPricesAsync(herbIds, ct);

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
            else
            {
                var existing = medicalCase.Prescription;
                existing.DosageCount = input.Prescription.DosageCount;
                existing.Usage = input.Prescription.Usage;
                existing.Advice = input.Prescription.Advice;
                existing.Discount = input.Prescription.Discount;
                existing.ReferencedFormulas = input.Prescription.ReferencedFormulas;
                existing.Remark = input.Prescription.Remark;
                existing.UpdatedAt = DateTime.UtcNow;

                if (input.Prescription.Items != null)
                {
                    existing.Items.Clear();
                    var herbIds = input.Prescription.Items.Select(i => i.HerbId).Distinct().ToList();
                    var herbPrices = await herbCrossModule.GetHerbPricesAsync(herbIds, ct);

                    foreach (var itemDto in input.Prescription.Items)
                    {
                        var unitPrice = herbPrices.TryGetValue(itemDto.HerbId, out var price) ? price : itemDto.UnitPrice;
                        existing.Items.Add(new LYBT.Entities.Prescriptions.PrescriptionItem
                        {
                            Id = Guid.NewGuid(),
                            PrescriptionId = existing.Id,
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
            }
        }

        var result = await repository.UpdateAsync(medicalCase, ct);
        var dto = mapper.MapToMedicalCaseDetailDto(result);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}


