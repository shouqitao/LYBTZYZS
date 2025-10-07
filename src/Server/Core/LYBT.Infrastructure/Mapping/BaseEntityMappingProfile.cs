using AutoMapper;
using LYBT.Entities.Common;

namespace LYBT.Infrastructure.Mapping
{
    /// <summary>
    /// BaseEntity 全局 AutoMapper 配置
    /// 自动忽略所有继承 BaseEntity 的实体的审计字段
    /// 由 AppDbContext.SetAuditFields 统一负责这些字段的设置
    /// </summary>
    public class BaseEntityMappingProfile : Profile
    {
        public BaseEntityMappingProfile()
        {
            // 全局配置：所有映射到 BaseEntity 或其派生类的操作都忽略审计字段
            CreateMap<object, BaseEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .IncludeAllDerived();
        }
    }
}
