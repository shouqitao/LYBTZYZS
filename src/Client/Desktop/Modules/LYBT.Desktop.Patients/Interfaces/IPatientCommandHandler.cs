using System.Windows.Input;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IPatientCommandHandler
    {
        #region 事件定义

        /// <summary>
        /// 患者保存成功事件
        /// </summary>
        event Action? OnPatientSaved;

        /// <summary>
        /// 患者删除成功事件
        /// </summary>
        event Action? OnPatientDeleted;

        /// <summary>
        /// 患者编辑启用事件
        /// </summary>
        event Action? OnEditEnabled;

        /// <summary>
        /// 患者编辑取消事件
        /// </summary>
        event Action? OnEditCancelled;

        #endregion

        #region 命令定义

        /// <summary>
        /// 保存命令
        /// </summary>
        ICommand SaveCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        ICommand EditCommand { get; }

        /// <summary>
        /// 取消编辑命令
        /// </summary>
        ICommand CancelEditCommand { get; }

        /// <summary>
        /// 删除命令
        /// </summary>
        ICommand DeleteCommand { get; }

        /// <summary>
        /// 查看病历历史命令
        /// </summary>
        ICommand ViewMedicalHistoryCommand { get; }

        /// <summary>
        /// 返回命令
        /// </summary>
        ICommand BackCommand { get; }

        #endregion

        #region 依赖管理

        /// <summary>
        /// 设置依赖
        /// </summary>
        void SetDependencies(PatientDataManager dataManager);

        #endregion

        #region 患者CRUD操作

        /// <summary>
        /// 创建患者
        /// </summary>
        Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto);

        /// <summary>
        /// 更新患者
        /// </summary>
        Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto);

        /// <summary>
        /// 删除患者
        /// </summary>
        Task<CommandResult<bool>> DeletePatientAsync(Guid patientId);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除患者
        /// OpenSpec: optimize-batch-operations Phase 2 - 返回BatchOperationResultDto
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds);

        #endregion

        #region 查询操作

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<CommandResult<IEnumerable<PatientDetailDto>>> SearchPatientsAsync(string keyword);

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<CommandResult<PagedResult<PatientDetailDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者（Issue #1788: 支持单个患者查询）
        /// </summary>
        Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId);

        #endregion
    }
}
