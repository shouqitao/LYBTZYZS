using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - UltraThink三层架构（纯委托模式）
    /// 职责：纯粹的服务委托，将请求分发到对应的专业服务层
    /// 三层架构：Core(CRUD) + Query(查询) + Business(业务逻辑)
    /// </summary>
    public class PatientService : BaseService<Patient, PatientDto, PatientCreateDto, PatientUpdateDto>, IPatientService
    {
        private readonly PatientServiceCore _coreService;
        private readonly PatientQueryService _queryService;
        private readonly PatientBusinessService _businessService;

        protected override string EntityName => "患者";

        public PatientService(
            PatientServiceCore coreService,
            PatientQueryService queryService,
            PatientBusinessService businessService,
            IMapper mapper,
            ILogger<PatientService> logger)
            : base(null!, mapper, logger) // BaseService需要context，但我们使用委托模式所以传null
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Core CRUD Operations (委托给CoreService)

        /// <summary>
        /// 根据患者ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            return await _coreService.GetByIdAsync(id);
        }

        /// <summary>
        /// 新增患者档案
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            return await _coreService.CreateAsync(dto);
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            return await _coreService.UpdateAsync(id, dto);
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _coreService.DeleteAsync(id);
        }

        /// <summary>
        /// 删除患者（带操作者信息）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var result = await _coreService.DeleteAsync(id);
            return result.IsSuccess && result.Data;
        }

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            var result = await _coreService.UpdateStatusAsync(id, isActive);
            return result.IsSuccess && result.Data;
        }

        #endregion

        #region Query Operations (委托给QueryService)

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<List<PatientDto>> GetAllAsync()
        {
            var result = await _queryService.GetAllActiveAsync();
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<List<PatientDto>> GetActivePatientsAsync()
        {
            var result = await _queryService.GetAllActiveAsync();
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var result = await _queryService.GetByPhoneNumberAsync(phoneNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDto?> GetByIDNumberAsync(string idNumber)
        {
            var result = await _queryService.GetByIdNumberAsync(idNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        public async Task<PagedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            var result = await _queryService.AdvancedSearchAsync(query);
            return result.IsSuccess ? result.Data : new PagedResult<PatientDto>
            {
                TotalCount = 0,
                Items = new List<PatientDto>(),
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 搜索患者（重构为Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            return await _queryService.SearchAsync(keyword);
        }

        /// <summary>
        /// 检查重复患者
        /// </summary>
        public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            var result = await _queryService.CheckDuplicatePatientsAsync(idNumber, phoneNumber);
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        #endregion

        #region Business Operations (委托给BusinessService)

        /// <summary>
        /// 启用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            var result = await _businessService.EnableAsync(id);
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "启用患者失败");
        }

        /// <summary>
        /// 禁用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            var result = await _businessService.DisableAsync(id);
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "禁用患者失败");
        }

        /// <summary>
        /// 批量导入患者
        /// </summary>
        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
        {
            return await _businessService.ImportPatientsAsync(patients);
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
        {
            return await _businessService.ExportPatientsAsync(query);
        }

        /// <summary>
        /// 验证患者信息
        /// </summary>
        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
        {
            return await _businessService.ValidatePatientAsync(dto);
        }

        /// <summary>
        /// 获取导入模板
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            await Task.CompletedTask; // 保持异步接口一致性
            return _businessService.GenerateImportTemplate();
        }

        #endregion

        #region Shared Interface Implementation

        /// <summary>
        /// 根据身份证号查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
        {
            return await _queryService.GetByIdNumberAsync(idCard);
        }

        /// <summary>
        /// 根据电话号码查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        {
            var result = await _queryService.GetByPhoneNumberAsync(phone);
            if (result.IsSuccess && result.Data != null)
            {
                return ServiceResult<List<PatientDto>>.Success(new List<PatientDto> { result.Data });
            }
            return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
        }

        #endregion

        #region Legacy Methods (简化实现 - UltraThink v2.0)

        /// <summary>
        /// 批量导入患者数据 - 基础数据功能
        /// </summary>
        public async Task<ServiceResult<int>> ImportPatientsAsync(List<PatientImportDto> patients)
        {
            try
            {
                if (patients == null || !patients.Any())
                    return ServiceResult<int>.Success(0);

                // 转换为PatientCreateDto
                var createDtos = patients.Select(p => new PatientCreateDto
                {
                    Name = p.Name,
                    Gender = ParseGender(p.GenderText), // 解析性别文本
                    BirthDate = ParseBirthDate(p.BirthDateText), // 解析出生日期文本
                    PhoneNumber = p.PhoneNumber,
                    IdNumber = p.IdNumber,
                    Address = p.Address,
                    EmergencyContact = p.EmergencyContact,
                    EmergencyPhone = p.EmergencyPhone
                    // PatientCreateDto没有Remark字段，所以跳过
                }).ToList();

                var result = await _businessService.ImportPatientsAsync(createDtos);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // 假设返回的Data包含SuccessCount属性
                    return ServiceResult<int>.Success(((dynamic)result.Data).SuccessCount);
                }
                
                return ServiceResult<int>.Failure(result.ErrorMessage ?? "导入失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者失败");
                return ServiceResult<int>.Failure("批量导入患者失败");
            }
        }

        /// <summary>
        /// 导出患者数据 - 基础数据功能
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync()
        {
            try
            {
                // 使用默认查询参数导出所有患者
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = int.MaxValue
                };
                
                var result = await _queryService.GetPagedAsync(new PatientPagedQueryDto 
                { 
                    PageIndex = query.PageIndex, 
                    PageSize = query.PageSize 
                });
                
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<PatientDto>>.Success(result.Data.Items);
                }
                
                return ServiceResult<List<PatientDto>>.Failure(result.ErrorMessage ?? "导出失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<List<PatientDto>>.Failure("导出患者数据失败");
            }
        }

        /// <summary>
        /// 获取统计信息 (已废弃 - UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        {
            try
            {
                await Task.CompletedTask;
                var emptyStats = new PatientStatisticsDto(); // 返回空的统计对象
                return ServiceResult<PatientStatisticsDto>.Success(emptyStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者统计失败");
                return ServiceResult<PatientStatisticsDto>.Failure("获取患者统计失败");
            }
        }

        /// <summary>
        /// 获取患者档案概览 (已废弃 - UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id)
        {
            try
            {
                await Task.CompletedTask;
                var emptyArchive = new { Message = "档案管理功能已废弃 - UltraThink精简", PatientId = id };
                return ServiceResult<object>.Success(emptyArchive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者档案失败: {Id}", id);
                return ServiceResult<object>.Failure("获取患者档案失败");
            }
        }

        /// <summary>
        /// 更新患者档案 (已废弃 - UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto)
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<bool>.Success(false); // 功能已废弃
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者档案失败: {Id}", id);
                return ServiceResult<bool>.Failure("更新患者档案失败");
            }
        }

        /// <summary>
        /// 获取年龄统计 (已废弃 - UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetAgeStatisticsAsync()
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<List<object>>.Success(new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取年龄统计失败");
                return ServiceResult<List<object>>.Failure("获取年龄统计失败", ex);
            }
        }

        #endregion

        #region BaseService Implementation

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected override object GetEntityId(Patient entity)
        {
            return entity.Id;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 解析性别文本
        /// </summary>
        private Gender ParseGender(string genderText)
        {
            return genderText?.Trim() switch
            {
                "男" => Gender.Male,
                "女" => Gender.Female,
                "Male" => Gender.Male,
                "Female" => Gender.Female,
                _ => Gender.Unknown
            };
        }

        /// <summary>
        /// 解析出生日期文本
        /// </summary>
        private DateTime? ParseBirthDate(string? birthDateText)
        {
            if (string.IsNullOrWhiteSpace(birthDateText))
                return null;

            if (DateTime.TryParse(birthDateText, out DateTime result))
                return result;

            return null;
        }

        #endregion
    }
}