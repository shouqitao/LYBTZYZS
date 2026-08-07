using LYBT.Shared.Models.Contracts.Registration;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Registration.Mappers;

/// <summary>
/// 挂号数据映射器 -- Mapperly 编译时生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class RegistrationMapper
{
    /// <summary>
    /// Registration 实体转 ListDto (队列展示)
    /// </summary>
    public partial RegistrationListDto ToListDto(LYBT.Entities.Registrations.Registration entity);

    /// <summary>
    /// Registration 实体列表转 ListDto 列表
    /// </summary>
    public partial List<RegistrationListDto> ToListDtos(List<LYBT.Entities.Registrations.Registration> entities);

    /// <summary>
    /// Registration 实体转 DetailDto
    /// </summary>
    public partial RegistrationDetailDto ToDetailDto(LYBT.Entities.Registrations.Registration entity);

    /// <summary>
    /// InputDto 转 Registration 实体 (创建)
    /// 忽略 Id、MedicalCaseId、Status 和审计字段 (由 Service 层设置)
    /// </summary>
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.Id))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.Status))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.QueueNumber))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.CreatedAt))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.CreatedBy))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.UpdatedAt))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.UpdatedBy))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.RowVersion))]
    [MapperIgnoreTarget(nameof(LYBT.Entities.Registrations.Registration.IsDeleted))]
    public partial LYBT.Entities.Registrations.Registration ToEntity(RegistrationInputDto dto);
}


