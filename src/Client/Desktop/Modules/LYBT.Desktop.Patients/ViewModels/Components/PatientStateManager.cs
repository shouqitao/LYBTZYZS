using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者数据管理器 - 组件化架构
    /// 职责单一：专注患者数据的CRUD操作和状态管理
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// OpenSpec: enhance-dataflow-logging - LOG-018 统一[STATE]前缀
    /// </summary>
    public class PatientStateManager
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientStateManager> _logger;

        public PatientStateManager(
            IPatientRepository patientRepository,
            ILogger<PatientStateManager> logger)
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

                _logger.LogInformation("[STATE] Patient.Initialize started - PatientId={PatientId}", patientId);

                if (patientId == Guid.Empty)
                {
                    // 新建患者模式
                    IsNewPatient = true;
                    CurrentPatient = null;
                    IsReadOnly = false;
                    _logger.LogDebug("[STATE] Patient.Initialize → NewPatientMode");
                }
                else
                {
                    // 加载现有患者
                    await LoadExistingPatientAsync();
                }

                HasChanges = false;
                _logger.LogInformation("[STATE] Patient.Initialize completed - PatientId={PatientId}", patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STATE] Patient.Initialize failed - PatientId={PatientId}", patientId);
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
                _logger.LogDebug("[STATE] Patient.LoadExisting started - PatientId={PatientId}", PatientId);

                CurrentPatient = await _patientRepository.GetByIdAsync(PatientId);

                if (CurrentPatient != null)
                {
                    IsNewPatient = false;
                    _logger.LogDebug("[STATE] Patient.LoadExisting completed - PatientName={PatientName}", CurrentPatient.Name);
                }
                else
                {
                    _logger.LogWarning("[STATE] Patient.LoadExisting → NotFound - PatientId={PatientId}", PatientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STATE] Patient.LoadExisting failed - PatientId={PatientId}", PatientId);
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
                    _logger.LogWarning("[STATE] Patient.Save → NoData");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("[STATE] Patient.Save started - IsNew={IsNew}", IsNewPatient);

                // 转换为InputDto
                var inputDto = ConvertToInputDto(CurrentPatient);

                PatientDetailDto? savedPatient;
                if (IsNewPatient)
                {
                    savedPatient = await _patientRepository.CreateAsync(inputDto);
                    _logger.LogInformation("[STATE] Patient.Create completed - PatientId={PatientId} Name={PatientName}", savedPatient.Id, savedPatient.Name);
                }
                else
                {
                    savedPatient = await _patientRepository.UpdateAsync(inputDto);
                    _logger.LogInformation("[STATE] Patient.Update completed - PatientId={PatientId} Name={PatientName}", savedPatient.Id, savedPatient.Name);
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
                _logger.LogError(ex, "[STATE] Patient.Save failed - PatientId={PatientId}", PatientId);
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
                    _logger.LogWarning("[STATE] Patient.Delete → InvalidId");
                    return false;
                }

                IsLoading = true;
                _logger.LogInformation("[STATE] Patient.Delete started - PatientId={PatientId}", PatientId);

                var result = await _patientRepository.DeleteAsync(PatientId);

                if (result)
                {
                    _logger.LogInformation("[STATE] Patient.Delete completed - PatientId={PatientId}", PatientId);
                    CurrentPatient = null;
                    PatientId = Guid.Empty;
                }
                else
                {
                    _logger.LogWarning("[STATE] Patient.Delete → Failed - PatientId={PatientId}", PatientId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STATE] Patient.Delete failed - PatientId={PatientId}", PatientId);
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
                _logger.LogDebug("[STATE] Patient.Reload started - PatientId={PatientId}", PatientId);
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
