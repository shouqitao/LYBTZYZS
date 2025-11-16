using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IPatientDataManager
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        Guid PatientId { get; }

        /// <summary>
        /// 当前患者数据
        /// </summary>
        PatientDto? CurrentPatient { get; }

        /// <summary>
        /// 是否为新患者
        /// </summary>
        bool IsNewPatient { get; }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// 是否只读模式
        /// </summary>
        bool IsReadOnly { get; set; }

        /// <summary>
        /// 初始化数据（新建或编辑）
        /// </summary>
        Task InitializeAsync(Guid patientId);

        /// <summary>
        /// 保存患者数据（新建或更新）
        /// </summary>
        Task<bool> SaveAsync();

        /// <summary>
        /// 删除当前患者
        /// </summary>
        Task<bool> DeleteAsync();

        /// <summary>
        /// 重新加载患者数据
        /// </summary>
        Task ReloadAsync();

        /// <summary>
        /// 标记数据已更改
        /// </summary>
        void MarkAsChanged();
    }
}
