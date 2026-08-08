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
}


