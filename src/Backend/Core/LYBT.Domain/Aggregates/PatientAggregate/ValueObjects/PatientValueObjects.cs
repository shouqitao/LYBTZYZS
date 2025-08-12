using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.PatientAggregate.ValueObjects
{
    /// <summary>
    /// 患者姓名值对象 - UltraThink重构DDD架构
    /// </summary>
    public class PatientName : SingleValueObject<string>
    {
        private static readonly Regex ValidPatientNameRegex = new(@"^[\u4e00-\u9fa5a-zA-Z\s\.\-]{1,50}$", RegexOptions.Compiled);

        public string PinYinCode { get; private set; }
        public string WuBiCode { get; private set; }

        private PatientName(string value) : base(value) 
        {
            // 自动生成拼音码和五笔码
            PinYinCode = GeneratePinYinCode(value);
            WuBiCode = GenerateWuBiCode(value);
        }

        public static PatientName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("患者姓名不能为空", nameof(value));

            value = value.Trim();

            if (!ValidPatientNameRegex.IsMatch(value))
                throw new ArgumentException($"患者姓名格式不正确: '{value}'", nameof(value));

            return new PatientName(value);
        }

        /// <summary>
        /// 检查姓名是否包含指定文本
        /// </summary>
        public bool Contains(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Value.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   PinYinCode.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                   WuBiCode.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        private static string GeneratePinYinCode(string name)
        {
            // 简化的拼音码生成逻辑（实际项目中可使用专门的拼音库）
            return name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 4)).ToUpper() : "";
        }

        private static string GenerateWuBiCode(string name)
        {
            // 简化的五笔码生成逻辑（实际项目中可使用专门的五笔库）
            return name.Length > 0 ? name.Substring(0, Math.Min(name.Length, 4)).ToUpper() : "";
        }
    }

    /// <summary>
    /// 性别枚举值对象
    /// </summary>
    public class Gender : Enumeration<Gender>
    {
        public static readonly Gender Male = new(1, nameof(Male), "男");
        public static readonly Gender Female = new(2, nameof(Female), "女");
        public static readonly Gender Unknown = new(0, nameof(Unknown), "未知");

        public string DisplayName { get; }

        private Gender(int value, string name, string displayName) : base(value, name)
        {
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 出生日期值对象
    /// </summary>
    public class DateOfBirth : SingleValueObject<DateTime>
    {
        private DateOfBirth(DateTime value) : base(value) { }

        public static DateOfBirth Create(DateTime value)
        {
            if (value > DateTime.Today)
                throw new ArgumentException("出生日期不能大于当前日期", nameof(value));

            if (value < DateTime.Today.AddYears(-150))
                throw new ArgumentException("出生日期不能早于150年前", nameof(value));

            return new DateOfBirth(value.Date); // 只保留日期部分
        }

        /// <summary>
        /// 计算当前年龄
        /// </summary>
        public int GetCurrentAge()
        {
            var today = DateTime.Today;
            var age = today.Year - Value.Year;
            
            if (Value.Date > today.AddYears(-age))
                age--;
                
            return Math.Max(0, age);
        }

        /// <summary>
        /// 计算指定日期时的年龄
        /// </summary>
        public int GetAgeAt(DateTime date)
        {
            var age = date.Year - Value.Year;
            
            if (Value.Date > date.AddYears(-age))
                age--;
                
            return Math.Max(0, age);
        }
    }

    /// <summary>
    /// 身份证号码值对象
    /// </summary>
    public class IdCardNumber : SingleValueObject<string>
    {
        // 身份证号码验证正则（18位或15位）
        private static readonly Regex ValidIdCardRegex = new(@"^(\d{15}|\d{17}[\dXx])$", RegexOptions.Compiled);
        
        // 身份证号码校验位
        private static readonly int[] WeightFactors = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        private static readonly char[] CheckCodes = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        private IdCardNumber(string value) : base(value) { }

        public static IdCardNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("身份证号码不能为空", nameof(value));

            value = value.Trim().ToUpper();

            if (!ValidIdCardRegex.IsMatch(value))
                throw new ArgumentException($"身份证号码格式不正确: '{value}'", nameof(value));

            if (value.Length == 18 && !IsValidChecksum(value))
                throw new ArgumentException($"身份证号码校验位不正确: '{value}'", nameof(value));

            return new IdCardNumber(value);
        }

        /// <summary>
        /// 验证18位身份证校验位
        /// </summary>
        private static bool IsValidChecksum(string idCard)
        {
            if (idCard.Length != 18) return true; // 15位身份证不验证校验位

            var sum = 0;
            for (int i = 0; i < 17; i++)
            {
                if (!char.IsDigit(idCard[i])) return false;
                sum += (idCard[i] - '0') * WeightFactors[i];
            }

            var checkIndex = sum % 11;
            var expectedCheckCode = CheckCodes[checkIndex];
            
            return idCard[17] == expectedCheckCode;
        }

        /// <summary>
        /// 从身份证号码中提取出生日期
        /// </summary>
        public DateTime? GetBirthDate()
        {
            if (Value.Length == 15)
            {
                var yearStr = "19" + Value.Substring(6, 2);
                var monthStr = Value.Substring(8, 2);
                var dayStr = Value.Substring(10, 2);
                
                if (DateTime.TryParse($"{yearStr}-{monthStr}-{dayStr}", out var date))
                    return date;
            }
            else if (Value.Length == 18)
            {
                var yearStr = Value.Substring(6, 4);
                var monthStr = Value.Substring(10, 2);
                var dayStr = Value.Substring(12, 2);
                
                if (DateTime.TryParse($"{yearStr}-{monthStr}-{dayStr}", out var date))
                    return date;
            }
            
            return null;
        }

        /// <summary>
        /// 从身份证号码中提取性别
        /// </summary>
        public Gender GetGender()
        {
            if (Value.Length == 15)
            {
                var genderDigit = int.Parse(Value.Substring(14, 1));
                return genderDigit % 2 == 0 ? Gender.Female : Gender.Male;
            }
            else if (Value.Length == 18)
            {
                var genderDigit = int.Parse(Value.Substring(16, 1));
                return genderDigit % 2 == 0 ? Gender.Female : Gender.Male;
            }
            
            return Gender.Unknown;
        }
    }

    /// <summary>
    /// 联系电话值对象
    /// </summary>
    public class ContactPhone : SingleValueObject<string>
    {
        private static readonly Regex ValidPhoneRegex = new(@"^(1[3-9]\d{9}|0\d{2,3}-?\d{7,8})$", RegexOptions.Compiled);

        private ContactPhone(string value) : base(value) { }

        public static ContactPhone Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("联系电话不能为空", nameof(value));

            value = value.Trim().Replace(" ", "").Replace("-", "");

            if (!ValidPhoneRegex.IsMatch(value))
                throw new ArgumentException($"联系电话格式不正确: '{value}'", nameof(value));

            return new ContactPhone(value);
        }
    }

    /// <summary>
    /// 紧急联系人值对象
    /// </summary>
    public class EmergencyContact : ValueObject
    {
        public string Name { get; }
        public string Phone { get; }
        public string Relationship { get; }

        private EmergencyContact(string name, string phone, string relationship)
        {
            Name = name;
            Phone = phone;
            Relationship = relationship;
        }

        public static EmergencyContact Create(string name, string phone, string relationship)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("紧急联系人姓名不能为空", nameof(name));

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("紧急联系人电话不能为空", nameof(phone));

            if (string.IsNullOrWhiteSpace(relationship))
                throw new ArgumentException("与紧急联系人关系不能为空", nameof(relationship));

            // 验证电话格式
            var validPhoneRegex = new Regex(@"^(1[3-9]\d{9}|0\d{2,3}-?\d{7,8})$");
            phone = phone.Trim().Replace(" ", "").Replace("-", "");
            
            if (!validPhoneRegex.IsMatch(phone))
                throw new ArgumentException($"紧急联系人电话格式不正确: '{phone}'", nameof(phone));

            return new EmergencyContact(name.Trim(), phone, relationship.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Phone;
            yield return Relationship;
        }
    }

    /// <summary>
    /// 住址值对象
    /// </summary>
    public class Address : SingleValueObject<string>
    {
        private Address(string value) : base(value) { }

        public static Address Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            if (value.Length > 200)
                throw new ArgumentException("住址长度不能超过200个字符", nameof(value));

            return new Address(value);
        }
    }

    /// <summary>
    /// 婚姻状况枚举值对象
    /// </summary>
    public class MaritalStatus : Enumeration<MaritalStatus>
    {
        public static readonly MaritalStatus Single = new(1, nameof(Single), "未婚");
        public static readonly MaritalStatus Married = new(2, nameof(Married), "已婚");
        public static readonly MaritalStatus Divorced = new(3, nameof(Divorced), "离异");
        public static readonly MaritalStatus Widowed = new(4, nameof(Widowed), "丧偶");
        public static readonly MaritalStatus Unknown = new(0, nameof(Unknown), "未知");

        public string DisplayName { get; }

        private MaritalStatus(int value, string name, string displayName) : base(value, name)
        {
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 过敏史值对象
    /// </summary>
    public class AllergyHistory : ValueObject
    {
        public IReadOnlyList<string> Allergies { get; }

        private AllergyHistory(List<string> allergies)
        {
            Allergies = allergies?.Where(a => !string.IsNullOrWhiteSpace(a))
                                  .Select(a => a.Trim())
                                  .ToList() ?? new List<string>();
        }

        public static AllergyHistory Create(List<string> allergies)
        {
            return new AllergyHistory(allergies);
        }

        public bool HasAllergies() => Allergies.Any();

        public bool IsAllergicTo(string substance)
        {
            if (string.IsNullOrWhiteSpace(substance)) return false;
            
            return Allergies.Any(allergy => 
                allergy.Contains(substance, StringComparison.OrdinalIgnoreCase));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var allergy in Allergies.OrderBy(a => a))
            {
                yield return allergy;
            }
        }

        public override string ToString()
        {
            return HasAllergies() ? string.Join(", ", Allergies) : "无过敏史";
        }
    }

    /// <summary>
    /// 既往病史值对象
    /// </summary>
    public class MedicalHistory : ValueObject
    {
        public IReadOnlyList<string> Histories { get; }

        private MedicalHistory(List<string> histories)
        {
            Histories = histories?.Where(h => !string.IsNullOrWhiteSpace(h))
                                 .Select(h => h.Trim())
                                 .ToList() ?? new List<string>();
        }

        public static MedicalHistory Create(List<string> histories)
        {
            return new MedicalHistory(histories);
        }

        public bool HasMedicalHistory() => Histories.Any();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var history in Histories.OrderBy(h => h))
            {
                yield return history;
            }
        }

        public override string ToString()
        {
            return HasMedicalHistory() ? string.Join(", ", Histories) : "无既往病史";
        }
    }

    /// <summary>
    /// 家族病史值对象
    /// </summary>
    public class FamilyHistory : ValueObject
    {
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Histories { get; }

        private FamilyHistory(Dictionary<string, List<string>> histories)
        {
            var processedHistories = new Dictionary<string, IReadOnlyList<string>>();
            
            if (histories != null)
            {
                foreach (var kvp in histories)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                    
                    var diseases = kvp.Value?.Where(d => !string.IsNullOrWhiteSpace(d))
                                            .Select(d => d.Trim())
                                            .ToList() ?? new List<string>();
                    
                    if (diseases.Any())
                    {
                        processedHistories[kvp.Key.Trim()] = diseases;
                    }
                }
            }
            
            Histories = processedHistories;
        }

        public static FamilyHistory Create(Dictionary<string, List<string>> histories)
        {
            return new FamilyHistory(histories);
        }

        public bool HasFamilyHistory() => Histories.Any();

        public bool HasFamilyHistoryOf(string disease)
        {
            if (string.IsNullOrWhiteSpace(disease)) return false;
            
            return Histories.Values.Any(diseases => 
                diseases.Any(d => d.Contains(disease, StringComparison.OrdinalIgnoreCase)));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var kvp in Histories.OrderBy(h => h.Key))
            {
                yield return kvp.Key;
                foreach (var disease in kvp.Value.OrderBy(d => d))
                {
                    yield return disease;
                }
            }
        }

        public override string ToString()
        {
            if (!HasFamilyHistory()) return "无家族病史";
            
            var summaries = Histories.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}");
            return string.Join("; ", summaries);
        }
    }

    /// <summary>
    /// 患者身份信息值对象 - Repository兼容性
    /// </summary>
    public class PatientIdentity : ValueObject
    {
        public string IdNumber { get; }
        public string IdType { get; }

        public PatientIdentity(string idNumber, string idType)
        {
            IdNumber = idNumber ?? "";
            IdType = idType ?? "";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return IdNumber;
            yield return IdType;
        }
    }

    /// <summary>
    /// 患者个人信息值对象 - Repository兼容性
    /// </summary>
    public class PatientPersonalInfo : ValueObject
    {
        public string PinYinCode { get; }
        public string WuBiCode { get; }
        public string Occupation { get; }
        public string MaritalStatus { get; }

        public PatientPersonalInfo(string pinYinCode, string wuBiCode, string occupation, string maritalStatus)
        {
            PinYinCode = pinYinCode ?? "";
            WuBiCode = wuBiCode ?? "";
            Occupation = occupation ?? "";
            MaritalStatus = maritalStatus ?? "";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return PinYinCode;
            yield return WuBiCode;
            yield return Occupation;
            yield return MaritalStatus;
        }
    }
}