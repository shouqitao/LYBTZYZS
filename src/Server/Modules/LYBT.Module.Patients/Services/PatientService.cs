using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services {

    /// <summary>
    /// 患者服务 - UltraThink简化架构纯委托模式
    /// 职责：统一服务入口，纯粹的请求分发器
    /// </summary>
    public class PatientService(
        IPatientQueryService queryService,
        IPatientBusinessService businessService) : IPatientService {
        private readonly IPatientQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IPatientBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region Core Operations - 委托给BusinessService

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
            => await _businessService.CreateAsync(dto);

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
            => await _businessService.UpdateAsync(id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id) {
            var result = await _businessService.DeleteAsync(id);
            return result.IsSuccess
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除患者失败");
        }

        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _businessService.DeleteAsync(id);
            return result.IsSuccess;
        }

        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName) {
            var patientIds = new List<Guid> { id };
            var status = isActive ? "enabled" : "disabled";
            var result = await _businessService.SetStatusAsync(patientIds, status);
            return result.IsSuccess;
        }

        #endregion Core Operations - 委托给BusinessService

        #region Query Operations

        public async Task<List<PatientDto>> GetAllAsync() {
            var result = await _queryService.GetAllAsync();
            return result.IsSuccess ? (result.Data ?? new List<PatientDto>()) : new List<PatientDto>();
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query) {
            // 转换为基础分页查询DTO
            var baseQuery = new PagedQueryBaseDto {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Keyword = query.Keyword
            };
            return await _queryService.GetPagedAsync(baseQuery);
        }

        public async Task<List<PatientDto>> GetActivePatientsAsync() {
            var result = await _queryService.GetActivePatientsAsync();
            return result.IsSuccess ? (result.Data ?? new List<PatientDto>()) : new List<PatientDto>();
        }

        public async Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber) {
            var result = await _queryService.GetByPhoneNumberAsync(phoneNumber);
            return result.IsSuccess ? result.Data : null;
        }

        public async Task<PatientDto?> GetByIDNumberAsync(string idNumber) {
            var result = await _queryService.GetByIDNumberAsync(idNumber);
            return result.IsSuccess ? result.Data : null;
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<PagedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query) {
            // 转换为PatientSearchDto
            var searchDto = new PatientSearchDto {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Keyword = query.Keyword,
                Name = query.Name,
                PhoneNumber = query.PhoneNumber,
                Gender = query.Gender,
                MinAge = query.MinAge,
                MaxAge = query.MaxAge
            };

            var result = await _queryService.AdvancedSearchAsync(searchDto);
            return result.IsSuccess ? (result.Data ?? new PagedResult<PatientDto> {
                TotalCount = 0,
                Items = new List<PatientDto>(),
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            }) : new PagedResult<PatientDto> {
                TotalCount = 0,
                Items = new List<PatientDto>(),
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber) {
            var createDto = new PatientCreateDto {
                IdNumber = idNumber,
                PhoneNumber = phoneNumber,
                Name = "临时检查", // 必填字段
                BirthDate = DateTime.Today.AddYears(-30) // 必填字段
            };
            var result = await _queryService.CheckDuplicatePatientsAsync(createDto);
            return result.IsSuccess ? (result.Data ?? new List<PatientDto>()) : new List<PatientDto>();
        }

        #endregion Query Operations

        #region Business Operations

        public async Task<ServiceResult> EnableAsync(Guid id) {
            var patientIds = new List<Guid> { id };
            var result = await _businessService.EnableAsync(patientIds);
            return result.IsSuccess
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage ?? "启用患者失败");
        }

        public async Task<ServiceResult> DisableAsync(Guid id) {
            var patientIds = new List<Guid> { id };
            var result = await _businessService.DisableAsync(patientIds);
            return result.IsSuccess
                ? ServiceResult.Success()
                : ServiceResult.Failure(result.ErrorMessage ?? "禁用患者失败");
        }

        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients) {
            // 转换为PatientImportDto列表
            var importDtos = new List<PatientImportDto>();
            foreach (var patient in patients) {
                var importDto = new PatientImportDto {
                    Name = patient.Name,
                    GenderText = patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : "女",
                    BirthDateText = patient.BirthDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    PhoneNumber = patient.PhoneNumber,
                    IdCardNumber = patient.IdNumber,
                    Address = patient.Address,
                    EmergencyContact = patient.EmergencyContact,
                    EmergencyPhone = patient.EmergencyPhone,
                    AllergyHistory = patient.AllergyHistory
                };
                importDtos.Add(importDto);
            }

            var result = await _businessService.ImportPatientsAsync(importDtos);
            if (result.IsSuccess) {
                // 转换结果为通用对象
                return ServiceResult<object>.Success(new {
                    SuccessCount = result.Data?.Count ?? 0,
                    ImportedPatients = result.Data
                });
            }
            return ServiceResult<object>.Failure(result.ErrorMessage ?? "导入患者失败");
        }

        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query) {
            // 转换为PatientExportDto
            var exportDto = new PatientExportDto {
                Name = query.Keyword ?? string.Empty
            };

            var result = await _businessService.ExportPatientsAsync(exportDto);
            if (result.IsSuccess) {
                // 将PatientDto列表转换为CSV字节数组
                var patients = result.Data ?? new List<PatientDto>();
                var csvContent = "姓名,性别,出生日期,手机号码,身份证号,地址\n";

                foreach (var patient in patients) {
                    var gender = patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : "女";
                    csvContent += $"{patient.Name},{gender},{patient.BirthDate:yyyy-MM-dd}," +
                                 $"{patient.PhoneNumber},{patient.IdNumber},{patient.Address}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                return ServiceResult<byte[]>.Success(bytes);
            }
            return ServiceResult<byte[]>.Failure(result.ErrorMessage ?? "导出患者失败");
        }

        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto) {
            var result = await _businessService.ValidatePatientAsync(dto);
            if (result.IsSuccess) {
                var validationErrors = result.Data ?? new List<string>();
                return ServiceResult<object>.Success(new {
                    IsValid = !validationErrors.Any(),
                    Errors = validationErrors
                });
            }
            return ServiceResult<object>.Failure(result.ErrorMessage ?? "验证患者信息失败");
        }

        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync() {
            var result = await _businessService.GetImportTemplateAsync();
            if (result.IsSuccess) {
                // 转换模板对象为CSV字节数组
                var template = "姓名,性别,出生日期,手机号码,身份证号,地址,紧急联系人,紧急联系人电话,过敏史\n" +
                              "张三,男,1990-01-01,13800138001,110101199001011234,北京市朝阳区,李四,13800138002,无\n" +
                              "王五,女,1985-05-15,13800138003,110101198505151234,北京市海淀区,赵六,13800138004,青霉素\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(template);
                return ServiceResult<byte[]>.Success(bytes);
            }
            return ServiceResult<byte[]>.Failure(result.ErrorMessage ?? "获取导入模板失败");
        }

        #endregion Business Operations

        #region Shared Interface

        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
            => await _queryService.GetByIdCardAsync(idCard);

        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
            => await _queryService.GetByPhoneAsync(phone);

        #endregion Shared Interface
    }
}
