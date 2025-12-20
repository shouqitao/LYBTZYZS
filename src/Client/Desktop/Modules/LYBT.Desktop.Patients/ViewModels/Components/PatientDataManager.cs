using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者数据管理器 - 组件化架构
    /// 职责单一：专注患者数据的CRUD操作和状态管理
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// </summary>
    public class PatientDataManager
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientDataManager> _logger;

        public PatientDataManager(
            IPatientRepository patientRepository,
            ILogger<PatientDataManager> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心数据属性

        /// <summary>患者ID</summary>
        public Guid PatientId { get; private set; }

        /// <summary>当前患者数据</summary>
        public PatientDetailDto? CurrentPatient { get; private set; }

        /// <summary>是否为新患者</summary>
        public bool IsNewPatient { get; private set; } = true;

        /// <summary>是否正在加载</summary>
        public bool IsLoading { get; private set; }

        /// <summary>是否有未保存的变更</summary>
        public bool HasChanges { get; private set; }

        /// <summary>是否只读模式</summary>
        public bool IsReadOnly { get; set; } = true;

        #endregion 核心数据属性

        #region 数据初始化

        /// <summary>
        /// 初始化患者数据
        /// </summary>
        /// <param name="patientId">患者ID，如果为Empty则创建新患者</param>
        public async Task InitializeAsync(Guid patientId)
        {
            try
            {
                IsLoading = true;
                PatientId = patientId;

                _logger.LogInformation("开始初始化患者数据，患者ID: {PatientId}", patientId);

                if (patientId == Guid.Empty)
                {
                    // 新建患者模式
                    IsNewPatient = true;
                    CurrentPatient = null;
                    IsReadOnly = false;
                    _logger.LogInformation("初始化为新建患者模式");
                }
                else
                {
                    // 加载现有患者
                    await LoadExistingPatientAsync();
                }

                HasChanges = false;
                _logger.LogInformation("患者数据初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化患者数据失败");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 加载现有患者数据
        /// </summary>
        private async Task LoadExistingPatientAsync()
        {
            try
            {
                _logger.LogInformation("开始加载患者数据，患者ID: {PatientId}", PatientId);

                CurrentPatient = await _patientRepository.GetByIdAsync(PatientId);

                if (CurrentPatient != null)
                {
                    IsNewPatient = false;
                    _logger.LogInformation("成功加载患者数据: {PatientName}", CurrentPatient.Name);
                }
                else
                {
                    _logger.LogWarning("未找到患者数据，患者ID: {PatientId}", PatientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者数据失败");
                throw;
            }
        }

        #endregion 数据初始化

        #region 数据操作

        /// <summary>
        /// 保存患者数据
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            try
            {
                if (CurrentPatient == null)
                {
                    _logger.LogWarning("患者数据为空，无法保存");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("开始保存患者数据");

                // 转换为InputDto
                var inputDto = ConvertToInputDto(CurrentPatient);

                PatientDetailDto? savedPatient;
                if (IsNewPatient)
                {
                    savedPatient = await _patientRepository.CreateAsync(inputDto);
                    _logger.LogInformation("新建患者成功: {PatientName}", savedPatient.Name);
                }
                else
                {
                    savedPatient = await _patientRepository.UpdateAsync(inputDto);
                    _logger.LogInformation("更新患者成功: {PatientName}", savedPatient.Name);
                }

                if (savedPatient != null)
                {
                    CurrentPatient = savedPatient;
                    PatientId = savedPatient.Id;
                    IsNewPatient = false;
                    HasChanges = false;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存患者数据失败");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除患者数据
        /// </summary>
        public async Task<bool> DeleteAsync()
        {
            try
            {
                if (PatientId == Guid.Empty)
                {
                    _logger.LogWarning("患者ID为空，无法删除");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("开始删除患者，患者ID: {PatientId}", PatientId);

                var result = await _patientRepository.DeleteAsync(PatientId);

                if (result)
                {
                    _logger.LogInformation("删除患者成功");
                    CurrentPatient = null;
                    PatientId = Guid.Empty;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败");
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 重新加载患者数据
        /// </summary>
        public async Task ReloadAsync()
        {
            if (PatientId != Guid.Empty)
            {
                await LoadExistingPatientAsync();
                HasChanges = false;
            }
        }

        /// <summary>
        /// 标记数据已变更
        /// </summary>
        public void MarkAsChanged()
        {
            HasChanges = true;
        }

        #endregion 数据操作

        #region 辅助方法

        /// <summary>
        /// 转换为InputDto
        /// </summary>
        // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由服务端管理
        private PatientInputDto ConvertToInputDto(PatientDetailDto patient)
        {
            return new PatientInputDto
            {
                Id = IsNewPatient ? null : patient.Id,
                Name = patient.Name,
                Gender = patient.Gender,
                BirthDate = patient.BirthDate,
                // Issue #2240: Age不再是PatientInputDto的属性，仅BirthDate为输入
                IdNumber = patient.IdNumber,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                MaritalStatus = patient.MaritalStatus,
                IdType = patient.IdType,
                BloodType = patient.BloodType,
                AllergyHistory = patient.AllergyHistory,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelation = patient.EmergencyContactRelation
            };
        }

        #endregion 辅助方法
    }
}
