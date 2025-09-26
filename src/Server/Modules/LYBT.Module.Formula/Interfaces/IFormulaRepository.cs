using LYBT.Infrastructure.Interfaces;
using FormulaEntity = LYBT.Entities.Formula.Formula;

namespace LYBT.Module.Formula.Interfaces
{

    /// <summary>
    /// 验方仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展验方特定业务方法
    /// </summary>
    public interface IFormulaRepository : IRepository<FormulaEntity>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义验方特有的业务方法

        /// <summary>
        /// 获取模板验方列表
        /// </summary>
        Task<List<FormulaEntity>> GetTemplatesAsync();
    }
}
