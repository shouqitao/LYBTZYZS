using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Dtos
{
    /// <summary>
    /// MedicalCase统一更新请求模型
    /// Epic #1612: MedicalCase模块权限优化 - Phase 2 Task 2.3
    /// 合并6个分散的更新方法为统一的更新接口
    /// </summary>
    public class UpdateMedicalCaseRequest
    {
        #region 基本属性

        /// <summary>
        /// 病案状态
        /// </summary>
        public MedicalCaseStatus? Status { get; set; }

        /// <summary>
        /// 是否需要处方（三步流程Step 2）
        /// </summary>
        public bool? NeedsPrescription { get; set; }

        #endregion

        #region 辨证信息（Step 1）

        /// <summary>
        /// 辨证信息更新（三步流程Step 1）
        /// 如果提供，则更新Consultation实体
        /// </summary>
        public ConsultationInputDto? Consultation { get; set; }

        #endregion

        #region 处方操作（Step 3）

        /// <summary>
        /// 创建处方请求（三步流程Step 3a）
        /// </summary>
        public PrescriptionCreateDto? CreatePrescription { get; set; }

        /// <summary>
        /// 更新处方请求（三步流程Step 3b）
        /// </summary>
        public PrescriptionUpdateRequest? UpdatePrescription { get; set; }

        /// <summary>
        /// 删除处方请求
        /// </summary>
        public DeletePrescriptionRequest? DeletePrescription { get; set; }

        /// <summary>
        /// 完成病案请求（三步流程完成）
        /// </summary>
        public CompleteCaseRequest? CompleteCase { get; set; }

        #endregion

        #region 操作模式选项

        /// <summary>
        /// 更新模式
        /// </summary>
        public UpdateMode Mode { get; set; } = UpdateMode.UpdateAll;

        /// <summary>
        /// 是否跳过业务规则验证（仅管理员可用）
        /// </summary>
        public bool SkipBusinessRules { get; set; } = false;

        /// <summary>
        /// 是否强制执行（覆盖状态检查）
        /// </summary>
        public bool Force { get; set; } = false;

        #endregion
    }

    /// <summary>
    /// 更新模式枚举
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// 更新所有提供的字段
        /// </summary>
        UpdateAll,

        /// <summary>
        /// 仅更新提供的字段，其他保持不变
        /// </summary>
        UpdateOnly,

        /// <summary>
        /// 仅验证，不执行更新
        /// </summary>
        ValidateOnly,

        /// <summary>
        /// 事务模式：要么全部成功，要么全部回滚
        /// </summary>
        Transactional
    }

    /// <summary>
    /// 处方更新请求
    /// </summary>
    public class PrescriptionUpdateRequest
    {
        public Guid PrescriptionId { get; set; }
        public PrescriptionEditDto PrescriptionData { get; set; } = new();
    }

    /// <summary>
    /// 删除处方请求
    /// </summary>
    public class DeletePrescriptionRequest
    {
        public Guid PrescriptionId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完成病案请求
    /// </summary>
    public class CompleteCaseRequest
    {
        public bool SkipThreeStepValidation { get; set; } = false;
        public string CompletionNote { get; set; } = string.Empty;
    }
}