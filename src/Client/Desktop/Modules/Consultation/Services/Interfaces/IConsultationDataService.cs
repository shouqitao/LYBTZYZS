using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.Formulas;

namespace LYBT.Desktop.Consultation.Services.Interfaces
{
    /// <summary>
    /// 看诊数据服务接口
    /// </summary>
    public interface IConsultationDataService
    {
        /// <summary>
        /// 加载患者列表
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新缓存</param>
        Task<List<PatientInfo>> LoadPatientsAsync(bool forceRefresh = false);

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新缓存</param>
        Task<List<HerbInfo>> LoadHerbsAsync(bool forceRefresh = false);

        /// <summary>
        /// 加载验方模板列表
        /// </summary>
        /// <param name="forceRefresh">是否强制刷新缓存</param>
        Task<List<FormulaInfo>> LoadFormulasAsync(bool forceRefresh = false);

        /// <summary>
        /// 创建新的看诊记录
        /// </summary>
        /// <param name="patientId">患者ID</param>
        Task<ConsultationInfo?> CreateConsultationAsync(Guid patientId);

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        /// <param name="consultation">看诊信息</param>
        Task<bool> UpdateConsultationAsync(ConsultationInfo consultation);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void ClearAllCache();

        /// <summary>
        /// 清除特定类型的缓存
        /// </summary>
        /// <param name="cacheType">缓存类型（herbs/formulas/patients）</param>
        void ClearSpecificCache(string cacheType);

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        object GetCacheStatistics();
    }
}