using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// 患者搜索缓存服务接口
    /// </summary>
    public interface IPatientSearchCache
    {
        /// <summary>
        /// 获取缓存的搜索结果
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="page">页码</param>
        /// <returns>缓存的分页结果，如果不存在或已过期则返回null</returns>
        PagedResult<PatientDto>? Get(string keyword, int page);

        /// <summary>
        /// 设置搜索结果缓存
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="page">页码</param>
        /// <param name="result">分页结果</param>
        void Set(string keyword, int page, PagedResult<PatientDto> result);

        /// <summary>
        /// 失效缓存
        /// </summary>
        /// <param name="keyword">可选的关键字，如果为null则清空所有缓存</param>
        void Invalidate(string? keyword = null);
    }
}
