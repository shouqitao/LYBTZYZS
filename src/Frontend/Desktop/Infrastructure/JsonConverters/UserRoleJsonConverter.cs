using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Infrastructure.JsonConverters
{
    /// <summary>
    /// UserRole 枚举的 JSON 转换器
    /// 支持字符串和数字两种格式
    /// </summary>
    public class UserRoleJsonConverter : JsonConverter<UserRole>
    {
        public override UserRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (Enum.TryParse<UserRole>(stringValue, true, out var enumValue))
                {
                    return enumValue;
                }
                
                // 处理可能的字符串映射
                return stringValue?.ToLower() switch
                {
                    "staff" => UserRole.RegistrationStaff,
                    "registrationstaff" => UserRole.RegistrationStaff,
                    "diagnosingdoctor" => UserRole.DiagnosingDoctor,
                    "cashierstaff" => UserRole.CashierStaff,
                    "pharmacystaff" => UserRole.PharmacyStaff,
                    "physiotherapystaff" => UserRole.PhysiotherapyStaff,
                    "admin" => UserRole.Admin,
                    _ => UserRole.RegistrationStaff
                };
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return (UserRole)reader.GetInt32();
            }
            
            throw new JsonException($"Unable to convert \"{reader.GetString()}\" to UserRole");
        }

        public override void Write(Utf8JsonWriter writer, UserRole value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}