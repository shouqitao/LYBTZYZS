using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Entities.Registrations;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.LocalWebAPI.Mappers;

public static class LocalApiMapper
{
    public static PatientListDto ToListDto(this Patient p) => new()
    {
        Id = p.Id, Name = p.Name, Gender = p.Gender, Age = p.Age,
        PhoneNumber = p.PhoneNumber,
        PinYinCode = p.PinYinCode, Status = p.Status, CreatedAt = p.CreatedAt
    };

    public static PatientDetailDto ToDetailDto(this Patient p) => new()
    {
        Id = p.Id, Name = p.Name, Gender = p.Gender, BirthDate = p.BirthDate,
        Age = p.Age, IdNumber = p.IdNumber, PhoneNumber = p.PhoneNumber,
        PinYinCode = p.PinYinCode, Status = p.Status, CreatedAt = p.CreatedAt
    };

    public static UserListDto ToListDto(this ApplicationUser u) => new()
    {
        Id = u.Id, UserName = u.UserName, RealName = u.RealName,
        Role = u.Role, Status = u.Status
    };

    public static UserDetailDto ToDetailDto(this ApplicationUser u) => new()
    {
        Id = u.Id, UserName = u.UserName, RealName = u.RealName,
        Role = u.Role, Status = u.Status
    };

    public static RegistrationListDto ToListDto(this Registration r) => new()
    {
        Id = r.Id, PatientId = r.PatientId, Status = r.Status, Source = r.Source, DoctorId = r.DoctorId, MedicalCaseId = r.MedicalCaseId,
        QueueNumber = r.QueueNumber, RegistrationFee = r.RegistrationFee,
        CreatedAt = r.CreatedAt
    };

    public static RegistrationDetailDto ToDetailDto(this Registration r) => new()
    {
        Id = r.Id, PatientId = r.PatientId, Status = r.Status, Source = r.Source, DoctorId = r.DoctorId, MedicalCaseId = r.MedicalCaseId,
        QueueNumber = r.QueueNumber, RegistrationFee = r.RegistrationFee,
        CreatedAt = r.CreatedAt
    };

    public static MedicalCaseListDto ToListDto(this MedicalCase m) => new()
    {
        Id = m.Id, PatientId = m.PatientId, CaseStatus = m.CaseStatus
    };

    public static MedicalCaseDetailDto ToDetailDto(this MedicalCase m) => new()
    {
        Id = m.Id, PatientId = m.PatientId, CaseStatus = m.CaseStatus
    };
}
