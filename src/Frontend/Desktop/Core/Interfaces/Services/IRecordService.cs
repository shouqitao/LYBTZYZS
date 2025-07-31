using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.DTOs;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 病历服务接口（对应后端IRecordService）
    /// </summary>
    public interface IRecordService
    {
        /// <summary>
        /// 获取病历列表
        /// </summary>
        Task<ApiResponse<List<RecordDto>>> GetListAsync();

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        Task<ApiResponse<List<RecordDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取病历详情
        /// </summary>
        Task<ApiResponse<RecordDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增病历
        /// </summary>
        Task<ApiResponse<object>> AddAsync(RecordCreateDto dto);

        /// <summary>
        /// 编辑病历
        /// </summary>
        Task<ApiResponse<object>> UpdateAsync(RecordEditDto dto);

        /// <summary>
        /// 删除病历
        /// </summary>
        Task<ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        Task<ApiResponse<object>> MarkAsSharedAsync(Guid id, List<string> doctorIds);

        /// <summary>
        /// 撤销病历共享
        /// </summary>
        Task<ApiResponse<object>> RevokeSharingAsync(Guid id);

        /// <summary>
        /// 获取共享给当前医生的病历
        /// </summary>
        Task<ApiResponse<List<RecordDto>>> GetSharedRecordsAsync(Guid doctorId);
    }
}