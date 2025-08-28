using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services.Business
{
    /// <summary>
    /// 患者特殊业务服务接口
    /// UltraThink重构：专注于高级业务功能，如合并重复患者、就诊历史等
    /// </summary>
    public interface IPatientBusinessService
    {
        /// <summary>
        /// 合并重复患者
        /// </summary>
        /// <param name="primaryId">主患者ID（保留）</param>        /// <param name="duplicateId">重复患者ID（删除）</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>合并结果</returns>
        Task<ServiceResult<bool>> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>        /// <param name="patientId">患者ID</param>        /// <returns>就诊历史</returns>
        Task<ServiceResult<PatientVisitHistoryDto>> GetPatientVisitHistoryAsync(Guid patientId);

        /// <summary>
        /// 执行安全的患者操作（带事务和异常处理）
        /// </summary>        /// <typeparam name="T">返回类型</typeparam>        /// <param name="operation">操作函数</param>        /// <param name="operationName">操作名称</param>        /// <param name="logData">日志数据</param>        /// <returns>操作结果</returns>
        Task<ServiceResult<T>> ExecuteSafePatientOperationAsync<T>(
            Func<Task<ServiceResult<T>>> operation,
            string operationName,
            object? logData = null);

        /// <summary>
        /// 生成患者拼音码
        /// </summary>        /// <param name="name">患者姓名</param>        /// <returns>拼音码</returns>
        ServiceResult<string> GeneratePatientPinYinCode(string name);

        /// <summary>
        /// 记录患者操作日志
        /// </summary>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <param name="actionType">操作类型</param>        /// <param name="content">操作内容</param>        /// <param name="parameters">操作参数</param>
        /// <returns>记录任务</returns>
        Task LogPatientOperationAsync(Guid operatorId, string operatorName, string actionType, string content, string? parameters = null);
    }
}
