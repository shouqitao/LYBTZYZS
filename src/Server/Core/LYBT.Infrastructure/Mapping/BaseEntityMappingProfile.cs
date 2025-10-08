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
            // 注：审计字段（CreatedAt, UpdatedAt 等）由 AppDbContext.SetAuditFields() 统一管理
            // 不需要全局 AutoMapper 配置，避免循环映射错误
            // 如需在特定实体 Profile 中忽略审计字段，请在该实体的 Profile 中单独配置
        }
    }
}
