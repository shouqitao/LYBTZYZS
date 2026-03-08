using LYBT.Shared.Models.Contracts.Registration;
using Riok.Mapperly.Abstractions;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Mapping;

/// <summary>
/// 挂号数据映射器 -- Mapperly 编译时生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class RegistrationMapper
{
    /// <summary>
    /// Registration 实体转 ListDto (队列展示)
    /// </summary>
    public partial RegistrationListDto ToListDto(RegistrationEntity entity);

    /// <summary>
    /// Registration 实体列表转 ListDto 列表
    /// </summary>
    public partial List<RegistrationListDto> ToListDtos(List<RegistrationEntity> entities);

    /// <summary>
    /// Registration 实体转 DetailDto
    /// </summary>
    public partial RegistrationDetailDto ToDetailDto(RegistrationEntity entity);

    /// <summary>
    /// InputDto 转 Registration 实体 (创建)
    /// 忽略 Id、MedicalCaseId、Status 和审计字段 (由 Service 层设置)
    /// </summary>
    [MapperIgnoreTarget(nameof(RegistrationEntity.Id))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.Status))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.RowVersion))]
    [MapperIgnoreTarget(nameof(RegistrationEntity.IsDeleted))]
    public partial RegistrationEntity ToEntity(RegistrationInputDto dto);
}
