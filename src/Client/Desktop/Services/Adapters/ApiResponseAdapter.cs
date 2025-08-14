using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Refit;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Services.Adapters
{
    /// <summary>
    /// API响应适配器 - 统一Refit.ApiResponse到ServiceResult的转换
    /// </summary>
    public static class ApiResponseAdapter
    {
        /// <summary>
        /// 将Refit.ApiResponse转换为ServiceResult
        /// </summary>
        public static ServiceResult<T> ToServiceResult<T>(Refit.ApiResponse<T> apiResponse)
        {
            if (apiResponse == null)
            {
                return ServiceResult<T>.Failure("API响应为空");
            }

            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                return ServiceResult<T>.Success(apiResponse.Content);
            }

            // 处理错误情况
            var errorMessage = GetErrorMessage(apiResponse);
            return ServiceResult<T>.Failure(errorMessage);
        }

        /// <summary>
        /// 将Refit.ApiResponse转换为ServiceResult（无返回数据）
        /// </summary>
        public static ServiceResult ToServiceResult(Refit.ApiResponse<object> apiResponse)
        {
            if (apiResponse == null)
            {
                return ServiceResult.Failure("API响应为空");
            }

            if (apiResponse.IsSuccessStatusCode)
            {
                return ServiceResult.Success();
            }

            var errorMessage = GetErrorMessage(apiResponse);
            return ServiceResult.Failure(errorMessage);
        }

        /// <summary>
        /// 获取错误消息
        /// </summary>
        private static string GetErrorMessage<T>(Refit.ApiResponse<T> apiResponse)
        {
            try
            {
                var statusCode = apiResponse.StatusCode;
                var reasonPhrase = apiResponse.ReasonPhrase ?? "未知错误";

                return statusCode switch
                {
                    HttpStatusCode.BadRequest => "请求参数错误",
                    HttpStatusCode.Unauthorized => "身份验证失败，请重新登录",
                    HttpStatusCode.Forbidden => "权限不足，无法访问该资源",
                    HttpStatusCode.NotFound => "请求的资源不存在",
                    HttpStatusCode.Conflict => "数据冲突，请刷新后重试",
                    HttpStatusCode.InternalServerError => "服务器内部错误",
                    HttpStatusCode.BadGateway => "网关错误，请稍后重试",
                    HttpStatusCode.ServiceUnavailable => "服务暂不可用，请稍后重试",
                    HttpStatusCode.GatewayTimeout => "请求超时，请稍后重试",
                    _ => $"请求失败: {reasonPhrase} (HTTP {(int)statusCode})"
                };
            }
            catch
            {
                return "请求处理失败";
            }
        }

        /// <summary>
        /// 将PatientDto转换为PatientDetailDto
        /// </summary>
        public static PatientDetailDto ToPatientDetailDto(PatientDto patientDto)
        {
            if (patientDto == null) return new PatientDetailDto();

            return new PatientDetailDto
            {
                Id = patientDto.Id,
                Name = patientDto.Name,
                Gender = patientDto.Gender,
                Age = patientDto.Age,
                PhoneNumber = patientDto.PhoneNumber,
                IDNumber = patientDto.IDNumber,
                Address = patientDto.Address,
                AllergyHistory = patientDto.AllergyHistory,
                PinYinCode = patientDto.PinYinCode,
                // 设置默认值（PatientDto中缺少的属性）
                DateOfBirth = DateTime.Now.AddYears(-patientDto.Age), // 根据年龄估算出生日期
                CreateTime = DateTime.Now, // 默认创建时间
                UpdateTime = DateTime.Now, // 默认更新时间
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled, // 默认启用状态
                Remark = null // 默认无备注
            };
        }

        /// <summary>
        /// 批量转换PatientDto列表为PatientDetailDto列表
        /// </summary>
        public static List<PatientDetailDto> ToPatientDetailDtos(IEnumerable<PatientDto> patientDtos)
        {
            return patientDtos?.Select(ToPatientDetailDto).ToList() ?? new List<PatientDetailDto>();
        }

        /// <summary>
        /// 将PatientDetailDto转换为PatientCreateDto
        /// </summary>
        public static PatientCreateDto ToPatientCreateDto(PatientDetailDto patientDetail)
        {
            if (patientDetail == null) return new PatientCreateDto();

            return new PatientCreateDto
            {
                Name = patientDetail.Name,
                Gender = patientDetail.Gender,
                Age = patientDetail.Age,
                DateOfBirth = patientDetail.DateOfBirth,
                PhoneNumber = patientDetail.PhoneNumber,
                IDNumber = patientDetail.IDNumber ?? string.Empty,
                Address = patientDetail.Address ?? string.Empty,
                AllergyHistory = patientDetail.AllergyHistory ?? "无已知过敏史",
                Remark = patientDetail.Remark
            };
        }

        /// <summary>
        /// 将PatientDetailDto转换为PatientUpdateDto
        /// </summary>
        public static PatientUpdateDto ToPatientUpdateDto(PatientDetailDto patientDetail)
        {
            if (patientDetail == null) return new PatientUpdateDto();

            return new PatientUpdateDto
            {
                Id = patientDetail.Id,
                Name = patientDetail.Name,
                Gender = patientDetail.Gender,
                Age = patientDetail.Age,
                DateOfBirth = patientDetail.DateOfBirth,
                PhoneNumber = patientDetail.PhoneNumber,
                IDNumber = patientDetail.IDNumber ?? string.Empty,
                Address = patientDetail.Address ?? string.Empty,
                AllergyHistory = patientDetail.AllergyHistory ?? string.Empty,
                Status = patientDetail.Status,
                Remark = patientDetail.Remark
            };
        }
    }
}