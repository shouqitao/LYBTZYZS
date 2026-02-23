namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 批量检查患者引用请求DTO
    /// X7: 引用检查强制执行
    /// </summary>
    public class PatientBatchCheckReferenceInputDto
    {
        /// <summary>患者ID列表</summary>
        public List<Guid> PatientIds { get; set; } = new();
    }
}
