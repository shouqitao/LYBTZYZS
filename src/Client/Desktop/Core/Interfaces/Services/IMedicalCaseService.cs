using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 医疗案例服务接口
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<MedicalCaseInfo>> GetPagedAsync(int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        /// <param name="id">医疗案例ID</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        /// <param name="createDto">创建DTO</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<MedicalCaseInfo>> CreateAsync(MedicalCaseCreateDto createDto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        /// <param name="editDto">编辑DTO</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<bool>> UpdateAsync(MedicalCaseEditDto editDto);

        /// <summary>
        /// 获取患者的医疗案例列表
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<List<MedicalCaseInfo>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<List<MedicalCaseInfo>>> GetTodayByUserIdAsync(Guid userId);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        /// <param name="id">医疗案例ID</param>
        /// <param name="status">新状态</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        /// <param name="id">医疗案例ID</param>
        /// <returns>服务结果</returns>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
    }
}