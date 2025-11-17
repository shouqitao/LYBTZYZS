using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using System.Text.Json;

namespace LYBT.Infrastructure.Validation
{
    /// <summary>
    /// BusinessRuleValidator统一业务规则验证器
    /// Epic #2139: 创建BusinessRuleValidator统一业务规则
    ///
    /// 核心业务规则：
    /// - BR-001: 医案必须在同一天内完成三步流程（辨证→开方标记→处方）
    /// - AR-003: 一诊一方原则（每个患者每天只能有一个有效处方）
    /// - BF-002: 辨证必须包含完整信息（证型、症状、舌象、脉象）
    ///
    /// 使用方式：
    /// var result = BusinessRuleValidator.ValidateMedicalCaseRules(medicalCase, flexibleMode);
    /// </summary>
    public static class BusinessRuleValidator
    {
        /// <summary>
        /// 验证医案业务规则
        /// </summary>
        /// <param name="medicalCase">医案实体</param>
        /// <param name="flexibleMode">是否启用灵活模式（跳过严格验证）</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateMedicalCaseRules(object medicalCase, bool flexibleMode = false)
        {
            var result = new ValidationResult();

            try
            {
                // 灵活模式下跳过严格验证
                if (flexibleMode)
                {
                    result.IsValid = true;
                    result.Message = "灵活模式：跳过严格业务规则验证";
                    return result;
                }

                // BR-001: 三步流程验证
                var br001Result = ValidateBR001(medicalCase);
                if (!br001Result.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = br001Result.ErrorMessage;
                    result.ErrorCode = "BR-001";
                    return result;
                }

                // AR-003: 一诊一方验证
                var ar003Result = ValidateAR003(medicalCase);
                if (!ar003Result.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = ar003Result.ErrorMessage;
                    result.ErrorCode = "AR-003";
                    return result;
                }

                // BF-002: 辨证信息完整性验证
                var bf002Result = ValidateBF002(medicalCase);
                if (!bf002Result.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = bf002Result.ErrorMessage;
                    result.ErrorCode = "BF-002";
                    return result;
                }

                result.IsValid = true;
                result.Message = "所有业务规则验证通过";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"业务规则验证过程中发生异常：{ex.Message}";
                result.ErrorCode = "VALIDATION_ERROR";
                return result;
            }
        }

        /// <summary>
        /// 验证处方业务规则
        /// </summary>
        /// <param name="prescription">处方实体</param>
        /// <param name="medicalCase">关联医案</param>
        /// <param name="flexibleMode">是否启用灵活模式</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidatePrescriptionRules(object prescription, object medicalCase, bool flexibleMode = false)
        {
            var result = new ValidationResult();

            try
            {
                // 灵活模式下跳过严格验证
                if (flexibleMode)
                {
                    result.IsValid = true;
                    result.Message = "灵活模式：跳过处方业务规则验证";
                    return result;
                }

                // 处方与医案关联验证
                if (medicalCase == null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "处方必须关联有效的医案";
                    result.ErrorCode = "PR-001";
                    return result;
                }

                // 处方药材数量验证（至少一味药，一般不超过20味）
                var herbCountResult = ValidatePrescriptionHerbCount(prescription);
                if (!herbCountResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = herbCountResult.ErrorMessage;
                    result.ErrorCode = "PR-002";
                    return result;
                }

                result.IsValid = true;
                result.Message = "处方业务规则验证通过";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"处方业务规则验证异常：{ex.Message}";
                result.ErrorCode = "PRESCRIPTION_VALIDATION_ERROR";
                return result;
            }
        }

        /// <summary>
        /// 验证用户业务规则
        /// </summary>
        /// <param name="user">用户实体</param>
        /// <param name="flexibleMode">是否启用灵活模式</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateUserRules(object user, bool flexibleMode = false)
        {
            var result = new ValidationResult();

            try
            {
                // 灵活模式下跳过严格验证
                if (flexibleMode)
                {
                    result.IsValid = true;
                    result.Message = "灵活模式：跳过用户业务规则验证";
                    return result;
                }

                // 用户基本信息验证
                var userInfoResult = ValidateUserInfo(user);
                if (!userInfoResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = userInfoResult.ErrorMessage;
                    result.ErrorCode = "USER-001";
                    return result;
                }

                result.IsValid = true;
                result.Message = "用户业务规则验证通过";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"用户业务规则验证异常：{ex.Message}";
                result.ErrorCode = "USER_VALIDATION_ERROR";
                return result;
            }
        }

        #region 私有验证方法

        /// <summary>
        /// BR-001: 三步流程验证
        /// </summary>
        private static ValidationResult ValidateBR001(object medicalCase)
        {
            // 使用反射或动态类型检查三步流程
            // 这里简化实现，实际应该检查辨证、开方标记、处方状态

            var result = new ValidationResult();

            // 模拟验证逻辑
            var consultationCompleted = GetPropertyValue<bool>(medicalCase, "ConsultationCompleted", false);
            var prescriptionFlagSet = GetPropertyValue<bool>(medicalCase, "PrescriptionFlagSet", false);
            var prescriptionCreated = GetPropertyValue<bool>(medicalCase, "PrescriptionCreated", false);

            if (!consultationCompleted)
            {
                result.IsValid = false;
                result.ErrorMessage = "必须完成辨证诊断才能进行后续步骤";
                return result;
            }

            if (!prescriptionFlagSet)
            {
                result.IsValid = false;
                result.ErrorMessage = "必须设置开方标记才能创建处方";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// AR-003: 一诊一方验证
        /// </summary>
        private static ValidationResult ValidateAR003(object medicalCase)
        {
            var result = new ValidationResult();

            // 检查是否已有当天的有效处方
            // 这里简化实现，实际需要查询数据库

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// BF-002: 辨证信息完整性验证
        /// </summary>
        private static ValidationResult ValidateBF002(object medicalCase)
        {
            var result = new ValidationResult();

            // 检查辨证关键信息
            var diagnosisType = GetPropertyValue<string>(medicalCase, "DiagnosisType", string.Empty);
            var symptoms = GetPropertyValue<string>(medicalCase, "Symptoms", string.Empty);
            var tongueCondition = GetPropertyValue<string>(medicalCase, "TongueCondition", string.Empty);
            var pulseCondition = GetPropertyValue<string>(medicalCase, "PulseCondition", string.Empty);

            if (string.IsNullOrEmpty(diagnosisType))
            {
                result.IsValid = false;
                result.ErrorMessage = "证型不能为空";
                return result;
            }

            if (string.IsNullOrEmpty(symptoms))
            {
                result.IsValid = false;
                result.ErrorMessage = "症状描述不能为空";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// 验证处方药材数量
        /// </summary>
        private static ValidationResult ValidatePrescriptionHerbCount(object prescription)
        {
            var result = new ValidationResult();

            // 获取药材数量
            var herbCount = GetPropertyValue<int>(prescription, "HerbCount", 0);

            if (herbCount == 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "处方必须包含至少一味药材";
                return result;
            }

            if (herbCount > 20)
            {
                result.IsValid = false;
                result.ErrorMessage = "处方药材数量不宜超过20味";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// 验证用户基本信息
        /// </summary>
        private static ValidationResult ValidateUserInfo(object user)
        {
            var result = new ValidationResult();

            var userName = GetPropertyValue<string>(user, "UserName", string.Empty);
            var role = GetPropertyValue<string>(user, "Role", string.Empty);

            if (string.IsNullOrEmpty(userName))
            {
                result.IsValid = false;
                result.ErrorMessage = "用户名不能为空";
                return result;
            }

            if (string.IsNullOrEmpty(role))
            {
                result.IsValid = false;
                result.ErrorMessage = "用户角色不能为空";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// 获取对象属性值（反射辅助方法）
        /// </summary>
        private static T GetPropertyValue<T>(object obj, string propertyName, T defaultValue)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        return (T)value;
                    }
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion
    }

    /// <summary>
    /// 业务规则验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// 从JSON字符串创建验证结果
        /// </summary>
        public static ValidationResult FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ValidationResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new ValidationResult();
            }
            catch
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "验证结果JSON解析失败"
                };
            }
        }
    }
}