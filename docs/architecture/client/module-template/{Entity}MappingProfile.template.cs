using AutoMapper;
using LYBT.Entities.{Module};
using LYBT.Shared.Models.Contracts.{Module};

namespace LYBT.Desktop.Services.Mapping;

/// <summary>
/// {Entity} AutoMapper 映射配置
/// 职责：定义 Entity ↔ DTO 的双向映射规则
/// </summary>
public class {Entity}MappingProfile : Profile
{
    public {Entity}MappingProfile()
    {
        // Entity → Dto (查询场景)
        CreateMap<{Entity}, {Entity}Dto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
            // TODO: 根据实际 DTO 结构添加其他字段映射

        // CreateDto → Entity (创建场景)
        CreateMap<Create{Entity}Dto, {Entity}>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // 由服务层设置
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // 由服务层设置
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()); // 由服务层设置
            // TODO: 根据实际 CreateDto 结构添加其他字段映射

        // UpdateDto → Entity (更新场景)
        CreateMap<Update{Entity}Dto, {Entity}>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // 保持现有值
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // 保持现有值
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()); // 由服务层设置
            // TODO: 根据实际 UpdateDto 结构添加其他字段映射
    }
}
