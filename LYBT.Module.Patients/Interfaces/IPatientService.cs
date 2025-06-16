using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;

namespace LYBT.Module.Patients.Interfaces {
    /// <summary>
    /// 病人服务接口，负责业务逻辑处理
    /// </summary>
    public interface IPatientService {
        /// <summary>
        /// 新增病人
        /// </summary>
        Task<bool> AddAsync(PatientCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 编辑病人
        /// </summary>
        Task<bool> UpdateAsync(PatientEditDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除单个病人
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据Id获取病人信息
        /// </summary>
        Task<PatientDetailDto> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取全部病人信息
        /// </summary>
        Task<List<PatientDto>> GetAllAsync();

        /// <summary>
        /// 分页条件查询
        /// </summary>
        Task<PagedResultDto<PatientDto>> GetPagedAsync(PatientPagedQueryDto query);

        /// <summary>
        /// 批量删除病人
        /// </summary>
        Task<int> BatchDeleteAsync(List<string> ids, Guid operatorId, string operatorName);
    }
}
