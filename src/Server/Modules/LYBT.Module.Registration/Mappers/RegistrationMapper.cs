using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Contracts.Registration;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Registrations.Mappers;

/// <summary>
/// 挂号数据映射器 -- Mapperly 编译时生成
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class RegistrationMapper
{
    /// <summary>
    /// Registration 实体转 ListDto (队列展示)
    /// </summary>
    public partial RegistrationListDto ToListDto(Registration entity);

    /// <summary>
    /// Registration 实体列表转 ListDto 列表
    /// </summary>
    public partial List<RegistrationListDto> ToListDtos(List<Registration> entities);

    /// <summary>
    /// Registration 实体转 DetailDto
    /// </summary>
    public partial RegistrationDetailDto ToDetailDto(Registration entity);

    /// <summary>
    /// InputDto 转 Registration 实体 (创建)
    /// 忽略 Id、MedicalCaseId、Status 和审计字段 (由 Service 层设置)
    /// </summary>
    [MapperIgnoreTarget(nameof(Registration.Id))]
    [MapperIgnoreTarget(nameof(Registration.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(Registration.Status))]
    [MapperIgnoreTarget(nameof(Registration.QueueNumber))]
    [MapperIgnoreTarget(nameof(Registration.CreatedAt))]
    [MapperIgnoreTarget(nameof(Registration.CreatedBy))]
    [MapperIgnoreTarget(nameof(Registration.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Registration.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Registration.RowVersion))]
    [MapperIgnoreTarget(nameof(Registration.IsDeleted))]
    public partial Registration ToEntity(RegistrationInputDto dto);
}


