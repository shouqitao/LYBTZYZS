using LYBT.Shared.Models.Contracts.Registration;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Application.Mappers;

/// <summary>
/// 挂号数据映射器。
/// </summary>
public static class RegistrationMapper
{
    /// <summary>
    /// Registration 实体转 ListDto。
    /// </summary>
    public static RegistrationListDto ToListDto(RegistrationEntity entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        PatientName = entity.PatientName,
        DoctorId = entity.DoctorId,
        DoctorName = entity.DoctorName,
        MedicalCaseId = entity.MedicalCaseId,
        QueueNumber = entity.QueueNumber,
        RegistrationFee = entity.RegistrationFee,
        Source = entity.Source,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };

    /// <summary>
    /// Registration 实体转 DetailDto。
    /// </summary>
    public static RegistrationDetailDto ToDetailDto(RegistrationEntity entity) => new()
    {
        Id = entity.Id,
        PatientId = entity.PatientId,
        PatientName = entity.PatientName,
        DoctorId = entity.DoctorId,
        DoctorName = entity.DoctorName,
        MedicalCaseId = entity.MedicalCaseId,
        Source = entity.Source,
        QueueNumber = entity.QueueNumber,
        RegistrationFee = entity.RegistrationFee,
        Status = entity.Status,
        Remark = entity.Remark,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedBy = entity.CreatedBy
    };
}


