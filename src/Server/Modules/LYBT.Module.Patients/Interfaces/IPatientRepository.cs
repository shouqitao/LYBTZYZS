using LYBT.Entities.Patients;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{

    /// <summary>
    /// 病人仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展患者特定业务方法
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public interface IPatientRepository : IBaseRepository<Patient>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义患者特有的业务方法

        /// <summary>
        /// 根据主键ID查询病人（支持权限控制）
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        Task<Patient?> GetByIdAsync(Guid id, bool includeDisabled = false);

        /// <summary>
        /// 启用患者档案
        /// </summary>
        Task<bool> EnableAsync(Guid id);

        /// <summary>
        /// 禁用患者档案（软删除）
        /// </summary>
        Task<bool> DisableAsync(Guid id);

        /// <summary>
        /// 批量禁用患者档案
        /// </summary>
        Task<int> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用患者档案
        /// </summary>
        Task<int> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 通过身份证号查找病人（包括禁用的患者档案）
        /// </summary>
        Task<Patient?> GetByIdNumberAsync(string idNumber);

        /// <summary>
        /// 通过手机号查找病人（包括禁用的患者档案）
        /// </summary>
        Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 检查身份证号是否存在（排除指定ID，包括禁用患者档案）
        /// </summary>
        Task<bool> IsIdNumberExistsAsync(string idNumber, Guid? excludeId = null);

        /// <summary>
        /// 检查手机号是否存在（排除指定ID，包括禁用患者档案）
        /// </summary>
        Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null);

        /// <summary>
        /// 根据关键词搜索患者档案
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        Task<List<Patient>> SearchAsync(string keyword, bool includeDisabled = false);

        /// <summary>
        /// 精确匹配搜索（手机号、身份证号）
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        Task<List<Patient>> ExactSearchAsync(string keyword, bool includeDisabled = false);

        /// <summary>
        /// 获取启用的患者档案列表
        /// </summary>
        Task<List<Patient>> GetActivePatientsAsync();

        /// <summary>
        /// 根据身份证号获取患者档案列表（用于重复检查）
        /// </summary>
        Task<List<Patient>> GetPatientsByIdNumberAsync(string idNumber);

        /// <summary>
        /// 根据姓名和手机号获取患者档案列表（用于重复检查）
        /// </summary>
        Task<List<Patient>> GetPatientsByNameAndPhoneAsync(string name, string phoneNumber);

        /// <summary>
        /// 根据相似姓名获取患者档案列表（用于重复检查）
        /// </summary>
        Task<List<Patient>> GetPatientsBySimilarNameAsync(string name);

        /// <summary>
        /// 根据姓名获取患者档案列表（用于查询或创建场景）
        /// </summary>
        Task<List<Patient>> GetByNameAsync(string name);
    }
}