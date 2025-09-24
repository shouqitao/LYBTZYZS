using System.ComponentModel;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 药材仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展药材特定业务方法
    /// </summary>
    public interface IHerbRepository : IRepository<Herb>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义药材特有的业务方法



    }
}
