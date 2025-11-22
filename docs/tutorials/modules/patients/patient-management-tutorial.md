# Patients模块管理教程 (Patient Management Tutorial)

> **学习导向**: 手把手掌握LYBTZYZS患者管理系统的使用和开发
> **适合人群**: 医生、护士、诊所管理人员、开发者
> **学习时间**: 75分钟
> **难度级别**: 中高级

## 🎯 学习目标

完成本教程后，您将能够：
- 理解LYBTZYZS患者管理系统的数据模型和业务流程
- 掌握患者档案的完整生命周期管理（注册、更新、归档）
- 学会病史记录管理和隐私保护机制
- 了解拼音码搜索和患者数据导入导出功能
- 能够在开发环境中实现患者管理功能

## 📋 前置条件

### 技术要求
- 完成Auth模块和Users模块基础教程
- 了解医疗行业数据隐私保护规范
- 熟悉Entity Framework Core数据操作
- 具备基础的WPF界面开发知识

### 环境准备
- LYBTZYZS开发环境已配置完成
- 数据库已初始化并包含基础患者数据
- 具备医生或护士权限的测试账户

### 权限要求
- 医生(Doctor)或护士(Nurse)角色
- 具备患者管理权限的用户账户

## 🔍 核心概念理解

### 患者数据模型

#### 患者实体结构
```csharp
public class Patient
{
    public Guid Id { get; set; }                    // 唯一标识符
    public string Name { get; set; }                   // 患者姓名
    public string PinYinCode { get; set; }             // 拼音码搜索码
    public string IdNumber { get; set; }                // 身份证号码
    public IdType IdType { get; set; }                    // 证件类型
    public Gender Gender { get; set; }                  // 性别
    public DateTime? BirthDate { get; set; }            // 出生日期
    public int? Age { get; set; }                        // 年龄（自动计算）
    public string PhoneNumber { get; set; }              // 手机号码
    public string Address { get; set; }                  // 详细地址
    public MaritalStatus MaritalStatus { get; set; }    // 婚姻状况
    public BloodType BloodType { get; set; }            // 血型
    public string MedicalHistory { get; set; }           // 病史摘要
    public string AllergyHistory { get; set; }           // 过敏史
    public string EmergencyContactName { get; set; }    // 紧急联系人姓名
    public string EmergencyContactPhone { get; set; }   // 紧急联系人电话
    public string EmergencyContactRelation { get; set; } // 紧急联系人关系
    public PatientStatus Status { get; set; }           // 患者状态
    public string DisableReason { get; set; }           // 禁用原因
    public DateTime? LastVisitTime { get; set; }         // 最后就诊时间
    public int VisitCount { get; set; }                 // 就诊次数
    public string Remark { get; set; }                  // 备注信息
}
```

### 患者状态管理
```csharp
public enum PatientStatus
{
    Active = 1,        // 正常状态，可以就诊
    Disabled = 0,      // 已禁用，无法就诊
    Deceased = 2,       // 已故，已归档
    Transferred = 3     // 转院，已转出
}
```

### 数据隐私保护机制

#### 敏感信息脱敏
```csharp
public class PatientDataMasking
{
    public PatientDto MaskSensitiveData(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            PinYinCode = patient.PinYinCode,
            // 脱敏身份证号：显示前3位和后4位
            IdNumber = MaskIdNumber(patient.IdNumber),
            // 脱敏手机号：显示前3位和后4位
            PhoneNumber = MaskPhoneNumber(patient.PhoneNumber),
            // 其他信息...
        };
    }

    private string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 7)
            return "****";
        return $"{idNumber.Substring(0, 3)}****{idNumber.Substring(idNumber.Length - 4)}";
    }
}
```

## 📝 模块一：患者档案管理

### 1.1 新建患者档案

#### 业务场景
诊所来了一位新患者，需要为其建立完整的电子档案。

#### 步骤1: 准备患者基本信息
```json
{
  "name": "张三",
  "idNumber": "110101199001011234",
  "idType": "IdentityCard",
  "gender": "Male",
  "birthDate": "1990-01-01",
  "phoneNumber": "13800138001",
  "address": "北京市朝阳区建国路123号",
  "maritalStatus": "Single",
  "bloodType": "O",
  "emergencyContactName": "李四",
  "emergencyContactPhone": "13900138002",
  "emergencyContactRelation": "配偶"
}
```

#### 步骤2: 调用创建患者API
```bash
POST /api/v1/patients
Authorization: Bearer <doctor_token>
Content-Type: application/json

{
  "name": "张三",
  "idNumber": "110101199001011234",
  "idType": "IdentityCard",
  "gender": "Male",
  "birthDate": "1990-01-01",
  "phoneNumber": "13800138001",
  "address": "北京市朝阳区建国路123号",
  "maritalStatus": "Single",
  "bloodType": "O",
  "emergencyContactName": "李四",
  "emergencyContactPhone": "13900138002",
  "emergencyContactRelation": "配偶"
}
```

#### 步骤3: 接收创建响应
```json
{
  "success": true,
  "message": "患者档案创建成功",
  "data": {
    "id": "00000000-0000-0000-0000-000000000003",
    "name": "张三",
    "pinYinCode": "zhangsan",
    "idNumber": "110****1234",
    "phoneNumber": "138****8001",
    "age": 35,
    "status": "Active",
    "createdAt": "2025-01-01T10:00:00Z",
    "createdBy": "doctor_wang"
  }
}
```

### 1.2 患者信息更新

#### 更新基本信息的API调用
```bash
PUT /api/v1/patients/{patientId}
Authorization: Bearer <doctor_token>
Content-Type: application/json

{
  "phoneNumber": "13800138003",
  "address": "北京市朝阳区建国路456号",
  "maritalStatus": "Married",
  "bloodType": "A",
  "remark": "联系电话和地址更新"
}
```

### 1.3 患�者档案查询

#### 多种查询方式
```bash
# 1. 按ID查询
GET /api/v1/patients/{patientId}
Authorization: Bearer <doctor_token>

# 2. 拼音码搜索
GET /api/v1/patients/search?q=zhangsan&searchType=pinyin
Authorization: Bearer <doctor_token>

# 3. 手机号查询
GET /api/v1/patients/search?q=13800138001&searchType=phone
Authorization: Bearer <doctor_token>

# 4. 身份证号查询
GET /api/v1/patients/search?q=110101199001011234&searchType=idcard
Authorization: Bearer <doctor_token>
```

## 📝 模块二：病史记录管理

### 2.1 病史信息录入

#### 病史分类结构
```csharp
public class MedicalHistory
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public HistoryType Type { get; set; }        // 病史类型
    public string DiseaseName { get; set; }        // 疾病名称
    public string Diagnosis { get; set; }          // 诊断结果
    public DateTime OnsetDate { get; set; }        // 发病时间
    public string Treatment { get; set; }          // 治疗方案
    public string Status { get; set; }             // 疾病状态
    public string Hospital { get; set; }           // 就诊医院
    public string Doctor { get; set; }             // 主治医生
    public string Remarks { get; set; }             // 备注信息
}
```

#### 病史类型枚举
```csharp
public enum HistoryType
{
    Personal = 1,        // 个人史
    Family = 2,           // 家族史
    Surgical = 3,          // 手术史
    Allergy = 4,           // 过敏史
    Medication = 5,        // 用药史
    Chronic = 6,           // 慢性病
    Infectious = 7         // 传染病史
}
```

### 2.2 过敏史管理

#### 过敏史数据结构
```csharp
public class AllergyRecord
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Allergen { get; set; }          // 过敏原
    public AllergyType Type { get; set; }        // 过敏类型
    public SeverityLevel Severity { get; set; } // 严重程度
    public string Symptoms { get; set; }          // 过敏症状
    public DateTime DiscoveredDate { get; set; } // 发现时间
    public string Treatment { get; set; }          // 处理方案
    public string Status { get; set; }             // 当前状态
}
```

#### 过敏类型定义
```csharp
public enum AllergyType
{
    Drug = 1,              // 药物过敏
    Food = 2,              // 食物过敏
    Environmental = 3,     // 环境过敏
    Contact = 4,           // 接触性过敏
    Other = 5              // 其他过敏
}

public enum SeverityLevel
{
    Mild = 1,              // 轻度
    Moderate = 2,           // 中度
    Severe = 3,             // 重度
    LifeThreatening = 4     // 危及生命
}
```

## 📝 模块三：高级搜索功能

### 3.1 智能搜索系统

#### 7级拼音码搜索算法
```csharp
public class PinYinSearchEngine
{
    public List<Patient> SearchByPinYin(string query, IEnumerable<Patient> patients)
    {
        var results = new List<Patient>();
        var searchTokens = TokenizeSearchQuery(query);

        foreach (var patient in patients)
        {
            var score = CalculateSearchScore(searchTokens, patient);
            if (score > 0)
            {
                results.Add(new Patient { /* ... */ });
            }
        }

        return results.OrderByDescending(p => p.SearchScore).ToList();
    }

    private double CalculateSearchScore(List<string> tokens, Patient patient)
    {
        var score = 0.0;

        // Level 1: 完全匹配拼音码
        if (tokens.Any(t => t.Equals(patient.PinYinCode, StringComparison.OrdinalIgnoreCase)))
            score += 100;

        // Level 2: 拼音码前缀匹配
        if (tokens.Any(t => patient.PinYinCode.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            score += 80;

        // Level 3: 拼音码包含匹配
        if (tokens.Any(t => patient.PinYYinCode.Contains(t, StringComparison.OrdinalIgnoreCase)))
            score += 60;

        // Level 4: 姓名拼音匹配
        var namePinyin = ConvertToPinYin(patient.Name);
        if (tokens.Any(t => namePinyin.Contains(t, StringComparison.OrdinalIgnoreCase)))
            score += 40;

        // Level 5: 名字前缀拼音匹配
        if (tokens.Any(t => namePinyin.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
            score += 30;

        // Level 6: 模糊匹配（编辑距离）
        foreach (var token in tokens)
        {
            var distance = CalculateLevenshteinDistance(token, patient.PinYinCode);
            if (distance <= 2)
                score += 20 - (distance * 10);
        }

        return score;
    }
}
```

### 3.2 多条件组合筛选

#### 复杂筛选API
```bash
GET /api/v1/patients/search?name=张&gender=Male&ageMin=30&ageMax=50&bloodType=O&status=Active&pageIndex=1&pageSize=20
Authorization: Bearer <doctor_token>
```

#### 筛选参数说明
| 参数名 | 类型 | 描述 |
|--------|------|------|
| name | string | 患者姓名或拼音码 |
| gender | string | 性别筛选（Male/Female） |
| ageMin | int | 最小年龄 |
| ageMax | int | 最大年龄 |
| bloodType | string | 血型筛选（A/B/O/AB） |
| status | string | 状态筛选 |
| lastVisitAfter | string | 最后就诊时间起始 |
| lastVisitBefore | string | 最后就诊时间结束 |
| visitCountMin | int | 就诊次数最小值 |
| visitCountMax | int | 就诊次数最大值 |

### 3.3 分页和排序

#### 分页查询实现
```csharp
public async Task<PagedResult<PatientDto>> GetPagedAsync(PatientSearchRequest request)
{
    var query = _dbContext.Patients.AsQueryable();

    // 应用筛选条件
    if (!string.IsNullOrEmpty(request.Name))
    {
        query = query.Where(p => p.PinYinCode.Contains(request.Name) ||
                                   p.Name.Contains(request.Name));
    }

    if (request.Gender.HasValue)
    {
        query = query.Where(p => p.Gender == request.Gender.Value);
    }

    if (request.AgeMin.HasValue)
    {
        var minBirthDate = DateTime.Today.AddYears(-request.AgeMin.Value);
        query = query.Where(p => p.BirthDate <= minBirthDate);
    }

    // 应用排序
    query = request.SortBy switch
    {
        "Name" => request.SortOrder == "desc"
            ? query.OrderByDescending(p => p.Name)
            : query.OrderBy(p => p.Name),
        "CreatedDate" => request.SortOrder == "desc"
            ? query.OrderByDescending(p => p.CreatedAt)
            : query.OrderBy(p => p.CreatedAt),
        "LastVisitTime" => request.SortOrder == "desc"
            ? query.OrderByDescending(p => p.LastVisitTime)
            : query.OrderBy(p => p.LastVisitTime),
        _ => query.OrderBy(p => p.CreatedAt)
    };

    // 分页查询
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((request.PageIndex - 1) * request.PageSize)
        .Take(request.PageSize)
        .ProjectToType<PatientDto>()
        .ToListAsync();

    return new PagedResult<PatientDto>
    {
        Items = items,
        TotalCount = totalCount,
        PageIndex = request.PageIndex,
        PageSize = request.PageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
    };
}
```

## 📝 模块四：批量数据处理

### 4.1 患者数据批量导入

#### Excel导入模板格式
```csv
姓名,性别,出生日期,身份证号,手机号,地址,血型,婚姻状况,紧急联系人,紧急联系人电话,紧急联系人关系,过敏史,病史摘要
张三,男,1990-01-01,110101199001011234,13800138001,北京市朝阳区建国路123号,O,单身,李四,13900138002,配偶,青霉素,无特殊病史
李四,女,1985-05-15,110101198505155678,13800138002,北京市海淀区中关村456号,B,已婚,王五,13900138003,父母,花粉过敏,高血压病史
```

#### 批量导入API
```bash
POST /api/v1/patients/bulk-import
Authorization: Bearer <admin_token>
Content-Type: multipart/form-data

file=@patients_data.xlsx
options={
  "dryRun": false,
  "skipDuplicates": true,
  "validateIdCard": true,
  "autoGeneratePinYin": true,
  "sendNotification": false,
  "batchSize": 50
}
```

#### 导入验证规则
```csharp
public class PatientImportValidator
{
    public ValidationResult ValidatePatientData(PatientImportData data)
    {
        var result = new ValidationResult();

        // 身份证号验证
        if (!IsValidIdCard(data.IdNumber, data.IdType))
        {
            result.AddError($"身份证号 {data.IdNumber} 格式不正确");
        }

        // 手机号验证
        if (!Regex.IsMatch(data.PhoneNumber, @"^1[3-9]\d{9}$"))
        {
            result.AddError($"手机号 {data.PhoneNumber} 格式不正确");
        }

        // 出生日期验证
        if (data.BirthDate > DateTime.Today)
        {
            result.AddError("出生日期不能晚于当前日期");
        }

        // 年龄合理性检查
        var age = CalculateAge(data.BirthDate);
        if (age > 120)
        {
            result.AddWarning($"患者年龄 {age}岁，请确认出生日期是否正确");
        }

        return result;
    }
}
```

### 4.2 数据导出功能

#### 导出格式选择
```bash
GET /api/v1/patients/export
Authorization: Bearer <admin_token>
```

#### 导出查询参数
```
format=excel&filter=ageMin:30,ageMax:60,gender:Female&status=Active&fields=Name,PhoneNumber,Age,BloodType,LastVisitTime
```

#### 导出字段配置
```json
{
  "availableFields": [
    {
      "name": "Name",
      "displayName": "姓名",
      "dataType": "string"
    },
    {
      "name": "PinYinCode",
      "displayName": "拼音码",
      "dataType": "string"
    },
    {
      "name": "Age",
      "displayName": "年龄",
      "dataType": "number"
    },
    {
      "name": "PhoneNumber",
      "displayName": "手机号",
      "dataType": "string"
    },
    {
      "name": "BloodType",
      "displayName": "血型",
      "dataType": "string"
    },
    {
      "name": "LastVisitTime",
      "displayName": "最后就诊",
      "dataType": "datetime"
    }
  ],
  "defaultFields": ["Name", "PinYinCode", "Age", "PhoneNumber"]
}
```

## 📝 模块五：隐私保护和合规

### 5.1 数据访问控制

#### 基于角色的数据访问
```csharp
public class PatientDataAccessControl
{
    public async Task<bool> CanAccessPatientAsync(string userId, string patientId, string operation)
    {
        var user = await _userService.GetByIdAsync(Guid.Parse(userId));
        var patient = await _patientRepository.GetByIdAsync(Guid.Parse(patientId));

        if (user == null || patient == null) return false;

        // 检查用户权限
        switch (user.Role)
        {
            case UserRole.Doctor:
                return await CanDoctorAccessPatientAsync(user, patient, operation);

            case UserRole.Nurse:
                return await CanNurseAccessPatientAsync(user, patient, operation);

            case UserRole.Admin:
                return await CanAdminAccessPatientAsync(user, patient, operation);

            default:
                return false;
        }
    }

    private async Task<bool> CanDoctorAccessPatientAsync(User user, Patient patient, string operation)
    {
        // 医生可以访问所有患者，但敏感操作需要特殊检查
        if (operation == "Delete" || operation == "Disable")
        {
            // 删除或禁用患者需要管理员权限
            return false;
        }

        if (operation == "Export" || operation == "Print")
        {
            // 导出或打印敏感数据需要记录操作日志
            await LogSensitiveDataOperationAsync(user.Id, patientId, operation);
        }

        return true;
    }
}
```

### 5.2 数据加密和脱敏

#### 数据加密存储
```csharp
public class PatientEncryptionService
{
    private readonly IEncryptionProvider _encryptionProvider;

    public async Task<string> EncryptSensitiveDataAsync(string data, string patientId)
    {
        // 使用患者ID作为加密密钥的一部分
        var key = GenerateEncryptionKey(patientId);
        return await _encryptionProvider.EncryptAsync(data, key);
    }

    public async Task<string> DecryptSensitiveDataAsync(string encryptedData, string patientId)
    {
        var key = GenerateEncryptionKey(patientId);
        return await _encryptionProvider.DecryptAsync(encryptedData, key);
    }

    private string GenerateEncryptionKey(string patientId)
    {
        // 基于患者ID和时间戳生成唯一加密密钥
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{patientId}_patient_key"));
        return Convert.ToBase64String(hash);
    }
}
```

#### 数据脱敏显示
```csharp
public class PatientDataMasking
{
    public PatientDto MaskPatientData(Patient patient, UserRole viewerRole)
    {
        var dto = new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            PinYinCode = patient.PinYinCode,
            // 根据查看者角色决定脱敏程度
            IdNumber = MaskIdCard(patient.IdNumber, viewerRole),
            PhoneNumber = MaskPhoneNumber(patient.PhoneNumber, viewerRole),
            Address = MaskAddress(patient.Address, viewerRole)
        };

        return dto;
    }

    private string MaskIdCard(string idCard, UserRole role)
    {
        return role switch
        {
            UserRole.Nurse => $"{idCard.Substring(0, 3)}****{idCard.Substring(idCard.Length - 4)}",
            UserRole.Doctor => $"{idCard.Substring(0, 3)}****{idCard.Substring(idCard.Length - 4)}",
            UserRole.Admin => idCard, // 管理员可以查看完整信息
            _ => "****"
        };
    }
}
```

### 5.3 操作审计

#### 审计日志记录
```csharp
public class PatientAuditService
{
    public async Task LogPatientOperationAsync(
        string operation,
        string patientId,
        string userId,
        Dictionary<string, object> changedFields = null)
    {
        var audit = new PatientAuditLog
        {
            Id = Guid.NewGuid(),
            Operation = operation,
            PatientId = Guid.Parse(patientId),
            UserId = Guid.Parse(userId),
            IPAddress = GetClientIPAddress(),
            UserAgent = GetUserAgent(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = changedFields != null
                ? JsonSerializer.Serialize(changedFields)
                : null,
            Success = true
        };

        await _auditRepository.AddAsync(audit);

        // 敏感操作需要额外验证
        if (IsSensitiveOperation(operation))
        {
            await ValidateSensitiveOperationAsync(audit);
        }
    }

    private bool IsSensitiveOperation(string operation)
    {
        var sensitiveOps = new[]
        {
            "Delete", "Export", "Print", "Disable", "EmergencyContact"
        };

        return sensitiveOps.Contains(operation);
    }
}
```

## 🔧 实践练习

### 练习1: 完整患者管理流程
**目标**: 从患者注册到档案维护的完整流程

**要求**:
1. 创建新患者档案，包含完整的基本信息
2. 添加个人史、家族史和过敏史记录
3. 测试拼音码搜索功能的有效性
4. 练习患者信息的更新和状态管理
5. 验证数据隐私保护机制

**验证步骤**:
- [ ] 患者档案创建成功，生成了正确的拼音码
- [ ] 身份证号验证和唯一性检查生效
- [ ] 病史信息分类记录正确
- [ ] 拼音码搜索功能准确返回结果
- [ ] 数据脱敏机制根据角色正确工作
- [ ] 操作审计日志完整记录

### 练习2: 高级搜索和数据处理
**目标**: 实现复杂的患者搜索和批量操作

**要求**:
1. 实现多条件组合搜索功能
2. 测试7级拼音码搜索算法
3. 实现Excel批量导入和导出功能
4. 优化大数据量下的搜索性能
5. 实现搜索结果的缓存机制

**性能要求**:
- 搜索响应时间 < 300ms
- 支持1000+患者数据的实时搜索
- 批量导入处理速度 > 100条/秒
- 缓存命中率 > 90%

### 练习3: 数据安全和隐私保护
**目标**: 加强患者数据的安全性和合规性

**要求**:
1. 实现基于角色的数据访问控制
2. 加密存储敏感数据
3. 实现完整的数据脱敏机制
4. 建立完善的操作审计系统
5. 配置数据备份和恢复策略

**安全验证**:
- [ ] 不同角色用户只能访问授权数据
- [ ] 敏感数据在传输和存储中加密
- [ 数据脱敏在所有界面正确显示
- [ ] 所有操作都有完整审计记录
- [ ] 数据备份和恢复机制正常工作

## 🚨 常见问题和解决方案

### Q1: 患者身份证号重复或无效
**解决方案**:
1. 使用身份证号验证算法进行格式检查
2. 检查系统中是否已存在相同身份证号
3. 对于无效身份证号，提供修正建议
4. 支持手动输入和证件扫描两种录入方式

### Q2: 拼音码搜索结果不准确
**解决方案**:
1. 检查拼音码生成算法的正确性
2. 优化搜索算法的权重分配
3. 增加同音字和多音字处理
4. 提供搜索建议和自动纠错功能

### Q3: 批量导入时数据格式错误
**解决方案**:
1. 提供标准Excel模板下载
2. 实现数据格式预检查
3. 提供详细的错误报告和修正建议
4. 支持分批次处理，避免全部失败

### Q4: 患者隐私数据泄露风险
**解决方案**:
1. 实施端到端的数据加密
2. 严格控制数据访问权限
3. 定期进行安全审计
4. 建立数据泄露应急响应机制

## ✅ 学习成果验证

完成以下任务以验证学习成果：

### 验证任务1: 基础患者管理
- [ ] 成功创建和查询患者档案
- [ ] 正确录入和管理病史信息
- [ ] 实现患者状态管理
- [ ] 验证拼音码搜索功能
- [ ] 测试数据隐私保护

### 验证任务2: 高级功能实现
- [ ] 实现多条件搜索和筛选
- [ ] 完成批量数据导入导出
- [ ] 优化搜索性能（<300ms响应）
- [ ] 实现搜索结果缓存
- [ ] 测试数据加密和脱敏

### 验证任务3: 安全和合规
- [ ] 实现基于角色的数据访问控制
- [ ] 建立完整操作审计系统
- [ ] 配置数据备份恢复
- [ ] 验证数据加密保护
- [ ] 完成合规性检查

## 📚 后续学习路径

完成本教程后，建议继续学习：

1. **[病历管理模块教程](../medical-case/medical-case-tutorial.md)** - 学习病历创建和管理
2. **[中医诊断模块教程](../consultation/consultation-tutorial.md)** - 学习四诊信息记录
3. **[数据隐私保护指南](../../how-to-guides/security/privacy-protection.md)** - 深入学习数据保护技术
4. **[医疗数据合规规范](../../explanation/business-domain/compliance.md)** - 了解医疗行业合规要求

## 🔗 相关资源

### 技术文档
- [Patients API参考文档](../../reference/api/patients.md)
- [数据隐私保护指南](../../reference/business-rules/privacy-protection.md)
- [数据模型说明](../../reference/technical-specs/entity-models.md)

### 开发资源
- [患者服务源码](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Server/Modules/LYBT.Module.Patients)
- [患者管理界面组件](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Client/Desktop/Modules/LYBT.Desktop.Patients)
- [拼音码生成工具](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Shared/LYBT.Shared.Utils/Pinyin)

### 外部资源
- [HIPAA隐私保护规范](https://www.hhs.gov/hipaa/)
- [个人信息保护法](https://www.npc.gov.cn/npc/c12459/202408/714586ca59a842b6697814a5711c69cb.shtml)
- [电子病历基本规范](http://www.nhc.gov.cn/gui/yuans/76165.shtml)

---

**文档类型**: Tutorial
**学习时间**: 75分钟
**难度级别**: 中高级
**维护团队**: 架构组 + 医疗业务团队
**更新时间**: 2025-11-22