using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Contracts.Registration;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 挂号映射器 - Entity 与 DTO 转换
/// </summary>
[Mapper]
internal partial class LocalRegistrationMapper
{
    /// <summary>
    /// Registration Entity -> RegistrationDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Registration.UpdatedBy))]
    [MapperIgnoreSource(nameof(Registration.RowVersion))]
    [MapperIgnoreSource(nameof(Registration.IsDeleted))]
    public partial RegistrationDetailDto ToDetailDto(Registration entity);

    /// <summary>
    /// Registration Entity -> RegistrationListDto
    /// </summary>
    [MapperIgnoreSource(nameof(Registration.UpdatedBy))]
    [MapperIgnoreSource(nameof(Registration.RowVersion))]
    [MapperIgnoreSource(nameof(Registration.IsDeleted))]
    [MapperIgnoreSource(nameof(Registration.Remark))]
    [MapperIgnoreSource(nameof(Registration.UpdatedAt))]
    [MapperIgnoreSource(nameof(Registration.CreatedBy))]
    public partial RegistrationListDto ToListDto(Registration entity);

    /// <summary>
    /// RegistrationInputDto -> Registration Entity
    /// </summary>
    [MapperIgnoreTarget(nameof(Registration.CreatedAt))]
    [MapperIgnoreTarget(nameof(Registration.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Registration.CreatedBy))]
    [MapperIgnoreTarget(nameof(Registration.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Registration.RowVersion))]
    [MapperIgnoreTarget(nameof(Registration.IsDeleted))]
    [MapperIgnoreTarget(nameof(Registration.Id))]
    [MapperIgnoreTarget(nameof(Registration.Status))]
    [MapperIgnoreTarget(nameof(Registration.MedicalCaseId))]
    public partial Registration ToEntity(RegistrationInputDto dto);
}
