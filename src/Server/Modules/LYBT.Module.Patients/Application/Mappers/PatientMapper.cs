using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Entities.Patients;

namespace LYBT.Module.Patients.Application.Mappers;

/// <summary>
/// 患者数据映射器。静态类，用于 Domain 实体与 DTO 之间的转换。
/// </summary>
public static class PatientMapper
{
    /// <summary>
    /// PatientInputDto 转换为 Patient 实体（创建）。
    /// </summary>
    public static Patient ToEntity(PatientInputDto dto, Guid? createdBy = null) => Patient.Create(
        dto.Name,
        dto.Gender,
        dto.BirthDate,
        dto.PhoneNumber,
        dto.IdNumber,
        dto.PinYinCode,
        createdBy);

    /// <summary>
    /// Patient 实体转换为 PatientListDto（列表查询）。
    /// </summary>
    public static PatientListDto ToListDto(Patient entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Gender = entity.Gender,
        Age = entity.Age,
        PhoneNumber = entity.PhoneNumber,
        PinYinCode = entity.PinYinCode,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };

    /// <summary>
    /// Patient 实体转换为 PatientDetailDto（详情查询）。
    /// </summary>
    public static PatientDetailDto ToDetailDto(Patient entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Gender = entity.Gender,
        BirthDate = entity.BirthDate,
        Age = entity.Age,
        IdNumber = entity.IdNumber,
        PhoneNumber = entity.PhoneNumber,
        PinYinCode = entity.PinYinCode,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedBy = entity.CreatedBy
    };
}


